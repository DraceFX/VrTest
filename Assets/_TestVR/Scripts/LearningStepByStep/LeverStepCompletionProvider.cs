using System;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class LeverStepCompletionProvider : MonoBehaviour, IStepCompletionProvider
{
    private XRLever _interactionLever;

    public event Action OnCompleted;

    private void Awake()
    {
        _interactionLever = GetComponent<XRLever>();
        if (_interactionLever != null)
            _interactionLever.onLeverActivate.AddListener(OnActivated);
    }

    private void OnActivated()
    {
        OnCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (_interactionLever != null)
            _interactionLever.onLeverActivate.RemoveListener(OnActivated);
    }
}
