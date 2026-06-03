using TMPro;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class WeldingMachineManager : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private GameObject _infoCanvas;

    [Header("XR Knob")]
    [SerializeField] private XRKnob _amperKnob;
    [SerializeField] private XRKnob _voltageKnob;

    [Header("Value Range")]
    public float MinAmperValue = 10f;
    public float MaxAmperValue = 300f;
    public float MinVoltageValue = 10f;
    public float MaxVoltageValue = 40f;

    [Header("Power Source (IPowerSource)")]
    [SerializeField] private MonoBehaviour _powerSourceComponent;  // перетаскиваем сюда WeldingSettings
    private IPowerSource _powerSource;

    [Header("Optional")]
    [SerializeField] private TMP_Text _textInfo;

    public bool IsMachineReady;

    private bool _welderConnected = false;
    private bool _groundedClampConnected = false;
    private bool _machineEnable = false;

    private void Start()
    {
        _powerSource = _powerSourceComponent as IPowerSource;
        if (_powerSource == null)
            Debug.LogError("PowerSource не реализует IPowerSource");

        InteractionManager.Instance.OnObjectUsed += CablesIsConeccted;
    }

    private void Update()
    {
        if (_amperKnob == null || _voltageKnob == null || _powerSource == null)
            return;

        float rawAmper = Mathf.Lerp(MinAmperValue, MaxAmperValue, _amperKnob.value);
        float rawVoltage = Mathf.Lerp(MinVoltageValue, MaxVoltageValue, _voltageKnob.value);

        int amperRounded = Mathf.RoundToInt(rawAmper);
        int voltageRounded = Mathf.RoundToInt(rawVoltage);

        if (_textInfo != null)
            _textInfo.text = $"A:{amperRounded} V:{voltageRounded}";

        _powerSource.SetCurrent(amperRounded);
        _powerSource.SetVoltage(voltageRounded);
    }

    private void OnDisable()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.OnObjectUsed -= CablesIsConeccted;
    }

    private void OnDestroy()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.OnObjectUsed -= CablesIsConeccted;
    }

    public void EnableWeldingMachine(bool isEnabled)
    {
        _infoCanvas.SetActive(isEnabled);
        _machineEnable = isEnabled;
        IsMachineReady = ReadyToWelding();
    }

    private void CablesIsConeccted(InteractableTrigger trigger)
    {
        if (trigger == null) return;

        if (trigger.Id == "Welder")
            _welderConnected = true;

        if (trigger.Id == "GroundedClamp")
            _groundedClampConnected = true;

        if (_machineEnable)
            IsMachineReady = ReadyToWelding();
    }

    private bool ReadyToWelding()
    {
        if (!_welderConnected || !_groundedClampConnected || !_machineEnable) return false;
        return true;
    }
}