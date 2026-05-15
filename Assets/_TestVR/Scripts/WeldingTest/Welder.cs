using UnityEngine;

public class Welder : MonoBehaviour
{
    [Header("Сокет держателя")]
    [SerializeField] private ElectrodeSocket _socket;

    [Header("Настройки сварки")]
    [SerializeField] private WeldingMachineManager _weldingMachineManager;
    [SerializeField] private WeldingSettings _settings;

    [Header("Генератор меша шва (префаб)")]
    [SerializeField] private GameObject _weldMeshPrefab;

    [SerializeField] private bool _debugMode = true;

    private bool _isActivated = false;
    private bool _isAssemblyCreated = false;
    private bool _effectsPlaying = false;

    private Weldable _targetA;
    private Weldable _targetB;

    private WeldAssembly _currentAssembly;
    private WeldMeshBuilder _activeBuilder;

    // Текущий активный электрод и флаг эффектов
    private Electrode _currentElectrode;

    public void SetActivated(bool state)
    {
        _isActivated = state;

        // При отжатии кнопки сразу останавливаем эффекты и сбрасываем контакт
        if (!state)
        {
            StopEffectsIfNeeded();
            _targetA = null;
            _targetB = null;
        }
    }

    private void FinishWeldSession()
    {
        if (_activeBuilder != null)
        {
            // Если билдер — временный объект, просто уничтожаем
            if (Application.isPlaying) Destroy(_activeBuilder.gameObject);
            else DestroyImmediate(_activeBuilder.gameObject);
            _activeBuilder = null;
        }

        _isAssemblyCreated = false;
        _currentAssembly = null;
    }

    private void Update()
    {
        if (!PrepareToWeld()) return;

        Electrode electrode = _socket?.AttachedElectrode;
        if (electrode == null)
        {
            // Электрод отсутствует — выключаем эффекты
            StopEffectsIfNeeded();
            _currentElectrode = null;
            return;
        }

        // Обновили электрод — если сменился, переносим управление эффектами
        if (_currentElectrode != electrode)
        {
            StopEffectsIfNeeded();      // остановить на старом
            _currentElectrode = electrode;
        }

        float power = _settings.Power;
        bool hasContact = TryWeld(electrode, power);

        // Управление эффектами: включаем только при успешном контакте, иначе выключаем
        if (hasContact)
        {
            WeldProcessModel model = _targetA?.ProcessModel;
            if (model == null) return;
            if (!_effectsPlaying)
            {
                _currentElectrode?.StartWeldEffects(power, model.OptimalPower);
                HandleElectrode(electrode, power);
                _effectsPlaying = true;
            }
            // Обновляем интенсивность эффектов в реальном времени
            _currentElectrode?.UpdateWeldEffects(power);
        }
        else
        {
            StopEffectsIfNeeded();
        }
    }

    private void StopEffectsIfNeeded()
    {
        if (_effectsPlaying && _currentElectrode != null)
        {
            _currentElectrode.StopWeldEffects();
            _effectsPlaying = false;
        }
    }

    private void HandleElectrode(Electrode electrode, float power)
    {
        WeldProcessModel model = _targetA?.ProcessModel;
        if (model == null) return;

        float melt = model.EvaluateMelt(power) * Time.deltaTime;
        electrode.Burn(melt);
    }

    /// <returns>True если электрод касается свариваемой пары и сварка выполняется</returns>
    private bool TryWeld(Electrode electrode, float power)
    {
        Ray ray = new Ray(electrode.Tip.position, electrode.Tip.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, electrode.WeldDistance))
        {
            return false;
        }

        Weldable a = hit.collider.GetComponent<Weldable>();

        if (a == null) return false;

        // Обновление цели
        if (_targetA != a)
        {
            _targetA = a;

            _targetB = FindNearbyWeldable(hit.point, a);

            if (_debugMode)
            {
                Debug.Log($"[Welder] Новая цель: " + $"A={_targetA?.name}, " + $"B={_targetB?.name}");
            }
        }

        // Второй детали нет
        if (_targetB == null)
            return false;

        WeldProcessModel model = _targetA?.ProcessModel;
        if (model == null)
        {
            Debug.LogWarning("Weldable не содержит WeldProcessModel!");
            return false;
        }

        // Проверка заземления
        if (!_targetA.IsGrounded || !_targetB.IsGrounded)
        {
            if (_debugMode)
            {
                Debug.Log("[Welder] Сварка невозможна: " + "объекты не заземлены");
            }

            return false;
        }

