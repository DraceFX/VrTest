using System;
using UnityEngine;

public static class DetectionState
{
    public static DetectionObject CurrentObject { get; private set; }

    public static event Action<DetectionObject> OnChanged;

    public static void Set(DetectionObject obj)
    {
        CurrentObject = obj;
        OnChanged?.Invoke(obj);
    }

    public static void Clear(DetectionObject obj)
    {
        if (CurrentObject == obj)
        {
            CurrentObject = null;
            OnChanged?.Invoke(null);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        CurrentObject = null;
        OnChanged = null;
    }
}
