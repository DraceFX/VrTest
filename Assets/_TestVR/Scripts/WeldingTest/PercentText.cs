using TMPro;
using UnityEngine;

public class PercentText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    private void Awake()
    {
        SetText("");
    }

    public void SetText(string text)
    {
        _text.text = text;
    }
}
