using UnityEngine;
using UnityEngine.Events;

public class StepEventListener : MonoBehaviour
{
    [SerializeField] private StepEventSO _event;
    [SerializeField] private UnityEvent _response;

    private void OnEnable()
    {
        if (_event != null)
            _event.OnRaised.AddListener(OnEventRaised);
    }

    private void OnDisable()
    {
        if (_event != null)
            _event.OnRaised.RemoveListener(OnEventRaised);
    }

    private void OnEventRaised()
    {
        _response?.Invoke();
    }
}
