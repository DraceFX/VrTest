using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NewStep", menuName = "Training/Step Data")]
public class StepData : ScriptableObject
{
    [TextArea(3, 10)]
    public string instructionText;          // текст инструкции
    public Sprite instructionImage;        // поясняющее изображение (опционально)

    [Tooltip("ID, заданный в компоненте StepTargetIdentifier на целевом объекте")]
    public string targetId;
    [Tooltip("Дополнительные объекты для подсветки (только визуальное выделение, без провайдера)")]
    public string[] additionalHighlightTargetIds;

    [Tooltip("Если true, шаг не ждёт автоматического завершения — вызовите StepManager.NextStep() из другого кода")]
    public bool isManual = false;

    [Tooltip("ID объекта, на котором висит компонент, реализующий ICustomStepChecker (если нужна проверка каждый кадр)")]
    public string customCheckerTargetId;    // (приводится к ICustomStepChecker в коде)

    [Header("События (ScriptableObject)")]
    public StepEventSO onStepStartEvent;
    public StepEventSO onStepCompletedEvent;
}
