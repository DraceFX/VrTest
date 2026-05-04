using System;
using UnityEngine;

public class WeldTorch : MonoBehaviour
{
    public event Action<bool> OnWeldStateChanged;

    [Header("Raycast")]
    public Transform tip;
    public float maxDistance = 0.06f;
    public LayerMask mask = ~0;

    private WeldZone currentZone;

    public void EnterZone(WeldZone zone) => currentZone = zone;

    public void ExitZone(WeldZone zone)
    {
        if (currentZone == zone)
            currentZone = null;
    }

    // Вызывать из XR input (кнопка/триггер)
    public void SetWelding(bool active)
    {
        OnWeldStateChanged?.Invoke(active);
    }

    public bool TryGetHit(out RaycastHit hit)
    {
        return Physics.Raycast(tip.position, tip.forward, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
    }

    public WeldZone CurrentZone => currentZone;
}