using System;
using UnityEngine;

[Serializable]
public class StoryLine
{
    [Tooltip("말하는 사람. 비워 두면 나레이션으로 표시된다.")]
    public string speaker;
    [TextArea(2, 5)]
    public string text;

    public StoryLine() { }

    public StoryLine(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}

/// <summary>스토리 한 편(대사 목록). Project 창에서 우클릭 > Create > Defense > Story 로 만든다.</summary>
[CreateAssetMenu(menuName = "Defense/Story", fileName = "NewStory")]
public class StoryData : ScriptableObject
{
    public Color background = new Color(0.08f, 0.09f, 0.14f);
    public StoryLine[] lines = new StoryLine[0];
}
