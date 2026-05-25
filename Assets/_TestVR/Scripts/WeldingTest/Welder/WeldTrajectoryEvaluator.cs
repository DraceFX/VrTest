using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeldPattern
{
    Straight,
    Zigzag,
    Triangle,
    Circle,
    Herringbone,
    Figure8,      // восьмёрка
    CShape,       // полумесяц
    Spiral,
    Sawtooth,
    Square,
    Wave,
    DoubleWave
}

public class WeldTrajectoryEvaluator : MonoBehaviour
{
    [Header("Параметры идеального шва")]
    [SerializeField] private WeldPattern _pattern = WeldPattern.Straight;

    [Tooltip("Амплитуда колебаний (м)")]
    [SerializeField] private float _weaveAmplitude = 0.003f;

    [Tooltip("Частота циклов на метр")]
    [SerializeField] private float _weaveFrequency = 2f;
    [SerializeField] private float _maxLateralError = 0.002f;

    [Header("Настройки оценки")]
    [SerializeField] private float _qualityDecayRate = 2f;
    [SerializeField] private float _smoothTime = 0.05f;

    // Внутреннее состояние
    private Vector3 _seamOrigin;
    private Vector3 _seamDirection;
    private Vector3 _seamNormal;
    private bool _isInitialized = false;

    private bool _directionChecked = false;   // была ли проверка направления
    private const int DIR_CHECK_FRAMES = 20;   // через сколько точек проверять

    private float _currentQuality = 1f;
    private Vector3 _smoothedPosition;
    private Vector3 _velocityRef;

    private List<Vector2> _realProfilePoints = new List<Vector2>();

    public float TrajectoryQuality => _currentQuality;
    public float WeaveAmplitude => _weaveAmplitude;
    public float WeaveFrequency => _weaveFrequency;
    public WeldPattern CurrentPattern => _pattern;

    public void Initialize(Vector3 contactPoint, Vector3 contactNormal, Vector3 weldForward)
    {
        _seamOrigin = contactPoint;
        _seamDirection = weldForward.normalized;
        _seamNormal = contactNormal.normalized;

        _isInitialized = true;
        _currentQuality = 1f;
        _smoothedPosition = contactPoint;

        _realProfilePoints.Clear();
        _realProfilePoints.Add(Vector2.zero);
    }

    public void UpdateTracking(Vector3 electrodeTipPosition)
    {
        if (!_isInitialized) return;

        _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, electrodeTipPosition, ref _velocityRef, _smoothTime);

        // Проверка фактического направления (один раз в начале)
        if (!_directionChecked && _realProfilePoints.Count >= DIR_CHECK_FRAMES)
        {
            _directionChecked = true;
            Vector3 currentDir = (_smoothedPosition - _seamOrigin).normalized;
            float dot = Vector3.Dot(currentDir, _seamDirection);

            // Если движение идёт преимущественно в обратную сторону
            if (dot < -0.5f)
            {
                _seamDirection = -_seamDirection;          // инвертируем ось
                // Сбрасываем накопленные точки и начинаем заново с верным направлением
                _realProfilePoints.Clear();
                _realProfilePoints.Add(Vector2.zero);
                // Текущая позиция станет первой после сброса – она будет учтена ниже
            }
        }

        Vector3 toPoint = _smoothedPosition - _seamOrigin;
        // Продольная координата вдоль шва
        float s = Vector3.Dot(toPoint, _seamDirection);
        // Центральная точка шва
        Vector3 basePoint = _seamOrigin + s * _seamDirection;
        // Боковая ось
        Vector3 lateral = Vector3.Cross(_seamNormal, _seamDirection).normalized;
        // Идеальное смещение
        float idealOffset = ComputeIdealOffset(s);
        Vector3 idealPoint = basePoint + idealOffset * lateral;
        // Ошибка
        Vector3 errorVec = _smoothedPosition - idealPoint;
        float lateralError = Vector3.Dot(errorVec, lateral);
        // Реальная траектория
        float realLateral = Vector3.Dot(toPoint, lateral);
        _realProfilePoints.Add(new Vector2(s, realLateral));

        // Оценка качества
        float absError = Mathf.Abs(lateralError);

        if (absError > _maxLateralError)
        {
            float excess = (absError - _maxLateralError) / _maxLateralError;

            _currentQuality -= excess * _qualityDecayRate * Time.deltaTime;
        }
        else
        {
            _currentQuality += 0.1f * Time.deltaTime;
        }

