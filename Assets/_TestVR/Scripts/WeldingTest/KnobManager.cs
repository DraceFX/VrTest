using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;

public class XRKnobValueBinder : MonoBehaviour
{
    [Header("XR Knob")]
    public XRKnob knob;

    [Header("Value Range")]
    public float minValue = 10f;
    public float maxValue = 300f;

    [Header("Optional")]
    public Slider slider;
    public WeldingSettings Amper;
    public WeldingSettings Voltage;

    [Header("Events")]
    public UnityEvent<float> onValueChanged;

    [Header("Debug")]
    [SerializeField] private float currentValue;

    private void Update()
    {
        if (knob == null)
            return;

        // XRKnob.value всегда 0..1
        currentValue = Mathf.Lerp(minValue, maxValue, knob.value);

        // Обновляем slider если есть
        if (slider != null)
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = currentValue;
        }

        if (Amper != null)
        {
            Amper.OnCurrentChanged(currentValue);
        }
        if (Voltage != null)
        {
            Voltage.OnVoltageChanged(currentValue);
        }

        // Событие
        onValueChanged?.Invoke(currentValue);
    }

    public float Value => currentValue;
}