using System;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class KnobStepCompletionProvider : MonoBehaviour, IStepCompletionProvider
{
    private XRKnob _knobInteractable;

    public event Action OnCompleted;

    private void Awake()
    {
        _knobInteractable = GetComponent<XRKnob>();
        if (_knobInteractable != null)
            _knobInteractable.onValueChange.AddListener(OnActivated);
    }

    private void OnActivated(float value)
    {
        OnCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (_knobInteractable != null)
            _knobInteractable.onValueChange.RemoveListener(OnActivated);
    }
}
