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
    public bool IsAssemblyCreated { get; private set; }

    //Начать новый шов (создать сборку и билдер)
    public void StartNewWeld(Weldable a, Weldable b, Vector3 startPoint, Vector3 normal, Vector3 forward)
    {
        if (CurrentAssembly == null)
        {
            if (_debugMode) Debug.Log($"[WeldSession] Создание узла: {a.name} + {b.name}");
            CurrentAssembly = WeldAssembly.Create(a, b, Vector3.zero);
        }
        else
        {
            if (_debugMode) Debug.Log("[WeldSession] Используем существующую сборку для нового шва.");
        }

        IsAssemblyCreated = true;

        GameObject go = Instantiate(_weldMeshPrefab, startPoint, Quaternion.identity);
        ActiveBuilder = go.GetComponent<WeldMeshBuilder>();
        if (ActiveBuilder == null)
        {
            Destroy(go);
            Debug.LogError("Префаб не содержит WeldMeshBuilder!");
            return;
        }

        ActiveBuilder.transform.SetParent(CurrentAssembly.transform, true);

        // Инициализируем системы
        if (_trajectoryEvaluator != null)
            _trajectoryEvaluator.Initialize(startPoint, normal, forward);
        if (_qualityAssessor != null)
            _qualityAssessor.StartAssessment();
    }

    // Завершить текущий шов и сбросить состояние
    public void FinishWeld()
    {
        if (_qualityAssessor != null)
        {
            _qualityAssessor.StopAssessment();
            float finalQuality = _qualityAssessor.OverallQuality;
            Debug.Log($"Шов завершён. Качество: {finalQuality * 100:F1}%");
        }

        if (_trajectoryEvaluator != null)
            _trajectoryEvaluator.Reset();

        // Визуализация профиля из билдера (адаптивная)
        if (_profileVisualizer != null && ActiveBuilder != null)
        {
            var beadPoints = new List<BeadPoint>(ActiveBuilder.BeadPoints);
            if (beadPoints.Count > 1)
            {
                List<Vector2> profile = WeldProfileBuilder.BuildProfile(beadPoints);
                _profileVisualizer.DrawProfile(profile, ActiveBuilder.ActualLength,
                    _trajectoryEvaluator.WeaveAmplitude, _trajectoryEvaluator.CurrentPattern, _trajectoryEvaluator.WeaveFrequency);

                // Оценка точности (RMS) с использованием идеального профиля, построенного в визуализаторе или здесь.
                // Вычислим RMS аналогично, но теперь realProfile — это наш profile (sCoord, отклонение).
                float rms = CalculateRMS(profile, _trajectoryEvaluator);
                float accuracy = Mathf.Clamp01(1f - rms / _trajectoryEvaluator.WeaveAmplitude);
                Debug.Log($"Точность траектории: {accuracy * 100:F1}%");
            }
        }

        IsAssemblyCreated = false;
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
            Destroy(CurrentAssembly.gameObject);
            CurrentAssembly = null;
        }
        ActiveBuilder = null;
        IsAssemblyCreated = false;

        if (_trajectoryEvaluator != null)
            _trajectoryEvaluator.Reset();
        if (_qualityAssessor != null)
            _qualityAssessor.StopAssessment();

        Debug.Log("[WeldSession] Сессия полностью сброшена.");
    }
}