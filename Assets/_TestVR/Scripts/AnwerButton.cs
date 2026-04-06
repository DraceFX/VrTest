using System;
using UnityEngine.UI;

[Serializable]
public class AnswerButton
{
    public AnswerButtonType Type;
    public Button Button;
}

public enum AnswerButtonType
{
    Button1,
    Button2,
    Button3,
    Button4,
    Button5,
    Button6
}