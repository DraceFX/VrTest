using UnityEngine;

public class Welder : MonoBehaviour
{
    [Header("Сокет держателя")]
    public ElectrodeSocket socket;

    [Header("Настройки сварки")]
    public WeldingSettings settings;
    public WeldProcessModel process = new WeldProcessModel();

    [Header("Генератор меша шва (префаб)")]
    public GameObject weldMeshPrefab;

    [Header("Градиент температуры")]
    public Gradient temperatureGradient;

    [Header("Поиск соседней детали")]
    [SerializeField] private float searchRadius = 0.08f;
    [SerializeField] private bool debugMode = true;

    private bool isActivated = false;
    private Weldable targetA, targetB;
    private bool isAssemblyCreated = false;
    private WeldAssembly currentAssembly;
    private WeldMeshBuilder activeBuilder;

    // Текущий активный электрод и флаг эффектов
    private Electrode currentElectrode;
    private bool effectsPlaying = false;

    public void SetActivated(bool state)
    {
        isActivated = state;

        // При отжатии кнопки сразу останавливаем эффекты и сбрасываем контакт
        if (!state)
        {
            StopEffectsIfNeeded();
            targetA = null;
            targetB = null;
        }
    }

    private void Update()
    {
        if (!isActivated || settings == null) return;

        Electrode electrode = socket?.AttachedElectrode;
        if (electrode == null)
        {
            // Электрод отсутствует — выключаем эффекты
            StopEffectsIfNeeded();
            currentElectrode = null;
            return;
        }

        // Обновили электрод — если сменился, переносим управление эффектами
        if (currentElectrode != electrode)
        {
            StopEffectsIfNeeded();      // остановить на старом
            currentElectrode = electrode;
        }

        float power = settings.Power;
        bool hasContact = TryWeld(electrode, power);

        // Управление эффектами: включаем только при успешном контакте, иначе выключаем
        if (hasContact)
        {
            if (!effectsPlaying)
            {
                currentElectrode?.StartWeldEffects(power, process.optimalPower);
                HandleElectrode(electrode, power);
                effectsPlaying = true;
            }
            // Обновляем интенсивность эффектов в реальном времени
            currentElectrode?.UpdateWeldEffects(power);
        }
        else
        {
            StopEffectsIfNeeded();
        }
    }

    private void StopEffectsIfNeeded()
    {
        if (effectsPlaying && currentElectrode != null)
        {
            currentElectrode.StopWeldEffects();
            effectsPlaying = false;
        }
    }

    private void HandleElectrode(Electrode electrode, float power)
    {
        float melt = process.EvaluateMelt(power) * Time.deltaTime;
        electrode.Burn(melt);
    }

    /// <returns>True если электрод касается свариваемой пары и сварка выполняется</returns>
    private bool TryWeld(Electrode electrode, float power)
    {
        Ray ray = new Ray(electrode.tip.position, electrode.tip.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, electrode.weldDistance))
            return false;

        Weldable a = hit.collider.GetComponent<Weldable>();
        if (a == null) return false;

        if (targetA != a)
        {
            targetA = a;
            targetB = FindNearbyWeldable(hit.point, a);
            if (debugMode)
                Debug.Log($"[Welder] Новая цель: A={targetA?.name}, B={targetB?.name}");
        }

        if (targetB == null) return false;   // нет второго объекта — сварка невозможна

        // Мгновенное создание узла, если ещё не создан
        if (!isAssemblyCreated)
            ExecuteWeld();

        // Создаём/получаем билдер меша шва
        if (activeBuilder == null)
        {
            activeBuilder = InstantiateWeldBuilder(hit.point);
            if (activeBuilder == null) return false;

            if (isAssemblyCreated && currentAssembly != null)
                activeBuilder.transform.SetParent(currentAssembly.transform, true);
            else
            {
                Rigidbody rbA = targetA.GetComponentInParent<Rigidbody>();
                Transform parent = rbA ? rbA.transform : targetA.transform;
                activeBuilder.transform.SetParent(parent, true);
            }
        }

        // Наплавка шва
        float deposit = process.EvaluateDeposit(power);
        activeBuilder.spacing = Mathf.Lerp(0.006f, 0.002f, Mathf.Clamp01(deposit * 50f));
        activeBuilder.AddBead(hit.point, hit.normal);

        // Дефекты
        float overheat = Mathf.Clamp01((power - process.optimalPower) / process.optimalPower);
        if (Random.value < overheat * 0.15f)
            activeBuilder.AddPore(hit.point + hit.normal * 0.0002f, Random.Range(0.0003f, 0.0008f));
        if (process.IsBurning(power))
            activeBuilder.AddBurn(hit.point, hit.normal);

        return true; // сварка успешно произошла
    }

    private void ExecuteWeld()
    {
        if (isAssemblyCreated) return;
        isAssemblyCreated = true;
        if (debugMode) Debug.Log($"[Welder] Мгновенное создание узла: {targetA.name} + {targetB.name}");

        currentAssembly = WeldAssembly.Create(targetA, targetB, Vector3.zero);

        if (activeBuilder != null)
            activeBuilder.transform.SetParent(currentAssembly.transform, true);
    }

    private WeldMeshBuilder InstantiateWeldBuilder(Vector3 point)
    {
        GameObject go = Instantiate(weldMeshPrefab, point, Quaternion.identity);
        var builder = go.GetComponent<WeldMeshBuilder>();
        if (builder == null) { Destroy(go); return null; }
        builder.temperatureGradient = temperatureGradient ?? new Gradient();
        return builder;
    }

    private Weldable FindNearbyWeldable(Vector3 point, Weldable ignore)
    {
        Collider[] hits = Physics.OverlapSphere(point, searchRadius);
        foreach (var col in hits)
        {
            if (col.transform == ignore.transform || col.isTrigger) continue;
            Weldable w = col.GetComponent<Weldable>();
            if (w != null) return w;
        }
        return null;
    }
}