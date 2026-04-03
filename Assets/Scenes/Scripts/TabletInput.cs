using UnityEngine;
using UnityEngine.UI;

public class TabletInput : MonoBehaviour
{
    [SerializeField] private Button _tabletPressed;

    [SerializeField] private UiManager _uiManager;

    private void OnEnable()
    {
        _tabletPressed.onClick.AddListener(() => _uiManager.ShowPanel(true));
    }

    private void OnDisable()
    {
        RemoveListener();
    }

    private void OnDestroy()
    {
        RemoveListener();
    }

    private void RemoveListener()
    {
        _tabletPressed.onClick.RemoveListener(() => _uiManager.ShowPanel(false));
    }
}
