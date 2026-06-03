using UnityEngine;

public class WeldContactDetector : MonoBehaviour, IWeldContactDetector
{
    [SerializeField] private bool _debugMode = true;

    [Header("BoxCast Settings")]
    [SerializeField] private Vector3 _boxHalfExtents = new Vector3(0.002f, 0.002f, 0.01f); // тонкая пластина вдоль оси электрода
    [Header("Search")]
    [SerializeField] private float _searchRadius = 0.03f;

    public Weldable TargetA { get; private set; }
    public Weldable TargetB { get; private set; }
    public WeldProcessModel ProcessModel => TargetA?.ProcessModel;

    // Пытается обнаружить свариваемую пару. Возвращает true, если контакт возможен.
    public bool Evaluate(IWeldingTool tool, out RaycastHit hit)
    {
        hit = default;
        if (tool == null) return false;

        Vector3 origin = tool.TipPosition;
        Vector3 direction = tool.TipForward;
        float distance = tool.WeldDistance;

        if (!Physics.BoxCast(origin, _boxHalfExtents, direction, out hit,
            Quaternion.LookRotation(direction), distance))
            return false;

        Weldable a = hit.collider.GetComponent<Weldable>();
        if (a == null) return false;

        if (TargetA != a)
        {
            TargetA = a;
            TargetB = FindNearbyWeldable(hit.point, a, _searchRadius);
            if (_debugMode)
                Debug.Log($"[WeldContact] Новая цель: A={TargetA?.name}, B={TargetB?.name}");
        }

        if (TargetA == null || !TargetA.IsGrounded)
        {
            if (_debugMode) Debug.Log("[WeldContact] Объект A не заземлён");
            return false;
        }

        if (ProcessModel == null)
        {
            Debug.LogWarning("Weldable не содержит WeldProcessModel!");
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