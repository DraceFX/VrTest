using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Welder : MonoBehaviour
{
    [Header("Компоненты (интерфейсы)")]
    [SerializeField] private MonoBehaviour _socketComponent;    // IToolSocket
    [SerializeField] private WeldingMachineManager _weldingMachineManager;
    [SerializeField] private MonoBehaviour _powerSourceComponent; // IPowerSource

    [Header("Модули (интерфейсы)")]
    [SerializeField] private MonoBehaviour _contactDetectorComponent;      // IWeldContactDetector
    [SerializeField] private MonoBehaviour _sessionManagerComponent;       // IWeldSessionManager
    [SerializeField] private MonoBehaviour _defectEngineComponent;         // IWeldDefectEngine
    [SerializeField] private MonoBehaviour _effectControllerComponent;     // IWeldEffectController
    [SerializeField] private MonoBehaviour _consumableConsumerComponent;   // IWeldConsumableConsumer
    [SerializeField] private MonoBehaviour _trajectoryEvaluatorComponent;  // IWeldTrajectoryEvaluator
    [SerializeField] private MonoBehaviour _qualityAssessorComponent;      // IWeldQualityAssessor

    [SerializeField] private bool _needPressTrigger = true;

    private bool _isActivated;
    private bool _hasBeenWelding = false;
    private XRGrabInteractable _xrGrab;

    private enum ArcState { Idle, Striking, Welding, Stuck }
    private ArcState _arcState = ArcState.Idle;
    private float _stickTimer = 0f;

    private IToolSocket _socket;
    private IPowerSource _powerSource;
    private IWeldContactDetector _contactDetector;
    private IWeldSessionManager _sessionManager;
    private IWeldDefectEngine _defectEngine;
    private IWeldEffectController _effectController;
    private IWeldConsumableConsumer _consumableConsumer;
    private IWeldTrajectoryEvaluator _trajectoryEvaluator;
    private IWeldQualityAssessor _qualityAssessor;

    private void OnValidate()
    {
        if (_socketComponent != null && !(_socketComponent is IToolSocket))
            _socketComponent = null;

        if (_powerSourceComponent != null && !(_powerSourceComponent is IPowerSource))
            _powerSourceComponent = null;

        if (_contactDetectorComponent != null && !(_contactDetectorComponent is IWeldContactDetector))
            _contactDetectorComponent = null;

        if (_sessionManagerComponent != null && !(_sessionManagerComponent is IWeldSessionManager))
            _sessionManagerComponent = null;

        if (_defectEngineComponent != null && !(_defectEngineComponent is IWeldDefectEngine))
            _defectEngineComponent = null;

        if (_effectControllerComponent != null && !(_effectControllerComponent is IWeldEffectController))
            _effectControllerComponent = null;

        if (_consumableConsumerComponent != null && !(_consumableConsumerComponent is IWeldConsumableConsumer))
            _consumableConsumerComponent = null;

        if (_trajectoryEvaluatorComponent != null && !(_trajectoryEvaluatorComponent is IWeldTrajectoryEvaluator))
            _trajectoryEvaluatorComponent = null;

        if (_qualityAssessorComponent != null && !(_qualityAssessorComponent is IWeldQualityAssessor))
            _qualityAssessorComponent = null;
    }

    private void Awake()
    {
        _xrGrab = GetComponent<XRGrabInteractable>();

        _socket = _socketComponent as IToolSocket;
        _powerSource = _powerSourceComponent as IPowerSource;
        _contactDetector = _contactDetectorComponent as IWeldContactDetector;
        _sessionManager = _sessionManagerComponent as IWeldSessionManager;
        _defectEngine = _defectEngineComponent as IWeldDefectEngine;
        _effectController = _effectControllerComponent as IWeldEffectController;
        _consumableConsumer = _consumableConsumerComponent as IWeldConsumableConsumer;
        _trajectoryEvaluator = _trajectoryEvaluatorComponent as IWeldTrajectoryEvaluator;
        _qualityAssessor = _qualityAssessorComponent as IWeldQualityAssessor;

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

        IWeldingTool tool = _socket?.AttachedTool;
        if (tool == null)
        {
            _effectController?.StopEffects();
            _arcState = ArcState.Idle;
            _stickTimer = 0f;
            return;
        }

        float power = _powerSource?.Power ?? 0f;

        // Важнейший вызов – детектор целей (должен быть каждый кадр)
        bool hasContact = _contactDetector?.Evaluate(tool, out RaycastHit hit) ?? false;
        bool hasArcHit = tool.TryGetArcContact(out RaycastHit arcHit, out float arcDist);

        // Трекинг траектории
        if (_isActivated && _trajectoryEvaluator != null)
            _trajectoryEvaluator.UpdateTracking(tool.TipPosition);

        // --- Зона залипания ---
        bool inStickZone = hasArcHit && tool.IsInStickZone(arcDist);

        // Таймер залипания запускается ТОЛЬКО если мы уже были в режиме Welding
        if (inStickZone)
        {
            _stickTimer += Time.deltaTime;
            float stickTime = tool is Electrode electrode ? electrode.StickTime : float.PositiveInfinity;
            if (_stickTimer >= stickTime)
            {
                StickElectrode(arcHit);
                _arcState = ArcState.Stuck;
                tool.IsArcStruck = false;
                tool.StopWeldEffects();
                _effectController?.StopEffects();
                return;
            }

        }
        else
        {
            _stickTimer = 0f;
        }

        // --- Конечный автомат дуги ---
        switch (_arcState)
        {
            case ArcState.Idle:
                // Первое касание – зажигаем дугу
                if (hasArcHit && tool.IsInStickZone(arcDist))
                {
                    _arcState = ArcState.Striking;
                    _stickTimer = 0f;
                    _hasBeenWelding = false;          // новое зажигание – забываем историю
                    tool.IsArcStruck = true;
                    WeldProcessModel model = _contactDetector?.ProcessModel;
                    float optimal = model != null ? model.OptimalPower : power;
                    tool.StartWeldEffects(power, optimal);
                    _effectController?.StartEffects(tool, power, optimal);
                }
                else
                {
                    _effectController?.StopEffects();
                    tool.StopWeldEffects();
                    tool.IsArcStruck = false;
                }
                break;

            case ArcState.Striking:
                bool lostContact = !hasArcHit || arcDist > tool.ArcMaxDistance;
                if (lostContact)
                {
                    _arcState = ArcState.Idle;
                    _effectController?.StopEffects();
                    tool.StopWeldEffects();
                    tool.IsArcStruck = false;
                    _hasBeenWelding = false;          // потеря дуги – сбрасываем флаг
                }
                else if (tool.IsInArcGap(arcDist))   // вошли в рабочий зазор
                {
                    _arcState = ArcState.Welding;
                    _hasBeenWelding = true;           // теперь риск залипания активирован
                    StartWeldSession(arcHit);        // используем хит от электрода, а не детектора
                }
                break;

            case ArcState.Welding:
                bool arcStable = hasArcHit && tool.IsInArcGap(arcDist);
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
                        PerformWelding(tool, model, arcHit, power);
                    }
                }
                break;

            case ArcState.Stuck:
                break;
        }
    }

    private void StartWeldSession(RaycastHit hit)
    {
        IWeldingTool tool = _socket?.AttachedTool;
        if (tool == null) return;

        Vector3 forwardOnSurface = Vector3.ProjectOnPlane(tool.TipForward, hit.normal).normalized;
        _sessionManager?.StartNewWeld(_contactDetector.TargetA, _contactDetector.TargetB, hit.point, hit.normal, forwardOnSurface);
    }

    private void TrySetSecondTarget()
    {
        if (_sessionManager != null && _sessionManager.ActiveTargetB == null && _contactDetector?.TargetB != null)
            _sessionManager.SetSecondTarget(_contactDetector.TargetB);
    }

    private void PerformWelding(IWeldingTool tool, WeldProcessModel model, RaycastHit hit, float power)
    {
        WeldMeshBuilder builder = _sessionManager?.ActiveBuilder;
        if (builder == null) return;

        float deposit = model.EvaluateDeposit(power);

        builder.Spacing = Mathf.Lerp(0.006f, 0.002f, Mathf.Clamp01(deposit * 50f));
        builder.AddBead(hit.point, hit.normal);

        float powerQuality = model.EvaluateQuality(power);
        float trajectoryFactor = _trajectoryEvaluator?.TrajectoryQuality ?? 1f;
        float finalQuality = powerQuality * trajectoryFactor;
        float arcDistFull = Vector3.Distance(tool.TipPosition, hit.point);
        float idealArc = tool.WeldDistance * 0.7f;

        _qualityAssessor?.UpdateAssessment(power, model.OptimalPower, tool.TipPosition, arcDistFull, idealArc, builder.Spacing);
        _defectEngine?.ProcessDefects(builder, model, power, finalQuality, hit.point, hit.normal, tool, hit);
        _effectController?.StartEffects(tool, power, model.OptimalPower);
        _effectController?.UpdateEffects(power);
        _consumableConsumer?.Consume(tool, model, power);
    }

    private void StopWeld()
    {
        _effectController?.StopEffects();
        _sessionManager?.FinishWeld();
        _contactDetector?.ResetTargets();
        _hasBeenWelding = false;   // после остановки шва забываем, что мы уже варили
    }

    private bool PrepareToWeld()
    {
        return _isActivated && _weldingMachineManager.IsMachineReady;
    }

    private void StickElectrode(RaycastHit hit)
    {
        IWeldingTool tool = _socket?.AttachedTool;
        if (tool is not Electrode electrode) return;

        Transform parent = null;
        if (_contactDetector?.TargetA != null)
            parent = _contactDetector.TargetA.transform;
        else if (hit.collider != null)
            parent = hit.collider.transform;

        if (parent == null)
        {
            Debug.LogError("Не удалось найти объект для прилипания электрода!");
            return;
        }

        _socket?.Detach(tool);
        electrode.StickToSurface(parent);

        StopWeld();
        Debug.Log($"Электрод прилип к {parent.name}");
    }
}