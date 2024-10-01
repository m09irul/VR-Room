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
    // Timer for controlling session stages
    public float sessionDuration = 1800f; // 30 minutes in seconds
    private float sessionTimer;

    // UI Elements
    public TextMeshProUGUI agentDialogueText;
    public Button stressLevelButton;

    // Stress levels for multiple users
    private Dictionary<int, int> userStressLevels = new Dictionary<int, int>(); // Key: userID, Value: stress level

    // Number of participants
    public enum SessionType { Single, Group }
    public SessionType sessionType = SessionType.Single;

    // VR interactions
    public GameObject breathingGuide;
    public GameObject muscleRelaxationGuide;

    // ChatGPT API Configuration
    private string apiUrl = "https://api.openai.com/v1/chat/completions"; // OpenAI API URL
    [SerializeField] private string apiKey; // REPLACE THIS with your actual API key (sk-***)

    // States for the session flow
    private enum SessionStage { Introduction, BreathingExercise, RelaxationActivity, Reflection, CheckOut }
    private SessionStage currentStage;

    // Event Triggers
    private bool isBreathingComplete = false;
    private bool isReflectionComplete = false;

    void Start()
    {
        sessionTimer = sessionDuration;
        currentStage = SessionStage.Introduction;

        if (sessionType == SessionType.Single)
        {
            // Single session: Initialize for one participant (userID 1)
            userStressLevels[1] = 0;
        }
        else if (sessionType == SessionType.Group)
        {
            // Group session: Initialize for three participants (userID 1, 2, 3)
            userStressLevels[1] = 0;
            userStressLevels[2] = 0;
            userStressLevels[3] = 0;
        }

        // Start session flow
        StartCoroutine(SessionFlow());
    }

    IEnumerator SessionFlow()
    {
        // Introduction Stage (3-5 minutes)
        StartIntroduction();
        yield return new WaitForSeconds(300);

        // Breathing Exercise Stage (5-7 minutes)
        StartBreathingExercise();
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

    async void StartIntroduction()
    {
        currentStage = SessionStage.Introduction;

        if (sessionType == SessionType.Single)
        {
            // Fetch prompt for single user introduction
            string prompt = await GetDynamicPrompt("introduction", "single");
            agentDialogueText.text = prompt;

            // Prompt single user to input stress level
            stressLevelButton.gameObject.SetActive(true);
        }
        else if (sessionType == SessionType.Group)
        {
            // Fetch prompt for group introduction
            string prompt = await GetDynamicPrompt("introduction", "group");
            agentDialogueText.text = prompt;

            // Prompt each user in the group to input their stress level
            StartCoroutine(CollectGroupStressLevels());
        }
    }

    // For Single Session: Stress Level Input
    public void SetStressLevel(int level)
    {
        userStressLevels[1] = level; // For single session, userID 1
        stressLevelButton.gameObject.SetActive(false);
        StartBreathingExercise();
    }

    // For Group Session: Collect Stress Levels from All Participants
    IEnumerator CollectGroupStressLevels()
    {
        // Simulate collecting stress levels from multiple users
        for (int i = 1; i <= 3; i++)
        {
            // Fetch individual user prompts (you can replace this with actual VR user input)
            string prompt = $"User {i}, please input your stress level.";
            agentDialogueText.text = prompt;

            yield return new WaitForSeconds(5); // Simulating the time to input stress level
            userStressLevels[i] = Random.Range(1, 11); // Random input for demonstration
        }

        agentDialogueText.text = "Thank you, everyone. Let's proceed.";
        StartBreathingExercise();
    }

    async void StartBreathingExercise()
    {
        currentStage = SessionStage.BreathingExercise;

        if (sessionType == SessionType.Single)
        {
            string prompt = await GetDynamicPrompt("breathing_exercise", "single");
            agentDialogueText.text = prompt;
        }
        else if (sessionType == SessionType.Group)
        {
            string prompt = await GetDynamicPrompt("breathing_exercise", "group");
            agentDialogueText.text = prompt;
        }

        breathingGuide.SetActive(true);
        StartCoroutine(BreathingExerciseCoroutine());
    }

    IEnumerator BreathingExerciseCoroutine()
    {
        yield return new WaitForSeconds(180);
        breathingGuide.SetActive(false);
        isBreathingComplete = true;
        StartRelaxationActivity();
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

        if (sessionType == SessionType.Single)
        {
            string prompt = await GetDynamicPrompt("checkout", "single");
            agentDialogueText.text = prompt;

            stressLevelButton.gameObject.SetActive(true); // Single user input
        }
        else if (sessionType == SessionType.Group)
        {
            string prompt = await GetDynamicPrompt("checkout", "group");
            agentDialogueText.text = prompt;

            // Simulate checking out for each user in the group
            StartCoroutine(CollectGroupStressLevels());
        }

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
