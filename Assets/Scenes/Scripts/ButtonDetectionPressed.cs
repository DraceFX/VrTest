using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonDetectionPressed : MonoBehaviour
{
    [SerializeField] private List<Button> _buttons;
    [SerializeField] private DetectionData _data;
    [SerializeField] private UiManager _uiManager;

    private DetectionObject _detectionObject;

    private void OnEnable()
    {
        foreach (var button in _buttons)
        {
            button.onClick.AddListener(() => OnAnyButtonClicked());
        }
    }

    private void OnDisable()
    {
        RemoveListener();
    }

    private void OnDestroy()
    {
        RemoveListener();
    }

    private void OnAnyButtonClicked()
    {
        _uiManager.OnButtonPressed(_data.currentObject);
    }

    private void RemoveListener()
    {
        foreach (var button in _buttons)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
