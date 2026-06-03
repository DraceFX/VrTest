using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Welder : MonoBehaviour
{
    [Header("Компоненты (интерфейсы)")]
    [SerializeField] private MonoBehaviour _socketComponent;    // реализует IToolSocket (например, ElectrodeSocket)
    [SerializeField] private WeldingMachineManager _weldingMachineManager;
    [SerializeField] private MonoBehaviour _powerSourceComponent; // реализует IPowerSource (WeldingSettings)

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

    private enum ArcState { Idle, Striking, Welding, Stuck }
    private ArcState _arcState = ArcState.Idle;
    private float _stickTimer = 0f;

    private IToolSocket _socket;
    private IPowerSource _powerSource;

    private void Awake()
    {
        _xrGrab = GetComponent<XRGrabInteractable>();
        _socket = _socketComponent as IToolSocket;
        _powerSource = _powerSourceComponent as IPowerSource;

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
        if (_xrGrab != null)
        {
            _xrGrab.activated.RemoveListener(OnActivated);
            _xrGrab.deactivated.RemoveListener(OnDeactivated);
        }
    }

    private void OnActivated(ActivateEventArgs args) => SetActivated(true);
    private void OnDeactivated(DeactivateEventArgs args) => SetActivated(false);

    public void SetActivated(bool state)
    {
        _isActivated = state;
        if (!state) StopWeld();
    }

    private void Update()
    {
        if (!PrepareToWeld()) return;

        IWeldingTool weldingTool = _socket?.AttachedTool;
        Electrode electrode = weldingTool as Electrode; // кастинг для старых модулей
        if (weldingTool == null)
        {
            _effectController.StopEffects();
            _arcState = ArcState.Idle;
            _stickTimer = 0f;
            return;
        }

        float power = _powerSource != null ? _powerSource.Power : 0f;
        bool hasContact = _contactDetector.Evaluate(electrode, out RaycastHit hit); // модуль пока ждёт Electrode
        bool hasArcHit = weldingTool.TryGetArcContact(out RaycastHit arcHit, out float arcDist);

        // Трекинг траектории
        if (_isActivated && _trajectoryEvaluator != null)
            _trajectoryEvaluator.UpdateTracking(weldingTool.TipPosition);

        // Залипание (только для электрода)
        if (_arcState == ArcState.Striking || _arcState == ArcState.Welding)
        {
            if (hasArcHit && arcDist < weldingTool.StrikeMinGap)
            {
                _stickTimer += Time.deltaTime;
                float stickTime = electrode != null ? electrode.StickTime : float.PositiveInfinity;
                if (_stickTimer >= stickTime)
                {
                    StickElectrode(arcHit);
                    _arcState = ArcState.Stuck;
                    _effectController.StopEffects();
                    return;
                }
            }
            else
            {
                _stickTimer = 0f;
            }
        }
        else
        {
            _stickTimer = 0f;
        }

        // Конечный автомат дуги
        switch (_arcState)
        {
            case ArcState.Idle:
                if (hasContact)
                {
                    _arcState = ArcState.Striking;
                    _stickTimer = 0f;
                }
                else
                    _effectController.StopEffects();
                break;

            case ArcState.Striking:
                if (!hasArcHit || arcDist > weldingTool.ArcMaxDistance)
                {
                    _arcState = ArcState.Idle;
                    _effectController.StopEffects();
                }
                else if (weldingTool.IsInArcGap(arcDist))
                {
                    _arcState = ArcState.Welding;
                    StartWeldSession(arcHit);
                }
                break;

            case ArcState.Welding:
                bool arcStable = hasArcHit && weldingTool.IsInArcGap(arcDist);
                if (!arcStable)
                {
                    StopWeld();
                    _arcState = ArcState.Idle;
                }
                else
                {
                    WeldProcessModel model = _contactDetector.ProcessModel;
                    if (model != null)
                    {
                        TrySetSecondTarget();
                        PerformWelding(weldingTool, model, arcHit, power);
                    }
                }
                break;
        }
    }

    private void StartWeldSession(RaycastHit hit)
    {
        IWeldingTool tool = _socket?.AttachedTool;
        if (tool == null) return;

        Vector3 forwardOnSurface = Vector3.ProjectOnPlane(tool.TipForward, hit.normal).normalized;
        _sessionManager.StartNewWeld(_contactDetector.TargetA, _contactDetector.TargetB, hit.point, hit.normal, forwardOnSurface);
    }

    private void TrySetSecondTarget()
    {
        if (_sessionManager.ActiveTargetB == null && _contactDetector.TargetB != null)
            _sessionManager.SetSecondTarget(_contactDetector.TargetB);
    }

    private void PerformWelding(IWeldingTool tool, WeldProcessModel model, RaycastHit hit, float power)
    {
        WeldMeshBuilder builder = _sessionManager.ActiveBuilder;
        if (builder == null) return;

        float deposit = model.EvaluateDeposit(power);
        builder.Spacing = Mathf.Lerp(0.006f, 0.002f, Mathf.Clamp01(deposit * 50f));
        builder.AddBead(hit.point, hit.normal);

        float powerQuality = model.EvaluateQuality(power);
        float trajectoryFactor = _trajectoryEvaluator != null ? _trajectoryEvaluator.TrajectoryQuality : 1f;
        float finalQuality = powerQuality * trajectoryFactor;
        float arcDistFull = Vector3.Distance(tool.TipPosition, hit.point);
        float idealArc = tool.WeldDistance * 0.7f;

        _qualityAssessor?.UpdateAssessment(power, model.OptimalPower, tool.TipPosition, arcDistFull, idealArc, builder.Spacing);

        // Модули пока требуют Electrode, делаем кастинг
        Electrode electrode = tool as Electrode;
        if (electrode != null)
        {
            _defectEngine.ProcessDefects(builder, model, power, finalQuality, hit.point, hit.normal, electrode, hit);
            _effectController.StartEffects(electrode, power, model.OptimalPower);
            _effectController.UpdateEffects(power);
            _electrodeConsumer.ConsumeElectrode(electrode, model, power);
        }
        else
        {
            // Для других инструментов эффекты и расход можно будет реализовать позже
            Debug.LogWarning("Эффекты и расход не поддерживаются для данного инструмента");
        }
    }

    private void StopWeld()
    {
        _effectController.StopEffects();
        _sessionManager.FinishWeld();
        _contactDetector.ResetTargets();
    }

    private bool PrepareToWeld()
    {
        return _isActivated && _weldingMachineManager.IsMachineReady;
    }

    private void StickElectrode(RaycastHit hit)
    {
        IWeldingTool tool = _socket?.AttachedTool;
        Electrode electrode = tool as Electrode;
        if (electrode == null) return;

        Transform parent = null;
        if (_contactDetector.TargetA != null)
            parent = _contactDetector.TargetA.transform;
        else if (hit.collider != null)
            parent = hit.collider.transform;

        if (parent == null)
        {
            Debug.LogError("Не удалось найти объект для прилипания электрода!");
            return;
        }

        // Отсоединяем электрод через интерфейс сокета
        _socket.Detach(tool);
        // Прилипаем (специфика электрода)
        electrode.StickToSurface(parent);

        StopWeld();
        Debug.Log($"Электрод прилип к {parent.name}");
    }
}