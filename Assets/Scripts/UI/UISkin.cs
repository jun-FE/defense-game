using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 UI 벡터 팩 조각(Assets/Art/Main/UI, 9-slice 모서리 폭은 ui_borders.json)으로 IMGUI 틀·아이콘·글자를 그리는 도구.
/// 전투 HUD가 쓴다. 이름은 조각 파일 이름(확장자 제외)이다.
/// </summary>
public class UISkin
{
    [Serializable]
    class BorderItem { public string name; public int border; }

    [Serializable]
    class BorderList { public BorderItem[] items = new BorderItem[0]; }

    public static readonly Color Light = Hex("#F3EBDD"), Muted = Hex("#948AA8"), Gold = Hex("#E0BE7C"), Violet = Hex("#A98BFF");

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, int> borders = new Dictionary<string, int>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    public UISkin(IEnumerable<Sprite> sprites, TextAsset borderJson)
    {
        if (sprites != null)
            foreach (Sprite sprite in sprites)
                if (sprite != null) tex[sprite.name] = sprite.texture;
        if (borderJson != null)
            foreach (BorderItem item in JsonUtility.FromJson<BorderList>(borderJson.text).items)
                borders[item.name] = item.border;
    }

    public bool Has(string name) => tex.ContainsKey(name);

    /// <summary>9-slice 틀. 조각이 없으면 반투명 남색 상자.</summary>
    public void Frame(Rect r, string name)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (tex.ContainsKey(name)) FrameStyle(name).Draw(r, false, false, false, false);
        else GUI.Box(r, GUIContent.none, UIKit.Panel);
    }

    public void Icon(Rect r, string name)
    {
        Texture2D t;
        if (tex.TryGetValue(name, out t)) GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
    }

    public GUIStyle FrameStyle(string normal, string hover = null)
    {
        string key = "frame:" + normal + ":" + hover;
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle();
        Texture2D t;
        if (tex.TryGetValue(normal, out t)) style.normal.background = t;
        if (hover != null && tex.TryGetValue(hover, out t)) style.hover.background = t;
        int b;
        if (borders.TryGetValue(normal, out b)) style.border = new RectOffset(b, b, b, b);
        styles[key] = style;
        return style;
    }

    /// <summary>주 버튼(보라 틀, 마우스를 올리면 밝아짐).</summary>
    public GUIStyle Button(int fontSize = 24)
    {
        string key = "button:" + fontSize;
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(FrameStyle("button_primary", "button_primary_hover")) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = style.hover.textColor = Light;
        styles[key] = style;
        return style;
    }

    public GUIStyle Text(int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
    {
        string key = $"text:{size}:{color}:{anchor}:{bold}:{wrap}";
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, wordWrap = wrap, richText = true, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        style.normal.textColor = color;
        styles[key] = style;
        return style;
    }

    /// <summary>가로 게이지(틀 + 채움).</summary>
    public void Gauge(Rect r, float t, string fill = "gauge_fill")
    {
        Frame(r, "gauge_track");
        float inner = r.width - 8f;
        if (t > 0.001f) Frame(new Rect(r.x + 4f, r.y + 4f, Mathf.Max(16f, inner * Mathf.Clamp01(t)), r.height - 8f), fill);
    }

    public static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}
