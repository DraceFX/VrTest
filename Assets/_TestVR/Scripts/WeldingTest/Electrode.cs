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

    public Rigidbody Rb => _rigidbody;

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

    private void OnDrawGizmosSelected()
    {
        if (Tip != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Tip.position, SearchRadius);
        }
    }
}