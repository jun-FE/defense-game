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
    class BorderItem { public string name; public int border; public int bx; public int by; }

    [Serializable]
    class BorderList { public BorderItem[] items = new BorderItem[0]; }

    public static readonly Color Light = Hex("#F3EBDD"), Muted = Hex("#948AA8"), Gold = Hex("#E0BE7C"), Violet = Hex("#A98BFF");
    /// <summary>수선 공방 조각(양피지·리넨) 위 글자색: 잉크, 옅은 잉크, 갈색 강조.</summary>
    public static readonly Color Ink = Hex("#2E2219"), InkMuted = Hex("#6B5744"), Brown = Hex("#8A5520");

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, RectOffset> borders = new Dictionary<string, RectOffset>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    public UISkin(IEnumerable<Sprite> sprites, TextAsset borderJson)
    {
        if (sprites != null)
            foreach (Sprite sprite in sprites)
                if (sprite != null) tex[sprite.name] = sprite.texture;
        if (borderJson != null)
            foreach (BorderItem item in JsonUtility.FromJson<BorderList>(borderJson.text).items)
            {
                int bx = item.bx > 0 ? item.bx : item.border, by = item.by > 0 ? item.by : item.border;
                borders[item.name] = new RectOffset(bx, bx, by, by);
            }
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
        RectOffset b;
        if (borders.TryGetValue(normal, out b)) style.border = new RectOffset(b.left, b.right, b.top, b.bottom);
        styles[key] = style;
        return style;
    }

    /// <summary>주 버튼: 수선 공방 데님 리본(ws_button, 마우스를 올리면 밝아짐). 조각이 없으면 예전 보라 버튼.</summary>
    public GUIStyle Button(int fontSize = 24)
    {
        string key = "button:" + fontSize;
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        bool denim = tex.ContainsKey("ws_button");
        style = new GUIStyle(denim ? FrameStyle("ws_button", "ws_button_hover") : FrameStyle("button_primary", "button_primary_hover"))
            { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        if (denim) style.padding = new RectOffset(48, 48, 0, 6); // 양끝 리본 꼬리를 피해 가운데에
        style.normal.textColor = style.hover.textColor = style.active.textColor = Light;
        styles[key] = style;
        return style;
    }

    public GUIStyle Text(int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
    {
        string key = $"text:{size}:{color}:{anchor}:{bold}:{wrap}";
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, wordWrap = wrap, richText = true, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        // 글자는 클릭 대상이 아니므로 마우스를 올려도 색이 바뀌지 않게 모든 상태를 같은 색으로.
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
        styles[key] = style;
        return style;
    }

    /// <summary>가로 게이지(틀 + 채움).</summary>
    public void Gauge(Rect r, float t, string fill = "ws_gauge_fill")
    {
        Frame(r, "ws_gauge_track");
        float inner = r.width - 12f;
        if (t > 0.001f) Frame(new Rect(r.x + 6f, r.y + 6f, Mathf.Max(16f, inner * Mathf.Clamp01(t)), r.height - 12f), fill);
    }

    /// <summary>양피지 꼬리표·리넨 탭 모양 작은 버튼(잉크 글자). on이면 가죽 탭(밝은 글자).</summary>
    public GUIStyle Tab(bool on, int fontSize = 18)
    {
        string key = "tab:" + on + ":" + fontSize;
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(FrameStyle(on ? "ws_tab_on" : "ws_tab_off", on ? null : "ws_tab_on")) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        style.normal.textColor = on ? Light : Ink;
        style.hover.textColor = style.active.textColor = Light;
        styles[key] = style;
        return style;
    }

    public static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}