        _currentQuality = Mathf.Clamp01(_currentQuality);
    }

    /// <summary>
    /// Возвращает боковое смещение электрода
    /// относительно центральной оси шва.
    /// </summary>
    public float ComputeIdealOffset(float s)
    {
        // Нормализованная фаза
        float phase = s * _weaveFrequency;
        float omega = phase * Mathf.PI * 2f;

        switch (_pattern)
        {
            // ----------------------------------------------------
            // Прямая
            // ----------------------------------------------------
            case WeldPattern.Straight:
                return 0f;

            // ----------------------------------------------------
            // Синус
            // ----------------------------------------------------
            case WeldPattern.Wave:
            case WeldPattern.Circle: return _weaveAmplitude * Mathf.Sin(omega);

            // ----------------------------------------------------
            // Двойная волна
            // ----------------------------------------------------
            case WeldPattern.DoubleWave: return _weaveAmplitude * Mathf.Sin(omega) * Mathf.Cos(omega * 0.5f);

            // ----------------------------------------------------
            // Зигзаг
            // ----------------------------------------------------
            case WeldPattern.Zigzag:
                {
                    float t = Mathf.PingPong(phase, 1f);
                    return Mathf.Lerp(-_weaveAmplitude, _weaveAmplitude, t);
                }

            // ----------------------------------------------------
            // Треугольник
            // ----------------------------------------------------
            case WeldPattern.Triangle:
                {
                    float t = phase % 1f;

                    if (t < 0.5f) return Mathf.Lerp(-_weaveAmplitude, _weaveAmplitude, t * 2f);

                    return Mathf.Lerp(_weaveAmplitude, -_weaveAmplitude, (t - 0.5f) * 2f);
                }

            // ----------------------------------------------------
            // Пилообразный
            // ----------------------------------------------------
            case WeldPattern.Sawtooth:
                {
                    float t = phase % 1f;
                    return Mathf.Lerp(-_weaveAmplitude, _weaveAmplitude, t);
                }

            // ----------------------------------------------------
            // Меандр / квадрат
            // ----------------------------------------------------
            case WeldPattern.Square:
                {
                    return Mathf.Sign(Mathf.Sin(omega)) * _weaveAmplitude;
                }

            // ----------------------------------------------------
            // Ёлочка
            // ----------------------------------------------------
            case WeldPattern.Herringbone:
                {
                    float t = phase % 1f;

                    if (t < 0.5f) return _weaveAmplitude * (4f * t - 1f);

                    return _weaveAmplitude * (3f - 4f * t);
                }

            // ----------------------------------------------------
            // Восьмёрка
            // x = sin(t)
            // y = sin(2t)
            // В lateral берём y
            // ----------------------------------------------------
            case WeldPattern.Figure8:
                {
                    return _weaveAmplitude * Mathf.Sin(2f * omega);
                }

            // ----------------------------------------------------
            // Полумесяц / C-shape
            // ----------------------------------------------------
            case WeldPattern.CShape:
                {
                    float c = Mathf.Cos(omega);

                    // только одна сторона
                    return _weaveAmplitude * Mathf.Clamp(c, -0.2f, 1f);
                }

            // ----------------------------------------------------
            // Спираль
            // ----------------------------------------------------
            case WeldPattern.Spiral:
                {
                    float growth = 0.5f + 0.5f * Mathf.Sin(omega * 0.25f);

                    return _weaveAmplitude * growth * Mathf.Sin(omega);
                }

            default: return 0f;
        }
    }

    /// <summary>
    /// Полная 2D траектория.
    /// Используется если нужен не только lateral offset,
    /// а полноценная фигура в плоскости.
    /// </summary>
    public Vector2 ComputePattern2D(float s)
    {
        float phase = s * _weaveFrequency;
        float omega = phase * Mathf.PI * 2f;

        switch (_pattern)
        {
            case WeldPattern.Circle:
                {
                    return new Vector2(Mathf.Cos(omega), Mathf.Sin(omega)) * _weaveAmplitude;
                }

            case WeldPattern.Figure8:
                {
                    // Лиссажу
                    return new Vector2(Mathf.Sin(omega), Mathf.Sin(2f * omega)) * _weaveAmplitude;
                }

            case WeldPattern.Spiral:
                {
                    float r = _weaveAmplitude * (0.5f + 0.5f * Mathf.Sin(omega * 0.2f));

                    return new Vector2(Mathf.Cos(omega), Mathf.Sin(omega)) * r;
                }

            case WeldPattern.CShape:
                {
                    float a = Mathf.Lerp(-Mathf.PI * 0.75f, Mathf.PI * 0.75f, (Mathf.Sin(omega) + 1f) * 0.5f);

                    return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _weaveAmplitude;
                }

            default:
                {
                    return new Vector2(s, ComputeIdealOffset(s));
                }
        }
    }

    public IReadOnlyList<Vector2> GetRecordedProfile()
    {
        return _realProfilePoints;
    }

    public void Reset()
    {
        _isInitialized = false;
        _currentQuality = 1f;
        _realProfilePoints.Clear();
    }
}