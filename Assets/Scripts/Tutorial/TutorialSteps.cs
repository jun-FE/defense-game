using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 안내 단계 목록과 안내 상자 그리기(탐색·전투 화면 공용).
/// 단계마다: 말하는 사람(비우면 "플레이 안내"), 제목, 내용, 끝나는 조건(없으면 "다음" 버튼).
/// 안내 상자는 화면 위 가운데에 뜨고, 오른쪽 위에 "튜토리얼 건너뛰기"가 있다.
/// </summary>
public class TutorialSteps
{
    public sealed class Step
    {
        public string speaker, title, text;
        /// <summary>true가 되면 다음 단계로. null이면 "다음" 버튼으로 넘긴다.</summary>
        public Func<bool> done;
        public Action onEnter;
    }

    readonly List<Step> steps = new List<Step>();
    int index = -1;
    float shownAt;
    GUIStyle titleStyle, bodyStyle, speakerStyle, skipStyle;

    public int Index => index;
    public Step Current => index >= 0 && index < steps.Count ? steps[index] : null;

    public TutorialSteps Guide(string title, string text, Func<bool> done = null, Action onEnter = null)
    {
        steps.Add(new Step { title = title, text = text, done = done, onEnter = onEnter });
        return this;
    }

    public TutorialSteps Say(string speaker, string text, Func<bool> done = null, Action onEnter = null)
    {
        steps.Add(new Step { speaker = speaker, text = text, done = done, onEnter = onEnter });
        return this;
    }

    /// <summary>첫 단계부터 시작.</summary>
    public void Start() => GoTo(0);

    public void GoTo(int i)
    {
        if (i == index) return;
        index = i;
        shownAt = Time.unscaledTime;
        Current?.onEnter?.Invoke();
        if (Current != null) SoundFx.Play("ui_open", 0.5f);
    }

    /// <summary>조건이 채워진 단계는 넘긴다(한 프레임에 여러 단계도).</summary>
    public void Update()
    {
        int guard = 0;
        while (Current != null && Current.done != null && Current.done() && guard++ < 8) GoTo(index + 1);
    }

    /// <summary>안내 상자를 그린다(UIKit.Begin 뒤, 1080p 기준 좌표). 건너뛰기를 누르면 true.</summary>
    public bool Draw(float w, float top = 86f)
    {
        EnsureStyles();
        bool skip = UIKit.ClickButton(new Rect(w - 250f, top, 220f, 44f), "튜토리얼 건너뛰기", skipStyle);
        Step s = Current;
        if (s == null) return skip;

        // 나타날 때 살짝 내려오며 밝아진다
        float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.25f);
        var box = new Rect(w / 2f - 470f, top - 12f * (1f - t), 940f, 150f);
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, t);
        UIKit.DrawPanel(box);
        bool guide = string.IsNullOrEmpty(s.speaker);
        string head = guide ? "플레이 안내" + (string.IsNullOrEmpty(s.title) ? "" : " · " + s.title) : s.speaker;
        GUI.Label(new Rect(box.x + 32f, box.y + 12f, box.width - 64f, 36f), head, guide ? titleStyle : speakerStyle);
        GUI.Label(new Rect(box.x + 32f, box.y + 50f, box.width - (s.done == null ? 200f : 64f), box.height - 60f), s.text, bodyStyle);
        if (s.done == null && UIKit.ClickButton(new Rect(box.xMax - 160f, box.yMax - 62f, 136f, 48f), "다음 ▶", UIKit.ButtonSmall))
            GoTo(index + 1);
        GUI.color = old;
        return skip;
    }

    void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(UIKit.Speaker) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
        titleStyle.normal.textColor = new Color(0.95f, 0.82f, 0.5f);
        speakerStyle = new GUIStyle(UIKit.Speaker) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
        bodyStyle = new GUIStyle(UIKit.Dialogue) { fontSize = 22, wordWrap = true, richText = true };
        skipStyle = new GUIStyle(UIKit.ButtonSmall) { fontSize = 18 };
    }
}
