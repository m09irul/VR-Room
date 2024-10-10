using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using UnityEngine.XR.Interaction.Toolkit;


public class STT_Groq_OpenAI : MonoBehaviour
{
    [SerializeField]
    private string GROQ_API_KEY;
    const string GROQ_API_URI = "https://api.groq.com/openai/v1/audio/transcriptions";      //POST URI

    AI_WAV wavObject;                                   //Object that holds stream and methods for WAV
    AI_STT_Text_Filter aiSTTTextFilter;


    private void Start()
    {
        //Note: you can't use new to allocate memory for MonoBehavior objects
        wavObject = GetComponent<AI_WAV>();                      //Start with a clean stream
        aiSTTTextFilter = GetComponent<AI_STT_Text_Filter>();    //Connect with Text Filter

        StartSpeaking();
    }


    //=========================================================================
    //Event handlers initiate the AI Conversation
    //  Enter these in an XR Grab Interactable component -> Interactable Events
    //  of the GameObject you want to have a conversation with
    //=========================================================================
    public void SelectEnterEventHandler(SelectEnterEventArgs eventArgs)
    {
        StartSpeaking();
    }

    public void SelectExitEventHandler(SelectExitEventArgs eventArgs)
    {
        Microphone.End(null);
    }


    private void StartSpeaking()
    {
        wavObject = new AI_WAV();               //Start with a clean stream

        //Setup the AudioSource for reading
        AudioSource aud = GetComponent<AudioSource>();
        for (int i = 0; i < Microphone.devices.Length; i++)
            Debug.Log(Microphone.devices[i]);   //need to auto select the VR system if available?

        //listen to the mic for 30 sec, change to start/end click event! Non-blocking so use Coroutine!
        Debug.Log("Start recording");
        aud.clip = Microphone.Start(null, false, 30, 11025);        //use default mic

        StartCoroutine(RecordAudio(aud.clip));
    }


    //Coroutine to wait until recording is finished
    IEnumerator RecordAudio(AudioClip clip)
    {
        while (Microphone.IsRecording(null))
        {
            yield return null;
        }

        Debug.Log("Done Recording!");
        AudioSource aud = GetComponent<AudioSource>();
        
        wavObject.ConvertClipToWav(aud.clip);       //wavObject now holds the WAV stream data
        StartCoroutine(STT());                      //Call STT cloudsvc   
    }


    //REST API Call using the converted WAV stream buffer
    IEnumerator STT()
    {   
        //Groq STT doesnt use JSON but http forms
        WWWForm form = new WWWForm();
        form.AddField("model", "whisper-large-v3");
        form.AddField("language", "en");
        form.AddBinaryData("file", wavObject.stream.GetBuffer(), "audio.wav", "audio/wav" );          //push the data into a http form field
        UnityWebRequest request = UnityWebRequest.Post(GROQ_API_URI, form);                 //slightly different, not using JSON but Form to send parameters
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        //Headers
        request.SetRequestHeader("Authorization", "Bearer " + GROQ_API_KEY);                //Don't add a header Content-Type: Application/json here as it uses a http form

        // Send the request and decompress the multimedia response
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            SpeechToTextData sttResponse = JsonUtility.FromJson<SpeechToTextData>(responseText);

            // Extract the "Content" section, text
            Debug.Log(sttResponse.text);

            //Now analyze the text and direct to LLM or TTI or....
            //aiSTTTextFilter.DirectToCloudProviders(sttResponse.text);
        }
        else Debug.LogError("API request failed: " + request.error);
    }


    //JSON Output Class representation
    [Serializable]
    public class SpeechToTextData
    {
        public string text;
    }
}
