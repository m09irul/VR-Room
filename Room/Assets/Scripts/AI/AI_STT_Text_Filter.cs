using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_STT_Text_Filter : MonoBehaviour
{
    //Classes of content for selecting either SD, LLM etc..
    public enum PreFilterClass { isImage, isSpeech, is3D, isCode };      //add more when needed

    [SerializeField]
    private LLM_Groq llmGroq;                           //configure in Inspector UI

    [SerializeField]
    private TTI_HF_SDXLB ttI_HF;                        //configure in Inspector UI


    private PreFilterClass Analyze(string input)
    {
        if (input.ToLower().Contains("image") || input.ToLower().Contains("picture") || input.ToLower().Contains("photo"))
            return PreFilterClass.isImage;

        //Default
        return PreFilterClass.isSpeech;
    }


    public void DirectToCloudProviders(string sttResponseText)
    {
        switch (Analyze(sttResponseText))
        {
            case PreFilterClass.isSpeech:
                //Now send the text to LLM
                if (llmGroq) llmGroq.TextToLLM(sttResponseText);
                break;

            case PreFilterClass.isImage:
                //Apparently we're asking for an image so send to StableDiffusion
                if (ttI_HF) ttI_HF.GetImage(sttResponseText);
                break;

            default:
                Debug.LogWarning("Don't know what to do with this request!");
                break;
        }
    }
}
