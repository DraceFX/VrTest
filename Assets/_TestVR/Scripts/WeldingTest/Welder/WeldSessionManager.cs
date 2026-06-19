using System.Collections.Generic;
using UnityEngine;

public class WeldSessionManager : MonoBehaviour, IWeldSessionManager
{
    [Header("Зависимости")]
    [SerializeField] private GameObject _weldMeshPrefab;
    [SerializeField] private MonoBehaviour _trajectoryEvaluatorComponent;
    [SerializeField] private MonoBehaviour _qualityAssessorComponent;
    [SerializeField] private WeldProfileVisualizer _profileVisualizer;
    [SerializeField] private PercentText _percentText;

    [SerializeField] private bool _debugMode = true;

    private IWeldTrajectoryEvaluator _trajectoryEvaluator;
    private IWeldQualityAssessor _qualityAssessor;

    public WeldAssembly CurrentAssembly { get; private set; }
    public WeldMeshBuilder ActiveBuilder { get; private set; }
    public bool IsAssemblyCreated => CurrentAssembly != null;
    public bool IsSessionActive => _isSessionActive;
    public Weldable ActiveTargetA => _pendingA;
    public Weldable ActiveTargetB => _pendingB;

    private bool _isSessionActive = false;
    private Weldable _pendingA;
    private Weldable _pendingB;

    private void Awake()
    {
        _qualityAssessor = _qualityAssessorComponent as IWeldQualityAssessor;
        _trajectoryEvaluator = _trajectoryEvaluatorComponent as IWeldTrajectoryEvaluator;
    }

    //Начать новый шов (создать сборку и билдер)
    public void StartNewWeld(Weldable a, Weldable b, Vector3 startPoint, Vector3 normal, Vector3 forward)
    {
        if (a == null)
        {
            Debug.LogError("[WeldSession] StartNewWeld: Weldable A равен null!");
            return;
        }

        if (_weldMeshPrefab == null)
        {
            Debug.LogError("[WeldSession] _weldMeshPrefab не назначен в инспекторе!");
            return;
        }

        _pendingA = a;
        _pendingB = b;
        CurrentAssembly = null;

        GameObject go = Instantiate(_weldMeshPrefab, startPoint, Quaternion.identity);
        ActiveBuilder = go.GetComponent<WeldMeshBuilder>();
        if (ActiveBuilder == null)
        {
            Destroy(go);
            Debug.LogError("[WeldSession] Префаб не содержит WeldMeshBuilder!");
            return;
        }

        go.transform.SetParent(a.transform, worldPositionStays: true);

        _trajectoryEvaluator?.Initialize(startPoint, normal, forward);
        _qualityAssessor?.StartAssessment();

        _isSessionActive = true;
        if (_debugMode) Debug.Log($"[WeldSession] Начат шов между {a.name} и {b.name}");
    }

    public void SetSecondTarget(Weldable b)
    {
        if (!_isSessionActive)
        {
            Debug.LogWarning("[WeldSession] Попытка установить TargetB без активной сессии");
            return;
        }
        if (b == null) return;
        _pendingB = b;
        if (_debugMode) Debug.Log($"[WeldSession] TargetB установлен: {b.name}");
    }

    // Завершить текущий шов и сбросить состояние
    public void FinishWeld()
    {
        // 1. Качество от асессора
        float finalQuality = 0f;
        if (_qualityAssessor != null)
        {
            _qualityAssessor.StopAssessment();
            finalQuality = _qualityAssessor.OverallQuality;
        }

        _trajectoryEvaluator?.Reset();
        ActiveBuilder?.CombineAll();

        // 2. Точность траектории — рассчитываем всегда, если есть данные
        float accuracy = 1f;
        List<BeadPoint> beadPoints = ActiveBuilder != null ? new List<BeadPoint>(ActiveBuilder.BeadPoints) : null;

        if (beadPoints != null && beadPoints.Count > 1 && _trajectoryEvaluator != null)
        {
            List<Vector2> profile = WeldProfileBuilder.BuildProfile(beadPoints);
            float rms = CalculateRMS(profile, _trajectoryEvaluator);
            float amplitude = _trajectoryEvaluator.WeaveAmplitude;
            if (amplitude > 0.001f)
                accuracy = Mathf.Clamp01(1f - rms / amplitude);
        }

        // 3. Прочность шва — простое среднее
        float strength = (finalQuality + accuracy) / 2f;

        // 4. Создаём/обновляем соединение с прочностью
        if (_pendingA != null && _pendingB != null)
        {
            CurrentAssembly = WeldAssembly.Create(_pendingA, _pendingB, strength,
                                                  ActiveBuilder?.gameObject);
        }
        else
        {
            Debug.LogError("[WeldSession] Односторонний шов завершён без физического соединения.");
        }

        // 5. Визуализация профиля
        if (_profileVisualizer != null && ActiveBuilder != null && beadPoints != null && beadPoints.Count > 1 && _trajectoryEvaluator != null)
        {
            List<Vector2> profile = WeldProfileBuilder.BuildProfile(beadPoints);
            WeldPattern pattern = (_trajectoryEvaluator as WeldTrajectoryEvaluator)?.CurrentPattern ?? WeldPattern.Straight;

            _profileVisualizer.DrawProfile(profile, ActiveBuilder.ActualLength, _trajectoryEvaluator.WeaveAmplitude, pattern, _trajectoryEvaluator.WeaveFrequency);
        }

        // 6. Вывод результатов
        string qualityPercent = $"{finalQuality * 100:F1}%";
        string accuracyPercent = $"{accuracy * 100:F1}%";
        string strengthPercent = $"{strength * 100:F1}%";

        if (_debugMode)
        {
            Debug.Log($"[WeldSession] Качество: {qualityPercent}, " +
                      $"Точность: {accuracyPercent}, Прочность: {strengthPercent}");
        }

        // Заполняем UI, если поля назначены
        _percentText.SetText($"Качество: {qualityPercent}\n" +
                            $"Точность: {accuracyPercent}\n" +
                            $"Прочтоность шва: {strengthPercent}");

        // Сброс состояния
        ActiveBuilder = null;
        _pendingA = null;
        _pendingB = null;
        _isSessionActive = false;
    }

    private float CalculateRMS(List<Vector2> realProfile, IWeldTrajectoryEvaluator evaluator)
    {
        if (realProfile.Count < 2) return 0f;
        float sumSq = 0f;
        int count = 0;
        foreach (Vector2 p in realProfile)
        {
            float ideal = evaluator.ComputeIdealOffset(p.x);
            float err = p.y - ideal;
            sumSq += err * err;
            count++;
        }
        return Mathf.Sqrt(sumSq / count);
    }

    public void ResetSession()
    {
        if (CurrentAssembly != null)
        {
            Destroy(CurrentAssembly);
            CurrentAssembly = null;
        }

        if (ActiveBuilder != null)
        {
            Destroy(ActiveBuilder.gameObject);
            ActiveBuilder = null;
        }

        _pendingA = null;
        _pendingB = null;
        _isSessionActive = false;

        _trajectoryEvaluator?.Reset();
        _qualityAssessor?.StopAssessment();

        if (_debugMode) Debug.Log("[WeldSession] Сессия полностью сброшена.");
    }
}