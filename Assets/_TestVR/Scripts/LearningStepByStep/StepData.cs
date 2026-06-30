using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NewStep", menuName = "Training/Step Data")]
public class StepData : ScriptableObject
{
    [TextArea(3, 10)]
    public string instructionText;          // текст инструкции
    public Sprite instructionImage;        // поясняющее изображение (опционально)

    [Tooltip("ID, заданный в компоненте StepTargetIdentifier на целевом объекте")]
    public string targetId;        // объект, который нужно подсветить и/или с которым взаимодействовать
    public StepCompletionType completionType; // как завершается шаг

    // Если completionType == CustomScript, здесь лежит ссылка на MonoBehaviour с интерфейсом ICustomStepChecker
    public MonoBehaviour customChecker;    // (приводится к ICustomStepChecker в коде)

    // Дополнительно: действия при старте/завершении шага (звук, анимация)
    public UnityEvent onStepStart;
    public UnityEvent onStepCompleted;
}

public enum StepCompletionType
{
    Grab,            // схватить объект
    Activate,        // активировать (кнопка, рычаг)
    CollisionEnter,  // войти в зону
    CustomScript,    // проверка через скрипт (интерфейс)
    LeverActivate,
    KnobTurn,
    Clamp,
    Manual           // шаг завершается вызовом StepManager.NextStep() из другого кода
}
