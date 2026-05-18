using System;
using UnityEngine;

public enum WeldPattern
{
    Straight,       // без колебаний
    Zigzag,         // треугольная волна
    Circle,         // круговые петли
    Herringbone     // ёлочка
}

public class WeldTrajectoryEvaluator : MonoBehaviour
{
    [Header("Параметры идеального шва")]
    [SerializeField] private WeldPattern _pattern = WeldPattern.Straight;
    [SerializeField] private float _weaveAmplitude = 0.003f;   // амплитуда узора, м
    [SerializeField] private float _weaveFrequency = 2f;       // циклов на метр
    [SerializeField] private float _maxLateralError = 0.002f;  // допустимое отклонение, м

    [Header("Настройки оценки")]
    [SerializeField] private float _qualityDecayRate = 2f;     // скорость падения качества при ошибке
    [SerializeField] private float _smoothTime = 0.05f;        // сглаживание для фильтрации дрожания

    // Внутреннее состояние
    private Vector3 _seamOrigin;          // точка начала шва (мировая)
    private Vector3 _seamDirection;       // направление шва (единичный вектор)
    private Vector3 _seamNormal;          // нормаль к поверхности (для бокового смещения)
    private bool _isInitialized = false;

    private float _currentQuality = 1f;   // текущее качество траектории (0..1)
    private Vector3 _smoothedPosition;    // сглаженная позиция электрода
    private Vector3 _velocityRef;         // для SmoothDamp

    /// <summary>Публичное качество траектории (читается из Welder).</summary>
    public float TrajectoryQuality => _currentQuality;

    /// <summary>
    /// Инициализация из первого контакта — вызывается при старте сварки.
    /// Передаётся точка касания и нормаль, чтобы задать систему координат шва.
    /// </summary>
    public void Initialize(Vector3 contactPoint, Vector3 contactNormal, Vector3 weldForward)
    {
        _seamOrigin = contactPoint;
        _seamDirection = weldForward.normalized;
        _seamNormal = contactNormal.normalized;
        _isInitialized = true;
        _currentQuality = 1f;
        _smoothedPosition = contactPoint;
    }

    /// <summary>
    /// Вызывается каждый кадр, пока сварка активна.
    /// </summary>
    public void UpdateTracking(Vector3 electrodeTipPosition)
    {
        if (!_isInitialized) return;

        // Сглаживание для подавления XR-дрожания
        _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, electrodeTipPosition, ref _velocityRef, _smoothTime);

        // Проецируем сглаженную позицию на ось шва: находим s
        Vector3 toPoint = _smoothedPosition - _seamOrigin;
        float s = Vector3.Dot(toPoint, _seamDirection);
        Vector3 basePoint = _seamOrigin + s * _seamDirection;

        // Боковой вектор (перпендикуляр к направлению шва и нормали)
        Vector3 lateral = Vector3.Cross(_seamNormal, _seamDirection).normalized;

        // Идеальное поперечное смещение в зависимости от узора
        float idealOffset = ComputeIdealOffset(s);
        Vector3 idealPoint = basePoint + idealOffset * lateral;

        // Фактическая позиция электрода
        Vector3 realPoint = _smoothedPosition;

        // Ошибка — расстояние между реальной точкой и идеальной (или только поперечная компонента)
        Vector3 errorVec = realPoint - idealPoint;
        // Можно оставить полное расстояние, но чаще важна боковая ошибка:
        float lateralError = Vector3.Dot(errorVec, lateral);

        // Абсолютное отклонение (можно использовать модуль боковой ошибки)
        float absError = Mathf.Abs(lateralError);

        // Превышение допустимого порога
        if (absError > _maxLateralError)
        {
            // Снижаем качество пропорционально превышению (с накоплением)
            float excess = (absError - _maxLateralError) / _maxLateralError;
            _currentQuality -= excess * _qualityDecayRate * Time.deltaTime;
        }
        else
        {
            // Медленное восстановление качества, если всё хорошо (опционально)
            _currentQuality += 0.1f * Time.deltaTime;
        }

        _currentQuality = Mathf.Clamp01(_currentQuality);
    }

    private float ComputeIdealOffset(float s)
    {
        switch (_pattern)
        {
            case WeldPattern.Straight:
                return 0f;
            case WeldPattern.Zigzag:
                // Треугольная волна: используем (s * freq) % 1
                float t = (s * _weaveFrequency) % 1f;
                // Смещение линейно от -A до +A
                return _weaveAmplitude * (2f * t - 1f);
            case WeldPattern.Circle:
                // Синус для плавных круговых движений
                return _weaveAmplitude * Mathf.Sin(s * _weaveFrequency * 2f * Mathf.PI);
            case WeldPattern.Herringbone:
                // Ёлочка: пилообразная волна с резкими переходами
                float phase = (s * _weaveFrequency) % 1f;
                if (phase < 0.5f)
                    return _weaveAmplitude * (4f * phase - 1f);
                else
                    return _weaveAmplitude * (3f - 4f * phase);
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Сброс перед новым швом.
    /// </summary>
    public void Reset()
    {
        _isInitialized = false;
        _currentQuality = 1f;
    }
}