        // Создание assembly
        if (!_isAssemblyCreated)
            ExecuteWeld();

        // Создание builder
        if (_activeBuilder == null)
        {
            _activeBuilder = InstantiateWeldBuilder(hit.point);

            if (_activeBuilder == null)
                return false;

            if (_currentAssembly != null)
            {
                _activeBuilder.transform.SetParent(_currentAssembly.transform, true);
            }
        }

        // =========================================
        // ОСНОВНАЯ ЛОГИКА СВАРКИ
        // =========================================

        float deposit = model.EvaluateDeposit(power);

        _activeBuilder.Spacing = Mathf.Lerp(0.006f, 0.002f, Mathf.Clamp01(deposit * 50f));

        // Основной валик
        _activeBuilder.AddBead(hit.point, hit.normal);

        // =========================================
        // ОЦЕНКА КАЧЕСТВА СВАРКИ
        // =========================================

        float quality = model.EvaluateQuality(power);

        float defectChance = 1f - quality;

        // =========================================
        // ПРОЖОГ
        // =========================================

        if (model.IsBurning(power))
        {
            _activeBuilder.AddBurn(hit.point, hit.normal);

            int spatters = Random.Range(1, 3);

            for (int i = 0; i < spatters; i++)
            {
                _activeBuilder.AddSpatter(hit.point, hit.normal);
            }
        }

        // =========================================
        // ПОРЫ
        // =========================================

        if (Random.value < defectChance * 0.10f)
        {
            Vector3 poreOffset = hit.normal * 0.001f;

            _activeBuilder.AddPore(hit.point + poreOffset, hit.normal);
        }

        // =========================================
        // БУСИНКИ / РАЗБРЫЗГИВАНИЕ
        // =========================================

        if (Random.value < defectChance * 0.12f)
        {
            _activeBuilder.AddSpatter(hit.point, hit.normal);

            // Иногда дополнительная капля
            if (Random.value < 0.25f)
            {
                _activeBuilder.AddSpatter(hit.point, hit.normal);
            }
        }

        // =========================================
        // НЕДОГРЕВ
        // =========================================

        bool underpowered = power < model.OptimalPower * 0.75f;

        if (underpowered)
        {
            // Прерывистый шов
            if (Random.value < 0.3f)
            {
                return true;
            }

            // Дополнительные капли
            if (Random.value < 0.5f)
            {
                _activeBuilder.AddSpatter(hit.point, hit.normal);
            }
        }

        // =========================================
        // НЕСТАБИЛЬНАЯ ДУГА
        // =========================================

        float arcDistance = Vector3.Distance(electrode.Tip.position, hit.point);

        bool unstableArc = arcDistance > electrode.WeldDistance * 0.8f;

        if (unstableArc)
        {
            int extraSpatter = Random.Range(2, 6);

            for (int i = 0; i < extraSpatter; i++)
            {
                _activeBuilder.AddSpatter(hit.point, hit.normal);
            }

            // Иногда пропускаем валик
            if (Random.value < 0.25f)
            {
                return true;
            }
        }

        return true;
    }

    private void ExecuteWeld()
    {
        if (_isAssemblyCreated) return;
        _isAssemblyCreated = true;
        if (_debugMode) Debug.Log($"[Welder] Мгновенное создание узла: {_targetA.name} + {_targetB.name}");

        _currentAssembly = WeldAssembly.Create(_targetA, _targetB, Vector3.zero);

        if (_activeBuilder != null)
            _activeBuilder.transform.SetParent(_currentAssembly.transform, true);
    }

    private WeldMeshBuilder InstantiateWeldBuilder(Vector3 point)
    {
        GameObject go = Instantiate(_weldMeshPrefab, point, Quaternion.identity);

        WeldMeshBuilder builder = go.GetComponent<WeldMeshBuilder>();

        if (builder == null)
        {
            Destroy(go);
            return null;
        }

        return builder;
    }

    private Weldable FindNearbyWeldable(Vector3 point, Weldable ignore)
    {
        Collider[] hits = Physics.OverlapSphere(point, _currentElectrode._searchRadius);
        foreach (var col in hits)
        {
            if (col.transform == ignore.transform || col.isTrigger) continue;
            Weldable w = col.GetComponent<Weldable>();
            if (w != null) return w;
        }
        return null;
    }

    private bool PrepareToWeld()
    {
        if (!_isActivated || _settings == null) return false;
        if (!_weldingMachineManager.IsMachineReady) return false;

        return true;
    }
}