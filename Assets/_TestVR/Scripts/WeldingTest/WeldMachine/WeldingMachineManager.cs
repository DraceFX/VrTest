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
    public float MminAmperValue = 10f;
    public float MaxAmperValue = 300f;

    public float MinVoltageValue = 10f;
    public float MaxVoltageValue = 40f;

    [Header("Optional")]
    [SerializeField] private TMP_Text _textInfo;
    [SerializeField] private WeldingSettings _amper;
    [SerializeField] private WeldingSettings _voltage;

    public bool IsMachineReady;

    private bool _welderConnected = false;
    private bool _groundedClampConnected = false;
    private bool _machineEnable = false;

    private void Start()
    {
        InteractionManager.Instance.OnObjectUsed += CablesIsConeccted;
    }

    private void Update()
    {
        if (_amperKnob == null && _voltageKnob == null)
            return;

        float rawAmper = Mathf.Lerp(MminAmperValue, MaxAmperValue, _amperKnob.value);
        float rawVoltage = Mathf.Lerp(MinVoltageValue, MaxVoltageValue, _voltageKnob.value);

        int amperRounded = Mathf.RoundToInt(rawAmper);
        int voltageRounded = Mathf.RoundToInt(rawVoltage);

        if (_textInfo != null)
            _textInfo.text = $"A:{amperRounded} V:{voltageRounded}";

        if (_amper != null)
            _amper.OnCurrentChanged(amperRounded);
        if (_voltage != null)
            _voltage.OnVoltageChanged(voltageRounded);
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
        {
            _welderConnected = true;
        }

        if (trigger.Id == "GroundedClamp")
        {
            _groundedClampConnected = true;
        }
    }

    private bool ReadyToWelding()
    {
        if (!_welderConnected && !_groundedClampConnected && !_machineEnable) return false;
        return true;
    }
}