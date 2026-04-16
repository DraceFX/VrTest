using System;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    public event Action<InteractableTrigger> OnObjectUsed;

    private void Awake()
    {
        Instance = this;
    }

    public void NotifyUsed(InteractableTrigger trigger)
    {
        OnObjectUsed?.Invoke(trigger);
    }
}
