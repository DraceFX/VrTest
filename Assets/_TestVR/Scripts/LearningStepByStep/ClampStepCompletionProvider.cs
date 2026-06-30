using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ClampStepCompletionProvider : MonoBehaviour, IStepCompletionProvider
{
    private XRGrabInteractable _grabInteractable;

    public event Action OnCompleted;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable != null)
            _grabInteractable.deactivated.AddListener(OnGrabbed);
    }

    private void OnGrabbed(DeactivateEventArgs args)
    {
        OnCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
            _grabInteractable.deactivated.RemoveListener(OnGrabbed);
    }
}
