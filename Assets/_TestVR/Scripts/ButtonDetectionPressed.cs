using System.Collections.Generic;
using UnityEngine;

public class ButtonDetectionPressed : MonoBehaviour
{
    [SerializeField] private List<AnswerButton> _allButtons;
    [SerializeField] private UiManager _uiManager;

    private DetectionObject _cachedObject;

    private List<AnswerButton> _acitveButton = new List<AnswerButton>();

    private void OnEnable()
    {
        DetectionState.OnChanged += SetupButtons;

        SetupButtons(DetectionState.CurrentObject);
    }

    private void OnDisable()
    {
        RemoveListener();
    }

    private void OnDestroy()
    {
        RemoveListener();
    }

    private void SetupButtons(DetectionObject obj)
    {
        _cachedObject = obj;

        RemoveListener();

        foreach (var b in _allButtons)
        {
            b.Button.gameObject.SetActive(false);
        }

        AnswerButton correct = null;

        for (int i = 0; i < _allButtons.Count; i++)
        {
            if (_allButtons[i].Type == obj.CorrectButton)
            {
                correct = _allButtons[i];
                break;
            }
        }

        // if (correct == null) return;

        List<AnswerButton> pool = new List<AnswerButton>();

        for (int i = 0; i < _allButtons.Count; i++)
        {
            if (_allButtons[i].Type != obj.CorrectButton)
            {
                pool.Add(_allButtons[i]);
            }
        }

        _acitveButton.Clear();
        _acitveButton.Add(correct);

        int needed = Mathf.Min(3, pool.Count);

        for (int i = 0; i < needed; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);

            _acitveButton.Add(pool[index]);
            pool.RemoveAt(index);
        }

        for (int i = 0; i < _acitveButton.Count; i++)
        {
            int randIndex = UnityEngine.Random.Range(i, _acitveButton.Count);

            var temp = _acitveButton[i];
            _acitveButton[i] = _acitveButton[randIndex];
            _acitveButton[randIndex] = temp;
        }

        for (int i = 0; i < _acitveButton.Count; i++)
        {
            var btn = _acitveButton[i];

            btn.Button.gameObject.SetActive(true);

            var capture = btn;
            btn.Button.onClick.AddListener(() => OnButtonClicked(capture.Type));
        }
    }

    private void OnButtonClicked(AnswerButtonType pressedType)
    {
        bool isCorrectAnswer = false;

        if (_cachedObject != null)
        {
            isCorrectAnswer = pressedType == _cachedObject.CorrectButton;

        }

        _uiManager.OnAnswer(_cachedObject, isCorrectAnswer);
    }

    private void RemoveListener()
    {
        DetectionState.OnChanged -= SetupButtons;
        foreach (var b in _allButtons)
        {
            b.Button.onClick.RemoveAllListeners();
        }
    }
}
