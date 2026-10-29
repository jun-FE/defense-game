using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 글자 대본(.txt)을 스토리로 바꾼다. 대본만 고치면 스토리가 바뀌도록(코드·에셋 수정 없이).
///
/// 대본 쓰는 법(한 줄에 하나):
///   # 메모              — 무시(설명·메모용)
///   [장면] 제목 | 부제   — 다음 대사 앞에 장면 제목 카드(부제는 없어도 됨)
///   [배경] 이름          — 배경 그림 Resources/Stories/Backgrounds/이름 (없으면 색 배경 + 이름 표시)
///   [색] #RRGGBB         — 그림이 없을 때 쓸 배경색
///   이름: 대사           — 대사. 초상화는 Resources/Stories/Portraits/이름
///   이름(표정): 대사      — 표정 초상화 Resources/Stories/Portraits/이름_표정
///   : 나레이션           — 말하는 사람 없는 해설(앞의 콜론 생략 가능)
/// </summary>
public static class StoryScript
{
    public const string ProloguePath = "Stories/prologue";

    /// <summary>새로하기 프롤로그. 대본이 없거나 비어 있으면 null.</summary>
    public static StoryData LoadPrologue() => Load(ProloguePath);

    public static StoryData Load(string resourcePath)
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        return asset != null ? Parse(asset.text) : null;
    }

    public static StoryData Parse(string script)
    {
        var lines = new List<StoryLine>();
        var story = ScriptableObject.CreateInstance<StoryData>();
        string title = null, subtitle = null, background = null;

        foreach (string raw in script.Replace("\r", "").Split('\n'))
        {
            string row = raw.Trim();
            if (row.Length == 0 || row.StartsWith("#")) continue;

            if (Directive(row, "[장면]", out string value))
            {
                int bar = value.IndexOf('|');
                title = (bar >= 0 ? value.Substring(0, bar) : value).Trim();
                subtitle = bar >= 0 ? value.Substring(bar + 1).Trim() : "";
                continue;
            }
            if (Directive(row, "[배경]", out value)) { background = value; continue; }
            if (Directive(row, "[색]", out value))
            {
                if (ColorUtility.TryParseHtmlString(value, out Color color)) story.background = color;
                continue;
            }

            var line = SplitSpeaker(row);
            line.sceneTitle = title;
            line.sceneSubtitle = subtitle;
            line.background = background;
            title = subtitle = background = null;
            lines.Add(line);
        }

        story.lines = lines.ToArray();
        return story.lines.Length > 0 ? story : null;
    }

    static bool Directive(string row, string tag, out string value)
    {
        value = row.StartsWith(tag) ? row.Substring(tag.Length).Trim() : null;
        return value != null;
    }

    /// <summary>"이름(표정): 대사" → 말하는 사람·초상화·대사. 콜론이 없거나 맨 앞이면 나레이션.</summary>
    static StoryLine SplitSpeaker(string row)
    {
        int colon = row.IndexOf(':');
        // 대사 속 콜론("시간: 3시")과 헷갈리지 않게, 이름은 짧은 것만 인정한다.
        if (colon < 0 || colon > 16) return new StoryLine("", row);

        string name = row.Substring(0, colon).Trim();
        string text = row.Substring(colon + 1).Trim();
        if (name.Length == 0) return new StoryLine("", text);

        string portrait = name;
        int paren = name.IndexOf('(');
        if (paren > 0 && name.EndsWith(")"))
        {
            string mood = name.Substring(paren + 1, name.Length - paren - 2).Trim();
            name = name.Substring(0, paren).Trim();
            portrait = mood.Length > 0 ? $"{name}_{mood}" : name;
        }
        return new StoryLine(name, text) { portrait = portrait };
    }
}
