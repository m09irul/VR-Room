using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LLM_Groq : MonoBehaviour
{
    [SerializeField]
    private string apiKey;
    const string apiURI = "https://api.groq.com/openai/v1/chat/completions";

    private enum LLMModel { llama3_8b_8192, llama3_70b_8192, llama_3X1_70b_versatile, llama_3X1_8b_instant, mixtral_8x7b_32768, gemma_7b_it, gemma2_9b_it, llama3_groq_70b_8192_tool_use_preview, llama3_groq_8b_8192_tool_use_preview }

    [SerializeField]
    private LLMModel selectedModel;
    string selectedLLMString;

    private string LLMresult = "Waiting";

    [SerializeField]
    TTS_SF_Simba ttsSFSimba;

    [SerializeField]
    private bool shortResponse;


    // Start is called before the first frame update
    void Start()
    {
        selectedLLMString = selectedModel.ToString().Replace('_', '-').Replace('X', '.');
        Debug.Log("You have selected LLM: " + selectedLLMString);
    }

    
    public void TextToLLM(string mesg)
    {
        StartCoroutine(TalkToLLM(shortResponse ?  mesg + ", keep it short!" : mesg));
    }


    private IEnumerator TalkToLLM(string mesg)
    {
        RequestBody requestBody = new RequestBody();
        requestBody.messages = new Message[]
        {
            new Message {role = "user", content = mesg}
        };
        requestBody.model = selectedLLMString;
        string jsonRequestBody = JsonUtility.ToJson(requestBody);
        LLMresult = "Waiting";

        UnityWebRequest request = new UnityWebRequest(apiURI, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequestBody);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        //headers
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            GroqCloudResponse groqCS = JsonUtility.FromJson<GroqCloudResponse>(responseText);
            LLMresult = groqCS.choices[0].message.content;  //here is the field where the actual response is!
            Debug.Log(LLMresult);

            //now lets call TTS!
            if (ttsSFSimba) ttsSFSimba.Say(LLMresult);
            //add addl TTS modules here 
        }
        else Debug.Log("LLM API Request failed: " + request.error);

    }


    //=============================
    //Write JSON to LLM classes - generated with LLama!
    //=============================
    [System.Serializable]
    public class RequestBody
    {
        public Message[] messages;
        public string model;
    }


    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }


    //Read JSON response from LLM classes
    [System.Serializable]
    public class GroqCloudResponse
    {
        public string id;
        public string @object;
        public int created;
        public string model;
        public Choice[] choices;
        public Usage usage;
        public string system_fingerprint;
        public XGroq x_groq;
    }

    [System.Serializable]
    public class Choice
    {
        public int index;
        public ChoiceMessage message;
        public object logprobs;
        public string finish_reason;
    }

    [System.Serializable]
    public class ChoiceMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public float prompt_time;
        public int completion_tokens;
        public float completion_time;
        public int total_tokens;
        public float total_time;
    }

    [System.Serializable]
    public class XGroq
    {
        public string id;
    }
}
