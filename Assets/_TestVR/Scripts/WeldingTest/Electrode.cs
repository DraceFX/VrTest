using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Electrode : MonoBehaviour
{
    // Эти свойства устанавливаются извне
    public ElectrodeSocket CurrentSocket { get; set; }
    public ElectrodeSocket AttachedSocket { get; set; }

    [Header("Interaction")]
    [SerializeField] private XRGrabInteractable _grabInteractable;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Effects")]
    [SerializeField] private WeldEffectsManager _effect;

    [Header("Welding Settings")]
    public Transform Tip;
    public float WeldDistance = 0.03f;
    public float SearchRadius = 0.03f;

    [Header("Geometry")]
    [SerializeField] private float _length = 1f;

    [Header("Arc Striking")]
    [SerializeField] private float _arcMaxDistance = 0.1f;     // дальность поиска дугового зазора
    [SerializeField] private float _strikeMinGap = 0.006f;      // минимальный дуговой зазор
    [SerializeField] private float _strikeMaxGap = 0.02f;      // максимальный дуговой зазор
    [SerializeField] private Vector3 _arcBoxHalfExtents = new Vector3(0.002f, 0.002f, 0.01f); // форма BoxCast для дуги

    [Header("Sticking")]
    [SerializeField] private float _stickTime = 1f;

    public Rigidbody Rb => _rigidbody;
    public float ArcMaxDistance => _arcMaxDistance;
    public float StrikeMinGap => _strikeMinGap;
    public float StrikeMaxGap => _strikeMaxGap;
    public float StickTime => _stickTime;

    private bool _effectsActive = false;
    private float _currentPower;
    private float _optimalPower;

    private void Awake()
    {
        if (_grabInteractable == null)
            _grabInteractable = GetComponent<XRGrabInteractable>();

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnSelectEntered);
            _grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        // При старте, если электрод не прикреплён — даём ему физику
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
        // Если электрод был в сокете — открепляем
        if (AttachedSocket != null)
        {
            AttachedSocket.DetachElectrode(this);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // 1. Если мы над каким-то сокетом и ещё не прикреплены — пытаемся прикрепиться
        if (CurrentSocket != null && AttachedSocket == null)
        {
            CurrentSocket.TryAttachElectrode(this);
        }

        // 2. Если после попытки электрод всё равно не прикреплён — включаем физику
        if (AttachedSocket == null)
        {
            SetPhysicsEnabled(true);
        }
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = !enabled;
        }
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
        {
            _effect.transform.SetParent(Tip, false);
        }

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

    // Обновляем параметры эффектов каждый кадр
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
        // Сохраняем мировую позицию и поворот перед сменой родителя
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;

        transform.SetParent(parent, true);

        // Принудительно включаем кинематику, даже если до этого была отключена
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

        // ---------- 1. Основной луч (до _arcMaxDistance) ----------
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // полупрозрачный серый
        Gizmos.DrawRay(origin, direction * _arcMaxDistance);

        // ---------- 2. Рабочий дуговой зазор ----------
        Vector3 minGapPoint = origin + direction * _strikeMinGap;
        Vector3 maxGapPoint = origin + direction * _strikeMaxGap;

        // Отрезок между минимальным и максимальным зазором (зелёный)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(minGapPoint, maxGapPoint);

        // Небольшие сферы на границах зазора
        float sphereRadius = 0.0005f; // полмиллиметра – не масштабируется, чисто маркер
        Gizmos.DrawWireSphere(minGapPoint, sphereRadius);
        Gizmos.DrawWireSphere(maxGapPoint, sphereRadius);
    }
}