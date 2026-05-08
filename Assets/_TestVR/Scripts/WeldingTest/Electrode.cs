using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Electrode : MonoBehaviour
{
    // Эти свойства устанавливаются извне
    public ElectrodeSocket CurrentSocket { get; set; }
    public ElectrodeSocket AttachedSocket { get; set; }
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private WeldEffectsManager effect;

    public Rigidbody rb;

    [Header("Geometry")]
    public float length = 1f;

    [Header("Welding Source")]
    public Transform tip;
    public float weldDistance = 0.03f;

    private bool effectsActive = false;
    private float currentPower;
    private float optimalPower;


    private void Awake()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntering);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntering);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
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
        length -= amount;
        length = Mathf.Max(length, 0f);

        transform.localScale = new Vector3(1f, 1f, length);
    }

    public void StartWeldEffects(float power, float optimal)
    {
        if (effect == null) return;

        // Если эффект не дочерний к tip, переместим его в tip
        if (effect.transform.parent != tip)
        {
            effect.transform.SetParent(tip, false);   // false = сохраняем локальную позицию (обычно Vector3.zero)
        }

        currentPower = power;
        optimalPower = optimal;

        effect.Play();
        effectsActive = true;
    }

    public void StopWeldEffects()
    {
        if (effect == null) return;
        effect.Stop();
        effectsActive = false;
    }

    // Обновляем параметры эффектов каждый кадр
    public void UpdateWeldEffects(float power)
    {
        if (!effectsActive || effect == null) return;
        currentPower = power;
        effect.UpdateEffects(currentPower, optimalPower);
    }
}