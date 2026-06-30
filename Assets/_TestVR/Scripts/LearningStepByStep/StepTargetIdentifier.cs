using UnityEngine;

public class StepTargetIdentifier : MonoBehaviour
{
    [SerializeField] private string _targetId;
    public string TargetId => _targetId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Упрощаем ввод: генерируем ID по имени объекта, если поле пустое
        if (string.IsNullOrEmpty(_targetId))
            _targetId = gameObject.name;
    }
#endif
}
