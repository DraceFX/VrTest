using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Electrode : MonoBehaviour, IWeldingTool
{
    public ElectrodeSocket CurrentSocket { get; set; }
    public ElectrodeSocket AttachedSocket { get; set; }

    [Header("Effects")]
    [SerializeField] private WeldEffectsManager _effect;

    [Header("Welding Settings")]
    public Transform Tip;
    [field: SerializeField] public float WeldDistance { get; set; } = 0.03f;
    public float SearchRadius = 0.03f;

    [Header("Geometry")]
    [SerializeField] private float _length = 1f;

    [Header("Arc Striking")]
    [SerializeField] private float _arcMaxDistance = 0.1f;
    [SerializeField] private float _strikeMinGap = 0.006f;
    [SerializeField] private float _strikeMaxGap = 0.02f;
    [SerializeField] private Vector3 _arcBoxHalfExtents = new Vector3(0.002f, 0.002f, 0.01f);

    [Header("Sticking")]
    [SerializeField] private float _stickTime = 1f;

    public Rigidbody Rb => _rigidbody;
    public float ArcMaxDistance => _arcMaxDistance;
    public float StrikeMinGap => _strikeMinGap;
    public float StrikeMaxGap => _strikeMaxGap;
    public float StickTime => _stickTime;

    // ===== Реализация IWeldingTool =====
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rigidbody;
    public Vector3 TipPosition => Tip.position;
    public Vector3 TipForward => Tip.forward;
    public bool IsConsumable => true;
    private bool _effectsActive = false;
    private float _currentPower;
    private float _optimalPower;

    public bool TryGetArcContact(out RaycastHit hit, out float distance)
    {
        // Используем существующий метод для совместимости
        return TryGetArcDistance(out hit, out distance);
    }

    public void Consume(float amount)
    {
        Burn(amount);
    }

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rigidbody = GetComponent<Rigidbody>();

        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnSelectEntered);
            _grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        if (AttachedSocket == null)
            SetPhysicsEnabled(true);
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            _grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        if (AttachedSocket != null)
            AttachedSocket.DetachElectrode(this);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (AttachedSocket != null)
            AttachedSocket.DetachElectrode(this);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (CurrentSocket != null && AttachedSocket == null)
            CurrentSocket.TryAttachElectrode(this);

        if (AttachedSocket == null)
            SetPhysicsEnabled(true);
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (_rigidbody != null)
            _rigidbody.isKinematic = !enabled;
    }

    public void Burn(float amount)
    {
        _length -= amount;
        _length = Mathf.Max(_length, 0f);
        transform.localScale = new Vector3(1f, 1f, _length);
    }

    public void StartWeldEffects(float power, float optimal)
    {
        if (_effect == null) return;
        if (_effect.transform.parent != Tip)
            _effect.transform.SetParent(Tip, false);

        _currentPower = power;
        _optimalPower = optimal;
        _effect.Play();
        _effectsActive = true;
    }

    public void StopWeldEffects()
    {
        if (_effect == null) return;
        _effect.Stop();
        _effectsActive = false;
    }

    public void UpdateWeldEffects(float power)
    {
        if (!_effectsActive || _effect == null) return;
        _currentPower = power;
        _effect.UpdateEffects(_currentPower, _optimalPower);
    }

    public bool TryGetArcDistance(out RaycastHit hit, out float distance)
    {
        if (Tip == null)
        {
            hit = default;
            distance = float.MaxValue;
            return false;
        }

        Vector3 origin = Tip.position;
        Vector3 direction = Tip.forward;

        if (Physics.BoxCast(origin, _arcBoxHalfExtents, direction, out hit, Quaternion.LookRotation(direction), _arcMaxDistance))
        {
            distance = hit.distance;
            return true;
        }
        else
        {
            hit = default;
            distance = float.MaxValue;
            return false;
        }
    }

    public bool IsInArcGap(float distance)
    {
        return distance >= _strikeMinGap && distance <= _strikeMaxGap;
    }

    public void StickToSurface(Transform parent)
    {
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;
        transform.SetParent(parent, true);

        if (_rigidbody != null)
            _rigidbody.isKinematic = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (Tip != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Tip.position, SearchRadius);
        }

        Vector3 origin = Tip.position;
        Vector3 direction = Tip.forward;

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        Gizmos.DrawRay(origin, direction * _arcMaxDistance);

        Vector3 minGapPoint = origin + direction * _strikeMinGap;
        Vector3 maxGapPoint = origin + direction * _strikeMaxGap;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(minGapPoint, maxGapPoint);

        float sphereRadius = 0.0005f;
        Gizmos.DrawWireSphere(minGapPoint, sphereRadius);
        Gizmos.DrawWireSphere(maxGapPoint, sphereRadius);
    }
}