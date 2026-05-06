using UnityEngine;
using UnityEngine.UI;

public class WeldingUI : MonoBehaviour
{
    public WeldingSettings settings;

    public Slider currentSlider;
    public Slider voltageSlider;

    [SerializeField] private Toggle _toggleMask;
    [SerializeField] private Toggle _toggleApron;
    [SerializeField] private Toggle _toggleBoots;

    private void Start()
    {
        currentSlider.onValueChanged.AddListener(v => settings.current = v);
        voltageSlider.onValueChanged.AddListener(v => settings.voltage = v);

        InteractionManager.Instance.OnObjectUsed += HandleObjectUsed;
    }

    private void HandleObjectUsed(InteractableTrigger trigger)
    {
        if (trigger == null) return;

        switch (trigger.Id)
        {
            case "Mask":
                _toggleMask.isOn = true;
                break;

            case "Apron":
                _toggleApron.isOn = true;
                break;

            case "Boots":
                _toggleBoots.isOn = true;
                break;
        }
    }
}