using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepManager : MonoBehaviour
{
    [Header("Последовательность шагов")]
    [SerializeField] private List<StepData> _steps = new List<StepData>();
    [SerializeField] private int _currentStepIndex = 0;

    [Header("UI элементы")]
    [SerializeField] private TMP_Text _instructionTextUI;
    [SerializeField] private Image _instructionImageUI;

    [Header("Сервисы")]
    [SerializeField] private MonoBehaviour _outlineService;

    private IOutlineService _outline;
    private IStepCompletionProvider _currentCompletionProvider;
    private ICustomStepChecker _currentCustomChecker;

    private void Awake()
    {
        _outline = _outlineService as IOutlineService;
        if (_outline == null)
            Debug.LogWarning("OutlineService не реализует IOutlineService");
    }

    private void Start()
    {
        if (_steps.Count > 0)
            GoToStep(_currentStepIndex);
        else
            Debug.LogWarning("Нет шагов в последовательности");
    }

    public void NextStep()
    {
        if (_currentStepIndex < _steps.Count - 1)
        {
            CleanupCurrentStep();
            _currentStepIndex++;
            ApplyStep(_currentStepIndex);
        }
        else
        {
            CompleteTraining();
        }
    }

    public void PreviousStep()
    {
        if (_currentStepIndex > 0)
        {
            CleanupCurrentStep();
            _currentStepIndex--;
            ApplyStep(_currentStepIndex);
        }
    }

    public void GoToStep(int index)
    {
        if (index < 0 || index >= _steps.Count)
            return;
        CleanupCurrentStep();
        _currentStepIndex = index;
        ApplyStep(_currentStepIndex);
    }

    private void ApplyStep(int index)
    {
        StepData step = _steps[index];
        GameObject target = TargetRegistry.Instance.GetTarget(step.targetId);
        if (target == null)
        {
            Debug.LogError($"Целевой объект с ID '{step.targetId}' не найден. Шаг {index} пропущен.");
            NextStep();
            return;
        }

        DisplayStepUI(step);
        ActivateOutline(target);
        SetupCompletionProvider(step, target);
        step.onStepStart?.Invoke();
    }

    private void DisplayStepUI(StepData step)
    {
        if (_instructionTextUI != null)
            _instructionTextUI.text = step.instructionText;

        if (step.instructionImage != null)
        {
            _instructionImageUI.gameObject.SetActive(true);
            _instructionImageUI.sprite = step.instructionImage;
        }
        else
        {
            _instructionImageUI.gameObject.SetActive(false);
        }
    }

    private void ActivateOutline(GameObject target)
    {
        if (_outline != null && target != null)
            _outline.ShowOutLine(target);
    }

    private void SetupCompletionProvider(StepData step, GameObject target)
    {
        if (_currentCompletionProvider != null)
        {
            _currentCompletionProvider.OnCompleted -= OnStepActionCompleted;
            _currentCompletionProvider = null;
        }
        _currentCustomChecker = null;

        switch (step.completionType)
        {
            case StepCompletionType.Grab:
                _currentCompletionProvider = target.GetComponent<GrabStepCompletionProvider>();
                break;
            case StepCompletionType.Activate:
                _currentCompletionProvider = target.GetComponent<ActivateStepCompletionProvider>();
                break;
            case StepCompletionType.CollisionEnter:
                _currentCompletionProvider = target.GetComponent<TriggerStepCompletionProvider>();
                break;
            case StepCompletionType.CustomScript:
                _currentCustomChecker = step.customChecker as ICustomStepChecker;
                break;
            case StepCompletionType.LeverActivate:
                _currentCompletionProvider = target.GetComponent<LeverStepCompletionProvider>();
                break;
            case StepCompletionType.KnobTurn:
                _currentCompletionProvider = target.GetComponent<KnobStepCompletionProvider>();
                break;
            case StepCompletionType.Clamp:
                _currentCompletionProvider = target.GetComponent<ClampStepCompletionProvider>();
                break;
            case StepCompletionType.Manual:
                // Ничего не делаем – шаг будет завершён вызовом NextStep() извне
                break;
        }

        if (_currentCompletionProvider != null)
            _currentCompletionProvider.OnCompleted += OnStepActionCompleted;
    }

    private void OnStepActionCompleted()
    {
        StepData step = _steps[_currentStepIndex];
        step.onStepCompleted?.Invoke();
        NextStep();
    }

    private void Update()
    {
        if (_currentCustomChecker != null && _currentCustomChecker.IsCompleted)
        {
            _currentCustomChecker = null;
            StepData step = _steps[_currentStepIndex];
            step.onStepCompleted?.Invoke();
            NextStep();
        }
    }

    private void CleanupCurrentStep()
    {
        if (_currentCompletionProvider != null)
        {
            _currentCompletionProvider.OnCompleted -= OnStepActionCompleted;
            _currentCompletionProvider = null;
        }

        // Выключаем обводку у объекта текущего шага
        if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
        {
            string currentId = _steps[_currentStepIndex].targetId;
            if (!string.IsNullOrEmpty(currentId))
            {
                GameObject currentTarget = TargetRegistry.Instance.GetTarget(currentId);
                if (currentTarget != null)
                    _outline?.HideOutline(currentTarget);
            }
        }
        _currentCustomChecker = null;
    }

    public void AddStep(StepData newStep, int insertIndex = -1)
    {
        if (insertIndex < 0 || insertIndex > _steps.Count)
            _steps.Add(newStep);
        else
            _steps.Insert(insertIndex, newStep);

        if (_steps.Count == 1)
            GoToStep(0);
        else if (insertIndex <= _currentStepIndex && insertIndex >= 0)
            _currentStepIndex++;
    }

    public void RemoveStep(int index)
    {
        if (index < 0 || index >= _steps.Count)
            return;

        if (index == _currentStepIndex)
        {
            CleanupCurrentStep();
            _steps.RemoveAt(index);

            if (_steps.Count == 0)
                return;

            if (_currentStepIndex >= _steps.Count)
                _currentStepIndex = _steps.Count - 1;
            GoToStep(_currentStepIndex);
        }
        else
        {
            _steps.RemoveAt(index);
            if (index < _currentStepIndex)
                _currentStepIndex--;
        }
    }

    private void CompleteTraining()
    {
        _instructionTextUI.text = "Обучение завершено!";
        Debug.Log("Обучение завершено!");
    }
}
