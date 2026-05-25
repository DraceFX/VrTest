using System.Collections.Generic;
using UnityEngine;

public class WeldSessionManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private GameObject _weldMeshPrefab;
    [SerializeField] private WeldTrajectoryEvaluator _trajectoryEvaluator;
    [SerializeField] private WeldQualityAssessor _qualityAssessor;
    [SerializeField] private WeldProfileVisualizer _profileVisualizer;
    [SerializeField] private bool _debugMode = true;

    public WeldAssembly CurrentAssembly { get; private set; }
    public WeldMeshBuilder ActiveBuilder { get; private set; }
    public bool IsAssemblyCreated => CurrentAssembly != null;
    public bool IsSessionActive => _isSessionActive;

    private bool _isSessionActive = false;
    private Weldable _pendingA;
    private Weldable _pendingB;

    //Начать новый шов (создать сборку и билдер)
    public void StartNewWeld(Weldable a, Weldable b, Vector3 startPoint, Vector3 normal, Vector3 forward)
    {
        // Проверка входных данных
        if (a == null || b == null)
        {
            Debug.LogError("[WeldSession] StartNewWeld: один из Weldable равен null!");
            return;
        }

        // Проверка префаба меша
        if (_weldMeshPrefab == null)
        {
            Debug.LogError("[WeldSession] _weldMeshPrefab не назначен в инспекторе!");
            return;
        }

        if (_weldMeshPrefab == null)
        {
            Debug.LogError("[WeldSession] Префаб шва (_weldMeshPrefab) не назначен!");
            return;
        }

        // Сохраняем объекты для последующего создания WeldAssembly
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

        // Инициализируем системы
        if (_trajectoryEvaluator != null)
            _trajectoryEvaluator.Initialize(startPoint, normal, forward);
        if (_qualityAssessor != null)
            _qualityAssessor.StartAssessment();

        _isSessionActive = true;

        if (_debugMode) Debug.Log($"[WeldSession] Начат шов между {a.name} и {b.name}");
    }

    // Завершить текущий шов и сбросить состояние
    public void FinishWeld()
    {
        float finalQuality = 0f;

        // Получаем итоговое качество от асессора
        if (_qualityAssessor != null)
        {
            _qualityAssessor.StopAssessment();
            finalQuality = _qualityAssessor.OverallQuality;
            Debug.Log($"Шов завершён. Качество: {finalQuality * 100:F1}%");
        }

        // Сбрасываем оценщик траектории
        _trajectoryEvaluator?.Reset();

        // Объединяем меш шва (если билдер существует)
        if (ActiveBuilder != null)
            ActiveBuilder.CombineAll();

        // Создаём физическое соединение с прочностью, зависящей от качества
        if (_pendingA != null && _pendingB != null)
        {
            CurrentAssembly = WeldAssembly.Create(_pendingA, _pendingB, finalQuality, ActiveBuilder?.gameObject);
            // Меш уже является дочерним к _pendingA.transform, дополнительных действий не требуется
        }
        else
        {
            Debug.LogError("[WeldSession] Нет ссылок на свариваемые объекты при завершении шва.");
            if (ActiveBuilder != null)
                Destroy(ActiveBuilder.gameObject);
        }

        // Визуализация профиля (при наличии визуализатора и данных)
        if (_profileVisualizer != null && ActiveBuilder != null)
        {
            var beadPoints = new List<BeadPoint>(ActiveBuilder.BeadPoints);
            if (beadPoints.Count > 1 && _trajectoryEvaluator != null)
            {
                List<Vector2> profile = WeldProfileBuilder.BuildProfile(beadPoints);
                _profileVisualizer.DrawProfile(profile, ActiveBuilder.ActualLength,
                    _trajectoryEvaluator.WeaveAmplitude, _trajectoryEvaluator.CurrentPattern, _trajectoryEvaluator.WeaveFrequency);

                float rms = CalculateRMS(profile, _trajectoryEvaluator);
                float accuracy = Mathf.Clamp01(1f - rms / _trajectoryEvaluator.WeaveAmplitude);
                Debug.Log($"Точность траектории: {accuracy * 100:F1}%");
            }
        }

        // Сброс состояния сессии
        ActiveBuilder = null;
        _pendingA = null;
        _pendingB = null;
        _isSessionActive = false;
    }

    private float CalculateRMS(List<Vector2> realProfile, WeldTrajectoryEvaluator evaluator)
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
            Destroy(CurrentAssembly);   // удаляет компонент и Joint
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