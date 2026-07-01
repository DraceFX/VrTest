using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Training/Events/Step Event")]
public class StepEventSO : ScriptableObject
{
    public UnityEvent OnRaised;

    public void Raise()
    {
        OnRaised?.Invoke();
    }
}
