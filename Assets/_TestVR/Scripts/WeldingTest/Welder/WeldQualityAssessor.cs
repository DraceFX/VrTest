
using System.Collections.Generic;
using UnityEngine;

public class WeldQualityAssessor : MonoBehaviour
{
    [Header("Веса метрик (сумма = 1)")]
    [SerializeField] private float _weightTrajectory = 0.25f;
    [SerializeField] private float _weightHeatInput = 0.25f;
    [SerializeField] private float _weightArcStability = 0.15f;
    [SerializeField] private float _weightWidthUniformity = 0.15f;
    [SerializeField] private float _weightDefects = 0.20f;

    [SerializeField] private WeldTrajectoryEvaluator _trajectoryEvaluator;

    // Накопленные данные
    private List<float> _widthSamples = new List<float>();
    private float _totalArcInstability = 0f;
    private int _defectCount = 0;
    private float _totalHeatInputError = 0f;
    private float _elapsedTime = 0f;
    private bool _isWelding = false;

    // Итоговая оценка
    public float OverallQuality { get; private set; }
    public float TrajectoryScore { get; private set; }
    public float HeatInputScore { get; private set; }
    public float ArcStabilityScore { get; private set; }
    public float WidthUniformityScore { get; private set; }
    public float DefectScore { get; private set; }

    public void StartAssessment()
    {
        _widthSamples.Clear();
        _totalArcInstability = 0f;
        _defectCount = 0;
        _totalHeatInputError = 0f;
        _elapsedTime = 0f;
        _isWelding = true;
        OverallQuality = 0f;
        TrajectoryScore = 1f;
        HeatInputScore = 1f;
        ArcStabilityScore = 1f;
        WidthUniformityScore = 1f;
        DefectScore = 1f;
    }

    public void StopAssessment()
    {
        _isWelding = false;
        CalculateFinalScores();
    }

    // Вызывать каждый кадр из Welder
    public void UpdateAssessment(float currentPower, float optimalPower, Vector3 electrodeTipPos, float arcDistance, float idealArcDistance, float currentWidth)
    {
        if (!_isWelding) return;
        _elapsedTime += Time.deltaTime;

        // 1. Траектория – берём готовую из TrajectoryEvaluator
        if (_trajectoryEvaluator != null)
            TrajectoryScore = _trajectoryEvaluator.TrajectoryQuality;
        else
            TrajectoryScore = 1f; // без трекера – идеально

        // 2. Тепловложение: ошибка относительно оптимальной мощности
        float heatError = Mathf.Abs(currentPower - optimalPower) / optimalPower;
        _totalHeatInputError += heatError * Time.deltaTime;

        // 3. Стабильность дуги: отклонение длины дуги от номинала
        float arcError = Mathf.Abs(arcDistance - idealArcDistance) / idealArcDistance;
        _totalArcInstability += arcError * Time.deltaTime;

        // 4. Ширина шва (если есть чем измерять – из трекера или меша)
        if (currentWidth > 0)
            _widthSamples.Add(currentWidth);

        // 5. Дефекты – Welder будет вызывать специальный метод при появлении дефекта
    }

    // Из Welder вызывать при добавлении поры, брызга, прожога
    public void RegisterDefect()
    {
        _defectCount++;
    }

    private void CalculateFinalScores()
    {
        if (_elapsedTime <= 0f) _elapsedTime = 0.001f;

        // Траектория уже готова (средняя за время)
        // Тепловложение: средняя ошибка -> переводим в балл
        float avgHeatError = _totalHeatInputError / _elapsedTime;
        HeatInputScore = Mathf.Clamp01(1f - avgHeatError * 2f); // пример: ошибка 50% -> 0 баллов

        // Дуга: аналогично
        float avgArcError = _totalArcInstability / _elapsedTime;
        ArcStabilityScore = Mathf.Clamp01(1f - avgArcError * 5f);

        // Равномерность ширины: коэффициент вариации
        if (_widthSamples.Count > 1)
        {
            float mean = 0, variance = 0;
            foreach (float w in _widthSamples) mean += w;
            mean /= _widthSamples.Count;
            foreach (float w in _widthSamples) variance += (w - mean) * (w - mean);
            variance /= _widthSamples.Count;
            float cv = Mathf.Sqrt(variance) / mean; // коэффициент вариации
            WidthUniformityScore = Mathf.Clamp01(1f - cv * 3f);
        }
        else WidthUniformityScore = 1f;

        // Дефекты: штраф за количество на единицу длины (нужна длина шва)
        // float weldLength = ...; // можно взять из трекера (s)
        // float defectsPerMeter = weldLength > 0 ? _defectCount / weldLength : 0;
        // DefectScore = Mathf.Clamp01(1f - defectsPerMeter * 10f);

        // Итоговая средневзвешенная
        OverallQuality = TrajectoryScore * _weightTrajectory
                       + HeatInputScore * _weightHeatInput
                       + ArcStabilityScore * _weightArcStability
                       + WidthUniformityScore * _weightWidthUniformity
                       + DefectScore * _weightDefects;
        OverallQuality = Mathf.Clamp01(OverallQuality);
    }
}