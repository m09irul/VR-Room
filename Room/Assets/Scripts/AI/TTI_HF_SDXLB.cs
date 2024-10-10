using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using UnityEngine.UI;

public class TTI_HF_SDXLB : MonoBehaviour
{
    [SerializeField]
    private string HF_INF_API_KEY;
    const string TTI_API_URI = "https://api-inference.huggingface.co/models/stabilityai/stable-diffusion-xl-base-1.0";      //POST URI

    //We need to to able to talk, ensure you pick the same ones as Groq!
    [SerializeField]
    TTS_RA_OpenAI ttsOpenAI;
    [SerializeField]
    TTS_RA_Speach ttsSpeach;
    [SerializeField]
    TTS_SF_Simba ttsSimba;


    Animator avtAnimator;
    int texCount;

    public void GetImage(string prompt)
    {
        avtAnimator = GetComponent<Animator>();

        StartCoroutine(SD(prompt));
        Debug.Log("TTI: " + prompt);
    }


    IEnumerator SD(string prompt)
    {
        TTIData ttiData = new TTIData();
        ttiData.inputs = prompt;
        string jsonPrompt = JsonUtility.ToJson(ttiData);

        //Set up the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(TTI_API_URI, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPrompt));
        request.downloadHandler = new DownloadHandlerTexture();

        //Headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + HF_INF_API_KEY);

        if (ttsOpenAI) ttsOpenAI.Say("Generating image!");
        if (ttsSimba)  ttsSimba.Say("Generating image!");
        if (ttsSpeach) ttsSpeach.Say("Generating image!");

        avtAnimator.SetBool("isPainting", true);     //pretend you're working hard :)
        // Send the request and decompress the multimedia response
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D img = DownloadHandlerTexture.GetContent(request);
            
            GameObject genImg;
            genImg = Resources.Load<GameObject>("ImageFrame");
            if (!genImg) Debug.Log("Can't load the ImageFrame for the TTI output");
            else
            {
                //generate the image prefab
                GameObject g2 = Instantiate(genImg, transform.position+new Vector3(-1,1.5f,0), Quaternion.identity);    //to the left
                
                //set texture
                Material myNewMaterial = new Material(Shader.Find("Standard"));
                myNewMaterial.SetTexture("_MainTex", img);
                g2.GetComponent<MeshRenderer>().material = myNewMaterial;

                avtAnimator.SetBool("isPainting", false);        //done working

            }
        }
        else Debug.LogError("TTI API request failed: " + request.error);

    }

    //JSON Input Class representation
    [Serializable]
    public class TTIData
    {
        public string inputs;        //core only, need to expand with addl. parameters
        
    }

    //Output data is JPG, no JSON wrapper required
}
