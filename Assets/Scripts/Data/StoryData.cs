using System;
using UnityEngine;

[Serializable]
public class StoryLine
{
    [Tooltip("말하는 사람. 비워 두면 나레이션으로 표시된다.")]
    public string speaker;
    [TextArea(2, 5)]
    public string text;

    [Header("선택(비워 두면 없음)")]
    [Tooltip("이 대사 앞에 보여 줄 장면 제목 카드")]
    public string sceneTitle;
    [Tooltip("장면 제목 아래 작은 글씨")]
    public string sceneSubtitle;
    [Tooltip("배경 그림 이름(Resources/Stories/Backgrounds/이름). 한 번 정하면 다음에 바꿀 때까지 유지")]
    public string background;
    [Tooltip("초상화 이름(Resources/Stories/Portraits/이름). 비우면 말하는 사람 이름을 쓴다")]
    public string portrait;

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
