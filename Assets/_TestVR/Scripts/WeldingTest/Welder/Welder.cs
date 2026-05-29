using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Welder : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private ElectrodeSocket _socket;
    [SerializeField] private WeldingMachineManager _weldingMachineManager;
    [SerializeField] private WeldingSettings _settings;

    [Header("Модули")]
    [SerializeField] private WeldContactDetector _contactDetector;
    [SerializeField] private WeldSessionManager _sessionManager;
    [SerializeField] private WeldDefectEngine _defectEngine;
    [SerializeField] private WeldEffectController _effectController;
    [SerializeField] private WeldElectrodeConsumer _electrodeConsumer;
    [SerializeField] private WeldTrajectoryEvaluator _trajectoryEvaluator;
    [SerializeField] private WeldQualityAssessor _qualityAssessor;

    [SerializeField] private bool _needPressTrigger = true;

    private bool _isActivated;
    private XRGrabInteractable _xrGrab;


    private void Awake()
    {
        _xrGrab = GetComponent<XRGrabInteractable>();

        if (_needPressTrigger)
        {
            _xrGrab.activated.AddListener(OnActivated);
            _xrGrab.deactivated.AddListener(OnDeactivated);
        }
        else
        {
            _isActivated = true;
        }
    }

    private void OnDestroy()
    {
        // Отписка от событий
        if (_xrGrab != null)
        {
            _xrGrab.activated.RemoveListener(OnActivated);
            _xrGrab.deactivated.RemoveListener(OnDeactivated);
        }
    }

    private void OnActivated(ActivateEventArgs args)
    {
        SetActivated(true);
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        SetActivated(false);
    }

    public void SetActivated(bool state)
    {
        _isActivated = state;
        if (!state)
        {
            StopWeld();
        }
    }

    private void Update()
    {
        if (!PrepareToWeld()) return;

        Electrode electrode = _socket?.AttachedElectrode;
        if (electrode == null)
        {
            _effectController.StopEffects();
            return;
        }

        float power = _settings.Power;
        bool hasContact = _contactDetector.Evaluate(electrode, out RaycastHit hit);

        Debug.Log($"hasContact: {hasContact}, TargetA: {_contactDetector.TargetA?.name}, isGrounded: {_contactDetector.TargetA?.IsGrounded}");

        // Трекинг траектории
        if (_isActivated && _trajectoryEvaluator != null)
            _trajectoryEvaluator.UpdateTracking(electrode.Tip.position);

        // =========== Потеря контакта ===========
        if (!hasContact)
        {
            if (_sessionManager.IsSessionActive)
            {
                StopWeld();   // обычная потеря контакта во время сварки
            }
            else
            {
                _effectController.StopEffects();
            }
            return;
        }

        // =========== Контакт есть ===========
        WeldProcessModel model = _contactDetector.ProcessModel;
        if (model == null) return;

        // Старт сессии при первом касании
        if (!_sessionManager.IsSessionActive)
        {
            StartWeldSession(electrode, hit);
        }
        else
        {
            TrySetSecondTarget();
        }

        // =========== Обычная сварка ===========
        PerformWelding(electrode, model, hit, power);
    }

    private void StartWeldSession(Electrode electrode, RaycastHit hit)
    {
        Vector3 forwardOnSurface = Vector3.ProjectOnPlane(electrode.Tip.forward, hit.normal).normalized;
        _sessionManager.StartNewWeld(_contactDetector.TargetA, _contactDetector.TargetB, hit.point, hit.normal, forwardOnSurface);
    }

    private void TrySetSecondTarget()
    {
        if (_sessionManager.ActiveTargetB == null && _contactDetector.TargetB != null)
            _sessionManager.SetSecondTarget(_contactDetector.TargetB);
    }

    private void PerformWelding(Electrode electrode, WeldProcessModel model, RaycastHit hit, float power)
    {
        WeldMeshBuilder builder = _sessionManager.ActiveBuilder;
        if (builder == null) return;

        float deposit = model.EvaluateDeposit(power);
        builder.Spacing = Mathf.Lerp(0.006f, 0.002f, Mathf.Clamp01(deposit * 50f));
        builder.AddBead(hit.point, hit.normal);

        float powerQuality = model.EvaluateQuality(power);
        float trajectoryFactor = _trajectoryEvaluator != null ? _trajectoryEvaluator.TrajectoryQuality : 1f;
        float finalQuality = powerQuality * trajectoryFactor;
        float arcDistFull = Vector3.Distance(electrode.Tip.position, hit.point);
        float idealArc = electrode.WeldDistance * 0.7f;

        _qualityAssessor?.UpdateAssessment(power, model.OptimalPower, electrode.Tip.position, arcDistFull, idealArc, builder.Spacing);

        _defectEngine.ProcessDefects(builder, model, power, finalQuality, hit.point, hit.normal, electrode, hit);

        _effectController.StartEffects(electrode, power, model.OptimalPower);
        _effectController.UpdateEffects(power);
        _electrodeConsumer.ConsumeElectrode(electrode, model, power);
    }

    private void StopWeld()
    {
        _effectController.StopEffects();
        _sessionManager.FinishWeld();
        _contactDetector.ResetTargets();
    }

    private bool PrepareToWeld()
    {
        if (_isActivated && _weldingMachineManager.IsMachineReady) return true;
        return false;
    }
}