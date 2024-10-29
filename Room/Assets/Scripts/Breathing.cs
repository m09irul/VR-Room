using System.Collections;
using UnityEngine;
using TMPro;  // For displaying text instructions
using UnityEngine.UI;  // Optional: UI elements
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.ProBuilder.Shapes;
using DentedPixel;

public class Breathing : MonoBehaviour
{
    public Transform breathingSphere;  // Expands/Contracts with breath
    public TextMeshProUGUI instructionText;  // Display instructions
    public AudioSource voiceAudioSource;  // Plays voice guidance

    public float inhaleTime = 2f;
    public float holdTime = 2f;
    public float exhaleTime = 3f;
    public float sessionDuration = 300f;  // 5-minute session

    public bool isSessionActive = false;
    public float elapsedTime = 0f;
    public HttpClient httpClient;

    [SerializeField]
    TTSManager ttsManager;

    void Start()
    {
        httpClient = new HttpClient();
    }

    public IEnumerator BreathingSession()
    {
        isSessionActive = true;
        elapsedTime = 0f;

        // Step 1: Introduction (30-60 seconds)
        yield return PlayPhase("Let's take a few moments to focus on our breathing. This exercise will help calm your mind and body.", 5);

        // Step 2: Initial Rounds of Breathing (1-2 minutes)
        float initialRoundDuration = 10; // Increased to 2 minutes for better practice
        while (elapsedTime < initialRoundDuration && isSessionActive)
        {
            yield return BoxBreathing();
        }

        // Step 3: Mid-Session Check-In
        if (isSessionActive)
        {
            yield return PlayPhase("Notice how your breath feels as you inhale and exhale. Are you feeling more relaxed or centered?", 10);
        }

        // Step 4: Continue Breathing Cycles until near session end
        while (elapsedTime < sessionDuration - 60f && isSessionActive)
        {
            yield return BoxBreathing();
        }

        // Step 5: Closing
        if (isSessionActive)
        {
            yield return PlayPhase("Take one last deep breath... Now gently return to your normal breathing pattern. Keep this sense of calm with you.", 15);
            instructionText.text = "Great job! You've completed the breathing exercise.";
            yield return ttsManager.SynthesizeAndPlayCoroutine(instructionText.text);
        }

        isSessionActive = false;
        yield return new WaitForSeconds(5);
        AgentController.instance.isBreathingComplete = true;
    }

    IEnumerator PlayPhase(string message, float duration)
    {
        instructionText.text = message;
        yield return ttsManager.SynthesizeAndPlayCoroutine(message);
        yield return new WaitForSeconds(duration);
        elapsedTime += duration;
    }
    IEnumerator BoxBreathing()
    {
        // Inhale
        LeanTween.cancel(breathingSphere.gameObject);

        var msg = "Inhale deeply...";
        instructionText.text = msg;
        yield return ttsManager.SynthesizeAndPlayCoroutine(msg);

        LeanTween.scale(breathingSphere.gameObject, Vector3.one * 1.5f, inhaleTime)
            .setEase(LeanTweenType.easeInOutSine);
        yield return new WaitForSeconds(inhaleTime);

        // Hold after inhale
        msg = "Hold your breath...";
        instructionText.text = msg;
        yield return ttsManager.SynthesizeAndPlayCoroutine(msg);

        yield return new WaitForSeconds(holdTime);

        // Exhale
        msg = "Exhale slowly...";
        instructionText.text = msg;
        yield return ttsManager.SynthesizeAndPlayCoroutine(msg);

        LeanTween.scale(breathingSphere.gameObject, Vector3.one * 0.5f, exhaleTime)
            .setEase(LeanTweenType.easeInOutSine);
        yield return new WaitForSeconds(exhaleTime);

        // Hold after exhale
        msg = "Hold your breath...";
        instructionText.text = msg;
        yield return ttsManager.SynthesizeAndPlayCoroutine(msg);

        yield return new WaitForSeconds(holdTime);

        elapsedTime += inhaleTime + 2 * holdTime + exhaleTime;
    }
    void OnDestroy()
    {
        httpClient.Dispose();
    }
}
