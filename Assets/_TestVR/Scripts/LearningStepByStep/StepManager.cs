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

    private List<GameObject> _currentHighlightTargets = new List<GameObject>();

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

        step.onStepStartEvent?.Raise();

        DisplayStepUI(step);
        ActivateOutline(target);
        foreach (string id in step.additionalHighlightTargetIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            GameObject extraTarget = TargetRegistry.Instance.GetTarget(id);
            if (extraTarget != null)
            {
                ActivateOutline(extraTarget);
                _currentHighlightTargets.Add(extraTarget);
            }
            else
            {
                Debug.LogWarning($"Дополнительный объект с ID '{id}' не найден");
            }
        }
        SetupCompletionProvider(step, target);
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

        if (step.isManual)
            return;

        // 1) Ищем IStepCompletionProvider на целевом объекте
        var providerComponent = target.GetComponent<IStepCompletionProvider>() as MonoBehaviour;
        if (providerComponent != null)
        {
            _currentCompletionProvider = (IStepCompletionProvider)providerComponent;
            _currentCompletionProvider.OnCompleted += OnStepActionCompleted;

            return;   // всё настроено, выходим
        }

        // 2) Если провайдера нет — проверяем customCheckerTargetId
        if (!string.IsNullOrEmpty(step.customCheckerTargetId))
        {
            GameObject checkerObj = TargetRegistry.Instance.GetTarget(step.customCheckerTargetId);
            if (checkerObj != null)
            {
                _currentCustomChecker = checkerObj.GetComponent<ICustomStepChecker>();
                if (_currentCustomChecker == null)
                    Debug.LogError($"Объект с ID '{step.customCheckerTargetId}' не содержит компонент ICustomStepChecker");
            }
            else
            {
                Debug.LogError($"Объект с ID '{step.customCheckerTargetId}' не найден в реестре");
            }
            return;   // не важно, успешно или нет — мы пытались, больше ничего не делаем
        }

        // 3) Ничего не найдено — ошибка
        Debug.LogError($"Шаг {step.name}: на объекте '{target.name}' нет IStepCompletionProvider, " +
                       "и не задан customCheckerTargetId. Шаг не будет завершён.");
    }

    private void OnStepActionCompleted()
    {
        StepData step = _steps[_currentStepIndex];
        step.onStepCompletedEvent?.Raise();
        NextStep();
    }

    private void Update()
    {
        if (_currentCustomChecker != null && _currentCustomChecker.IsCompleted)
        {
            _currentCustomChecker = null;
            StepData step = _steps[_currentStepIndex];
            step.onStepCompletedEvent?.Raise();
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

        // Отключаем подсветку основного объекта
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
        // Отключаем подсветку всех дополнительных объектов
        foreach (var go in _currentHighlightTargets)
        {
            if (go != null)
                _outline?.HideOutline(go);
        }
        _currentHighlightTargets.Clear();

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
        CleanupCurrentStep();
    }
}
