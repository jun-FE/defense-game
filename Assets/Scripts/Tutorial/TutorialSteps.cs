using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 단계 목록과 안내 그리기(탐색·전투 화면 공용). 프롤로그 화면 구성 기획(xlsx) 기준:
/// - 대사 단계(Say): 화면 아래 대사창 "화자 | 대사". 클릭·Space·Enter로 넘긴다(키 표기는 넣지 않음).
/// - 안내 단계(Guide): 화면 아래 가운데 조작 안내 툴팁. 행동을 하거나 기다려도 그대로 두고, 다음 단계 안내가 나올 때 바뀐다.
///   (단순 키 입력이나 첫 행동만으로 숨기지 않는다.) keys를 주면 키캡(W A S D)을 그린다.
/// 오른쪽 위에 작은 "건너뛰기"가 있다.
/// </summary>
public class TutorialSteps
{
    public sealed class Step
    {
        public string speaker, text;
        public string[] keys = new string[0];
        /// <summary>true가 되면 다음 단계로. 대사 단계는 null이면 입력으로 넘긴다. 안내 단계는 null이면 바깥에서 GoTo로 넘긴다.</summary>
        public Func<bool> done;
        public Action onEnter;
        public bool IsDialog => speaker != null;
    }

    readonly List<Step> steps = new List<Step>();
    int index = -1;
    float shownAt;
    GUIStyle skipStyle;

    public int Index => index;
    public Step Current => index >= 0 && index < steps.Count ? steps[index] : null;
    /// <summary>지금 대사창을 보여 주는 중인지(이때는 조작을 막는 화면이 많다).</summary>
    public bool InDialog => Current != null && Current.IsDialog;

    public TutorialSteps Guide(string text, Func<bool> done = null, Action onEnter = null, params string[] keys)
    {
        steps.Add(new Step { text = text, done = done, onEnter = onEnter, keys = keys });
        return this;
    }

    public TutorialSteps Say(string speaker, string text, Func<bool> done = null, Action onEnter = null)
    {
        steps.Add(new Step { speaker = speaker, text = text, done = done, onEnter = onEnter });
        return this;
    }

    public void Start() => GoTo(0);

    public void GoTo(int i)
    {
        if (i == index) return;
        index = i;
        shownAt = Time.unscaledTime;
        Current?.onEnter?.Invoke();
    }

    public void Next() => GoTo(index + 1);

    /// <summary>조건이 채워진 단계는 넘긴다(한 프레임에 여러 단계도).</summary>
    public void Update()
    {
        int guard = 0;
        while (Current != null && Current.done != null && Current.done() && guard++ < 8) GoTo(index + 1);
    }

    /// <summary>안내를 그린다(UIKit.Begin 뒤, 1080p 기준 좌표). 건너뛰기를 누르면 true. 대사 단계의 넘기기 입력도 여기서 받는다.</summary>
    public bool Draw(float w, float h, bool showSkip = true)
    {
        if (skipStyle == null) skipStyle = new GUIStyle(UIKit.ButtonSmall) { fontSize = 16 };
        bool skip = showSkip && UIKit.ClickButton(new Rect(w - 190f, 24f, 160f, 40f), "건너뛰기 ▶▶", skipStyle);
        Step s = Current;
        if (s == null) return skip;

        // 대사는 한 글자씩
        float chars = Mathf.Max(1f, GameSettings.TextCharsPerSecond);
        int shown = GameSettings.TextCharsPerSecond <= 0f ? s.text.Length : Mathf.Min(s.text.Length, Mathf.FloorToInt((Time.unscaledTime - shownAt) * chars));
        if (s.IsDialog)
        {
            DreamUI.Dialog(w, h, s.speaker, s.text.Substring(0, shown), shown >= s.text.Length);
            Event e = Event.current;
            bool advance = (e.type == EventType.MouseDown && e.button == 0) ||
                           (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter));
            if (advance && s.done == null)
            {
                if (shown < s.text.Length) shownAt = -1000f; // 출력 중이면 바로 완성
                else Next();
                e.Use();
            }
        }
        else DreamUI.Tooltip(w, h, s.text, s.keys);
        return skip;
    }
}
