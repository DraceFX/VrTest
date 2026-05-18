using UnityEngine;

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

    private bool _isActivated;

    public void SetActivated(bool state)
    {
        _isActivated = state;
        if (!state)
        {
            _effectController.StopEffects();
            _sessionManager.FinishWeld();
            _contactDetector.ResetTargets();
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

        // Обновление трекера траектории
        if (_isActivated && _trajectoryEvaluator != null)
            _trajectoryEvaluator.UpdateTracking(electrode.Tip.position);

        if (hasContact)
        {
            WeldProcessModel model = _contactDetector.ProcessModel;
            if (model == null) return;

            // Старт сессии, если ещё не начата
            if (!_sessionManager.IsAssemblyCreated)
            {
                Vector3 forwardOnSurface = Vector3.ProjectOnPlane(electrode.Tip.forward, hit.normal).normalized;
                _sessionManager.StartNewWeld(_contactDetector.TargetA, _contactDetector.TargetB, hit.point, hit.normal, forwardOnSurface);
            }

            WeldMeshBuilder builder = _sessionManager.ActiveBuilder;
            if (builder == null) return;

            // Параметры наплавки
            float deposit = model.EvaluateDeposit(power);

            builder.Spacing = Mathf.Lerp(0.006f, 0.002f, Mathf.Clamp01(deposit * 50f));
            builder.AddBead(hit.point, hit.normal);

            // Оценка качества
            float powerQuality = model.EvaluateQuality(power);
            float trajectoryFactor = _trajectoryEvaluator != null ? _trajectoryEvaluator.TrajectoryQuality : 1f;
            float finalQuality = powerQuality * trajectoryFactor;
            float arcDist = Vector3.Distance(electrode.Tip.position, hit.point);
            float idealArc = electrode.WeldDistance * 0.7f;

            _qualityAssessor?.UpdateAssessment(power, model.OptimalPower, electrode.Tip.position, arcDist, idealArc, builder.Spacing);

            // Дефекты
            _defectEngine.ProcessDefects(builder, model, power, finalQuality, hit.point, hit.normal, electrode, hit);

            // Эффекты и расход электрода
            _effectController.StartEffects(electrode, power, model.OptimalPower);
            _effectController.UpdateEffects(power);
            _electrodeConsumer.ConsumeElectrode(electrode, model, power);
        }
        else
        {
            _effectController.StopEffects();
        }
    }

    private bool PrepareToWeld()
    {
        return _isActivated && _settings != null && _weldingMachineManager.IsMachineReady;
    }
}