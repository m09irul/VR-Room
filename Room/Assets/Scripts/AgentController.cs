using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;

public class AgentController : MonoBehaviour
{
    public static AgentController instance;
    // Timer for controlling session stages
    public float sessionDuration = 1800f; // 30 minutes in seconds
    public float sessionTimer;

    // UI Elements
    public TextMeshProUGUI agentDialogueText;
    public Button stressLevelButton;

    // Stress levels for multiple users
    public Dictionary<int, int> userStressLevels = new Dictionary<int, int>(); // Key: userID, Value: stress level

    // Number of participants
    public enum SessionType { Single, Group }
    public SessionType sessionType = SessionType.Single;

    // VR interactions
    public GameObject breathingGuide;
    public GameObject muscleRelaxationGuide;

    // ChatGPT API Configuration
    public string apiUrl = "https://api.openai.com/v1/chat/completions"; // OpenAI API URL
    [SerializeField] private string apiKey; // REPLACE THIS with your actual API key (sk-***)

    // States for the session flow
    public enum SessionStage { Introduction, BreathingExercise, RelaxationActivity, Reflection, CheckOut }
    public SessionStage currentStage;

    // Event Triggers
    public bool isIntroductionComplete = false;
    public bool isBreathingComplete = false;
    public bool isReflectionComplete = false;

    [SerializeField]
    TTSManager ttsManager;
    [SerializeField]
    Breathing m_breathing;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        sessionTimer = 0;
        currentStage = SessionStage.Introduction;

        // Start session flow
        StartCoroutine(SessionFlow());
    }

    IEnumerator SessionFlow()
    {
        // Introduction Stage (3-5 minutes)
        StartCoroutine(StartIntroduction());

        // Breathing Exercise Stage (5-7 minutes)
        yield return new WaitUntil(() => isIntroductionComplete);

        if(currentStage == SessionStage.BreathingExercise)
            StartCoroutine(m_breathing.BreathingSession());
        yield return new WaitUntil(() => isBreathingComplete);

        // Relaxation Activity Stage (7-10 minutes)
        StartRelaxationActivity();
        yield return new WaitForSeconds(600);

        // Reflection Stage (5-7 minutes)
        StartReflection();
        yield return new WaitUntil(() => isReflectionComplete);

        // Check-out Stage (3-5 minutes)
        StartCheckOut();

    }

    IEnumerator StartIntroduction()
    {
        currentStage = SessionStage.Introduction;

        yield return PlayPhase("Hello, it’s good to have you here today. Thank you for taking the time to join me for this relaxation session. This time is just for you—to relax, reset, and let go of whatever is weighing on your mind right now.\n" +
            " In the next 20 to 30 minutes, we’ll go through a few simple techniques designed to help you unwind. " +
            "There’s no right or wrong way to do them—just follow along at your own pace. " +
            "We’ll start with some focused breathing exercises to calm the body. " +
            "Then, we’ll move into a short guided activity to further ease any tension. " +
            "If at any point you need to pause, take your time—this is your space.", 50,
            () => {
                Debug.Log("Introduction phase completed");
                isIntroductionComplete = true;
                currentStage = SessionStage.BreathingExercise;
            });
    }
    IEnumerator PlayPhase(string message, float duration, System.Action onComplete = null)
    {
        agentDialogueText.text = message;
        yield return ttsManager.SynthesizeAndPlayCoroutine(message);
        yield return new WaitForSeconds(duration);
        sessionDuration += duration;

        onComplete?.Invoke();
    }

    async void StartRelaxationActivity()
    {
        currentStage = SessionStage.RelaxationActivity;

        if (sessionType == SessionType.Single)
        {
            string prompt = await GetDynamicPrompt("relaxation_activity", "single");
            agentDialogueText.text = prompt;
            agentDialogueText.text += "\nCan you think of one relaxing activity you can do today to continue this feeling of calm?";
        }
        else if (sessionType == SessionType.Group)
        {
            string prompt = await GetDynamicPrompt("relaxation_activity", "group");
            agentDialogueText.text = prompt;
            agentDialogueText.text += "\nLet's each suggest one relaxing activity we can do this week to maintain this feeling of calm.";
        }

        muscleRelaxationGuide.SetActive(true);
        StartCoroutine(RelaxationActivityCoroutine());
    }

    IEnumerator RelaxationActivityCoroutine()
    {
        yield return new WaitForSeconds(600);
        muscleRelaxationGuide.SetActive(false);
        StartReflection();
    }

    async void StartReflection()
    {
        currentStage = SessionStage.Reflection;

        if (sessionType == SessionType.Single)
        {
            string prompt = await GetDynamicPrompt("reflection_cbt", "single");
            agentDialogueText.text = prompt;

            // Additional CBT Questions for cognitive restructuring:
            agentDialogueText.text += "\nLet's reflect: What thoughts did you notice during the session?";
            agentDialogueText.text += "\nHow did these thoughts affect how you felt?";
            agentDialogueText.text += "\nWhat alternative, more balanced thoughts could you consider?";

            // Indicate reflection completion
            isReflectionComplete = true;
        }
        else if (sessionType == SessionType.Group)
        {
            string prompt = await GetDynamicPrompt("reflection_cbt", "group");
            agentDialogueText.text = prompt;

            // Start group reflection in a coroutine
            StartCoroutine(HandleGroupReflection());
        }
    }

    IEnumerator HandleGroupReflection()
    {
        for (int i = 1; i <= 3; i++)
        {
            agentDialogueText.text += $"\nUser {i}, what stressful thoughts did you notice during the session?";
            yield return new WaitForSeconds(5); // Simulate waiting for user input
        }

        // Additional Group CBT Reflection:
        agentDialogueText.text += "\nAs a group, let's discuss alternative ways of viewing these thoughts.";

        // Mark reflection as complete
        isReflectionComplete = true;
    }


    async void StartCheckOut()
    {
        currentStage = SessionStage.CheckOut;

        // End session in 3 minutes
        Invoke("EndSession", 180);
    }

    void EndSession()
    {
        agentDialogueText.text = "Thank you for participating in the relaxation session!";
    }

    // Fetch dynamic prompts from ChatGPT API
    private async Task<string> GetDynamicPrompt(string stage, string sessionContext)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestData = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a relaxation therapist." },
                    new { role = "user", content = $"Generate a {stage} prompt for a {sessionContext} session." }
                },
                max_tokens = 150
            };

            string jsonRequest = JsonConvert.SerializeObject(requestData);
            StringContent content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

            return jsonResponse.choices[0].message.content.ToString();
        }
    }
}
