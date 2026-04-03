using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] private GameObject _panel;

    private int _correctObject = 0;
    private int _incorrectObject = 0;

    private void Awake()
    {
        _panel.SetActive(false);
    }

    public void ShowPanel(bool isShow)
    {
        _panel.SetActive(true);
    }

    public void OnButtonPressed(DetectionObject obj)
    {
        _panel.SetActive(false);

        if (obj == null) return;

        if (obj.IsFake())
        {
            _incorrectObject++;
            Debug.Log("Incorrect objects: " + _incorrectObject);
        }
        else
        {
            _correctObject++;
            Debug.Log("Correct objects: " + _correctObject);
        }

        Destroy(obj.gameObject);
    }
}
