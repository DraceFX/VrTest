using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabStepCompletionProvider : MonoBehaviour, IStepCompletionProvider
{
    private XRGrabInteractable _grabInteractable;

    public event Action OnCompleted;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable != null)
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        OnCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }
}
