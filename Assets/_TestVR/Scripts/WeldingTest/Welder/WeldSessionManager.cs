using UnityEngine;

public class WeldSessionManager : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private GameObject _weldMeshPrefab;
    [SerializeField] private WeldTrajectoryEvaluator _trajectoryEvaluator;
    [SerializeField] private WeldQualityAssessor _qualityAssessor;
    [SerializeField] private bool _debugMode = true;

    public WeldAssembly CurrentAssembly { get; private set; }
    public WeldMeshBuilder ActiveBuilder { get; private set; }
    public bool IsAssemblyCreated { get; private set; }

    //Начать новый шов (создать сборку и билдер)
    public void StartNewWeld(Weldable a, Weldable b, Vector3 startPoint, Vector3 normal, Vector3 forward)
    {
        if (IsAssemblyCreated) return;
        IsAssemblyCreated = true;

        if (_debugMode) Debug.Log($"[WeldSession] Создание узла: {a.name} + {b.name}");
        CurrentAssembly = WeldAssembly.Create(a, b, Vector3.zero);

        // Создаём билдер меша
        GameObject go = Instantiate(_weldMeshPrefab, startPoint, Quaternion.identity);
        ActiveBuilder = go.GetComponent<WeldMeshBuilder>();
        if (ActiveBuilder == null)
        {
            Destroy(go);
            Debug.LogError("Префаб не содержит WeldMeshBuilder!");
            return;
        }
        ActiveBuilder.transform.SetParent(CurrentAssembly.transform, true);

        // Инициализация внешних оценщиков
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

        // Не удаляем билдер! Он остаётся частью сборки.
        // ActiveBuilder = null;
        // CurrentAssembly = null;
        // IsAssemblyCreated = false;
    }
}