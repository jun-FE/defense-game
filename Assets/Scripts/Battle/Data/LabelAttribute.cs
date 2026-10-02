using UnityEngine;

/// <summary>Inspector에 변수 이름 대신 보여줄 한국어 이름. 예: [Label("공격속도 (초)")]</summary>
public class LabelAttribute : PropertyAttribute
{
    public readonly string Text;

    public LabelAttribute(string text)
    {
        Text = text;
    }
}
