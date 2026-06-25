using System;
using Unity.XR.OpenXR.Features.PICOSupport;
using UnityEngine;
using UnityEngine.UI;

public class MRBackgroundController : MonoBehaviour
{
    [SerializeField] GameObject[] PassthroughRemoveObjects;
    [SerializeField] bool isSkyboxOnStart = false;
    Camera cam;

    
    
    public void ActivatePassthrought(Slider slider)
    {
        if(slider.value > 0)
        {
            RemovePassthroughObjects();
            PassthroughFeature.EnableSeeThroughManual(true);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
        else
        {
            ActivatePassthroughObjtcts();
            PassthroughFeature.EnableSeeThroughManual(false);
            cam.clearFlags = CameraClearFlags.Skybox;
        }
    }

    void Awake()
    {
        cam = Camera.main;
        if (isSkyboxOnStart)
        {
            ActivatePassthroughObjtcts();
            PassthroughFeature.EnableSeeThroughManual(false);
            cam.clearFlags = CameraClearFlags.Skybox;
        }
    }

    void RemovePassthroughObjects()
    {
        foreach (var obj in PassthroughRemoveObjects)
        {
            obj.SetActive(false);
        }
    }

    void ActivatePassthroughObjtcts()
    {
        foreach (var obj in PassthroughRemoveObjects)
        {
            obj.SetActive(true);
        }
    }
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            PassthroughFeature.EnableSeeThroughManual(false);
        }
        else
        {
            PassthroughFeature.EnableSeeThroughManual(true);
        }
    }
}


