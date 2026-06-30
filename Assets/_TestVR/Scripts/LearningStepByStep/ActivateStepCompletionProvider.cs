using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ActivateStepCompletionProvider : MonoBehaviour, IStepCompletionProvider
{
    private XRSimpleInteractable _interactable;

    public event Action OnCompleted;

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        if (_interactable != null)
            _interactable.activated.AddListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        OnCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.activated.RemoveListener(OnActivated);
    }
}
