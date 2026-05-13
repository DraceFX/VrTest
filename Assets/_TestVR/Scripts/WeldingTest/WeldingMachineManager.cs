using TMPro;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class WeldingMachineManager : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private GameObject _infoCanvas;

    [Header("XR Knob")]
    public XRKnob AmperKnob;
    public XRKnob VoltageKnob;

    [Header("Value Range")]
    public float minAmperValue = 10f;
    public float maxAmperValue = 300f;

    public float minVoltageValue = 10f;
    public float maxVoltageValue = 40f;

    [Header("Optional")]
    public TMP_Text TextInfo;
    public WeldingSettings Amper;
    public WeldingSettings Voltage;

    public bool isMachineReady;

    private bool _welderConnected = false;
    private bool _groundedClampConnected = false;
    private bool _machineEnable = false;

    private void Start()
    {
        InteractionManager.Instance.OnObjectUsed += CablesIsConeccted;
    }

    private void Update()
    {
        if (AmperKnob == null && VoltageKnob == null)
            return;

        float rawAmper = Mathf.Lerp(minAmperValue, maxAmperValue, AmperKnob.value);
        float rawVoltage = Mathf.Lerp(minVoltageValue, maxVoltageValue, VoltageKnob.value);

        int amperRounded = Mathf.RoundToInt(rawAmper);
        int voltageRounded = Mathf.RoundToInt(rawVoltage);

        if (TextInfo != null)
            TextInfo.text = $"A:{amperRounded} V:{voltageRounded}";

        if (Amper != null)
            Amper.OnCurrentChanged(amperRounded);
        if (Voltage != null)
            Voltage.OnVoltageChanged(voltageRounded);
    }

    public void EnableWeldingMachine(bool isEnabled)
    {
        _infoCanvas.SetActive(isEnabled);
        _machineEnable = isEnabled;

        isMachineReady = ReadyToWelding();
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