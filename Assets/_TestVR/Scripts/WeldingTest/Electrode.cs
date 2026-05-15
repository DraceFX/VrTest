using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Electrode : MonoBehaviour
{
    // Эти свойства устанавливаются извне
    public ElectrodeSocket CurrentSocket { get; set; }
    public ElectrodeSocket AttachedSocket { get; set; }
    [SerializeField] private XRGrabInteractable _grabInteractable;
    [SerializeField] private WeldEffectsManager _effect;

    public Rigidbody Rb;

    [Header("Geometry")]
    [SerializeField] private float _length = 1f;

    [Header("Welding Source")]
    public Transform Tip;
    public float WeldDistance = 0.03f;

    [Header("Поиск соседней детали")]
    public float _searchRadius = 0.08f;

    private bool _effectsActive = false;
    private float _currentPower;
    private float _optimalPower;

    private void Awake()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnSelectEntering);
            _grabInteractable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnSelectEntering);
            _grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntering(SelectEnterEventArgs args)
    {
        if (AttachedSocket != null)
        {
            AttachedSocket.DetachElectrode(this);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (CurrentSocket != null && AttachedSocket == null)
        {
            CurrentSocket.TryAttachElectrode(this);
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

        // Если эффект не дочерний к tip, переместим его в tip
        if (_effect.transform.parent != Tip)
        {
            _effect.transform.SetParent(Tip, false);   // false = сохраняем локальную позицию (обычно Vector3.zero)
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
}