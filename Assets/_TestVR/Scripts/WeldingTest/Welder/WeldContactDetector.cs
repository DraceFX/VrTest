using UnityEngine;

public class WeldContactDetector : MonoBehaviour
{
    [SerializeField] private bool _debugMode = true;

    public Weldable TargetA { get; private set; }
    public Weldable TargetB { get; private set; }
    public WeldProcessModel ProcessModel => TargetA?.ProcessModel;

    // Пытается обнаружить свариваемую пару. Возвращает true, если контакт возможен.
    public bool Evaluate(Electrode electrode, out RaycastHit hit)
    {
        hit = default;
        if (electrode == null) return false;

        Ray ray = new Ray(electrode.Tip.position, electrode.Tip.forward);
        if (!Physics.Raycast(ray, out hit, electrode.WeldDistance)) return false;

        Weldable a = hit.collider.GetComponent<Weldable>();
        if (a == null) return false;

        if (TargetA != a)
        {
            TargetA = a;
            TargetB = FindNearbyWeldable(hit.point, a, electrode.SearchRadius);
            if (_debugMode)
                Debug.Log($"[WeldContact] Новая цель: A={TargetA?.name}, B={TargetB?.name}");
        }

        if (TargetB == null) return false;
        if (ProcessModel == null)
        {
            Debug.LogWarning("Weldable не содержит WeldProcessModel!");
            return false;
        }
        if (!TargetA.IsGrounded || !TargetB.IsGrounded)
        {
            if (_debugMode) Debug.Log("[WeldContact] Объекты не заземлены");
            return false;
        }
        return true;
    }

    public void ResetTargets()
    {
        TargetA = null;
        TargetB = null;
    }

    private Weldable FindNearbyWeldable(Vector3 point, Weldable ignore, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(point, radius);
        foreach (var col in hits)
        {
            if (col.transform == ignore.transform || col.isTrigger) continue;

            Weldable w = col.GetComponent<Weldable>();
            if (w != null) return w;
        }
        return null;
    }
}