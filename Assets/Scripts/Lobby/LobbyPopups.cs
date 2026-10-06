using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로비 메뉴의 팝업 4종(이어하기·새로하기·설정·종료). 아트는 로비 팝업 리소스 팩
/// (ArtSource/Lobby_popups → Tools/Art/import_lobby_popups.py → Assets/Art/Lobby/UI/Popups)이고,
/// 배치는 팩의 layout.json(1672×941 기준 좌표)을 그대로 쓴다. 글자는 이미지에 없고 여기서 그린다.
/// 이미지마다 실제 몸체 영역(popup_bounds.json)을 맞춰 그려서, 빛 번짐이 있는 '마우스 올림' 버튼도 같은 자리에 겹친다.
/// </summary>
public class LobbyPopups
{
    public enum Kind { None, Continue, New, Settings, Quit }

    /// <summary>팝업 버튼을 눌렀을 때 로비가 할 일.</summary>
    public enum Action { None, Continue, StartNewGame, Quit }

    [Serializable] class Element { public string id; public float x, y, width, height; }
    [Serializable] class PopupLayout { public float[] reference_resolution = { 1672, 941 }; public Element[] elements = new Element[0]; }
    [Serializable] class Bound { public string name; public int texWidth, texHeight, x, y, width, height; }
    [Serializable] class BoundList { public Bound[] items = new Bound[0]; }

    static readonly string[] Prefix = { "", "continue_", "new_", "settings_", "quit_" };
    static readonly string[] TabNames = { "사운드", "화면", "조작" };
    static readonly string[] KeyNames = { "이동", "상호작용", "타워 건설", "도하 스킬" };
    static readonly string[] KeyValues = { "W A S D", "E", "1 – 5", "Q" };

    static readonly Color Title = UISkin.Hex("#E7C383"), ButtonText = UISkin.Hex("#F4DBAB"), Ink = UISkin.Hex("#33261E"),
        InkSoft = UISkin.Hex("#5A4636"), Cream = UISkin.Hex("#EDD5A7"), TabOn = UISkin.Hex("#FFF0BE"), TabOff = UISkin.Hex("#A79B88"),
        Warn = UISkin.Hex("#8A3A2A"), Muted = UISkin.Hex("#8E8577");

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, Bound> bounds = new Dictionary<string, Bound>();
    readonly Dictionary<Kind, PopupLayout> layouts = new Dictionary<Kind, PopupLayout>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    public Kind Open { get; private set; }
    public bool IsOpen => Open != Kind.None;

    // 설정 창 임시값(적용을 눌러야 저장)
    int tab;
    float master, bgm, sfx, brightness;
    bool fullscreen;
    int textSpeed;
    List<Vector2Int> resolutions = new List<Vector2Int>();
    int resolutionIndex;
    int dragging = -1;
    Vector2 mouse;

    public LobbyPopups(IEnumerable<Sprite> sprites, TextAsset boundJson, TextAsset continueLayout, TextAsset newLayout, TextAsset settingsLayout, TextAsset quitLayout)
    {
        if (sprites != null)
            foreach (Sprite sprite in sprites)
                if (sprite != null) tex[sprite.name] = sprite.texture;
        if (boundJson != null)
            foreach (Bound b in JsonUtility.FromJson<BoundList>(boundJson.text).items) bounds[b.name] = b;
        AddLayout(Kind.Continue, continueLayout);
        AddLayout(Kind.New, newLayout);
        AddLayout(Kind.Settings, settingsLayout);
        AddLayout(Kind.Quit, quitLayout);
    }

    void AddLayout(Kind kind, TextAsset json) =>
        layouts[kind] = json != null ? JsonUtility.FromJson<PopupLayout>(json.text) : new PopupLayout();

    /// <summary>아트가 들어 있으면 true(없으면 로비가 예전 방식으로 동작).</summary>
    public bool HasArt => tex.ContainsKey("continue_panel_notebook");

    public bool HasTexture(string name) => tex.ContainsKey(name);

    public void Show(Kind kind)
    {
        Open = kind;
        if (kind == Kind.Settings) LoadDraft();
    }

    public void Close() => Open = Kind.None;

    // ───────── 좌표 ─────────

    /// <summary>시안 좌표(1672×941) → 로비 배경(1920×1080) → 화면(UIKit) 좌표. 배경이 확대되면 같이 커진다.</summary>
    static Rect R(float x, float y, float w, float h)
    {
        const float k = 1920f / 1672f;
        Vector2 p = LayeredBackground.CanvasToScreen(x * k, y * k);
        float z = k * LayeredBackground.Zoom;
        return new Rect(p.x, p.y, w * z, h * z);
    }

    static int Font(float size) => Mathf.Max(10, Mathf.RoundToInt(size * 1920f / 1672f * LayeredBackground.Zoom));

    static Rect R(Element e) => R(e.x, e.y, e.width, e.height);

    // ───────── 그리기 ─────────

    /// <summary>팝업을 그리고 눌린 버튼에 따른 할 일을 돌려준다. UIKit.Begin 안에서 부른다.</summary>
    public Action Draw(Vector2 mousePosition)
    {
        if (!IsOpen) return Action.None;
        mouse = mousePosition;
        // 화면 전체를 어둡게(팝업이 열린 동안 로비 메뉴는 클릭을 받지 않는다).
        Prefixed("backdrop_dim", new Rect(0, 0, UIKit.Width, UIKit.Height), stretchWhole: true);
        PopupLayout layout = layouts[Open];
        var buttons = new List<Rect>();
        var tabs = new List<Rect>();
        var sliders = new List<Rect[]>();
        foreach (Element e in layout.elements)
        {
            if (e.id == "menu_marker" || e.id == "menu_underline") continue;
            if (e.id.StartsWith("button_")) { buttons.Add(R(e)); continue; }
            if (e.id.StartsWith("tab_")) { tabs.Add(R(e)); continue; }
            if (e.id == "slider_track") { sliders.Add(new Rect[3]); sliders[sliders.Count - 1][0] = R(e); continue; }
            if (e.id == "slider_fill" && sliders.Count > 0) { sliders[sliders.Count - 1][1] = R(e); continue; }
            if (e.id == "slider_knob" && sliders.Count > 0) { sliders[sliders.Count - 1][2] = R(e); continue; }
            if (Open == Kind.Settings && e.id == "row_divider") continue;
            Prefixed(e.id, R(e));
        }

        switch (Open)
        {
            case Kind.Continue: return DrawContinue(layout, buttons);
            case Kind.New: return DrawNew(layout, buttons);
            case Kind.Settings: DrawSettings(buttons, tabs, sliders); return Action.None;
            case Kind.Quit: return DrawQuit(buttons);
        }
        return Action.None;
    }

    Action DrawContinue(PopupLayout layout, List<Rect> buttons)
    {
        Element plaque = Array.Find(layout.elements, e => e.id == "header_plaque");
        GUI.Label(plaque != null ? R(plaque) : R(530, 315, 620, 60), "저장 기록", Text(32, Title, TextAnchor.MiddleCenter, true));

        DateTime? saved = SaveInfo.LastSaved;
        string[] labels = { "진행 중 의뢰", "마지막 저장 위치", "플레이 시간", "마지막 저장" };
        string[] values =
        {
            SaveInfo.CurrentQuestTitle,
            SaveInfo.LastLocation,
            SaveInfo.FormatPlayTime(SaveInfo.PlaySeconds),
            saved.HasValue ? saved.Value.ToString("yyyy.MM.dd HH:mm") : "-",
        };
        float[] tops = { 436, 498, 560, 622 };
        for (int i = 0; i < 4; i++)
        {
            GUI.Label(R(615, tops[i], 250, 44), labels[i], Text(20, Ink));
            GUI.Label(R(845, tops[i], 250, 44), values[i], Text(20, Ink, TextAnchor.MiddleRight, true));
        }

        if (buttons.Count >= 2)
        {
            if (Button(buttons[0], "이어하기")) return Action.Continue;
            if (Button(buttons[1], "돌아가기")) Close();
        }
        return Action.None;
    }

    Action DrawNew(PopupLayout layout, List<Rect> buttons)
    {
        Element plaque = Array.Find(layout.elements, e => e.id == "header_plaque");
        GUI.Label(plaque != null ? R(plaque.x, plaque.y + plaque.height * 0.2f, plaque.width, plaque.height * 0.8f) : R(530, 315, 620, 60),
            "새로운 이야기", Text(32, Title, TextAnchor.MiddleCenter, true));
        GUI.Label(R(550, 450, 570, 60), "끝내지 못한 꿈을 수선하는\n이야기를 시작합니다.", Text(20, Ink, TextAnchor.MiddleCenter, false, true));
        if (LobbyMenu.HasSave)
            GUI.Label(R(550, 527, 570, 38), "지금까지의 진행 기록은 지워져요.", Text(18, Warn, TextAnchor.MiddleCenter));

        if (buttons.Count >= 2)
        {
            if (Button(buttons[0], "새 이야기 시작")) return Action.StartNewGame;
            if (Button(buttons[1], "돌아가기")) Close();
        }
        return Action.None;
    }

    Action DrawQuit(List<Rect> buttons)
    {
        GUI.Label(R(530, 405, 620, 54), "종료", Text(32, Title, TextAnchor.MiddleCenter, true));
        GUI.Label(R(550, 515, 570, 46), "게임을 종료할까요?", Text(22, Cream, TextAnchor.MiddleCenter));
        if (buttons.Count >= 2)
        {
            if (Button(buttons[0], "종료", pressedImage: "quit_button_pressed")) return Action.Quit;
            if (Button(buttons[1], "돌아가기")) Close();
        }
        return Action.None;
    }

    // ───────── 설정 ─────────

    void LoadDraft()
    {
        tab = 0;
        master = GameSettings.MasterVolume;
        bgm = GameSettings.BgmVolume;
        sfx = GameSettings.SfxVolume;
        brightness = GameSettings.BackgroundBrightness;
        fullscreen = Screen.fullScreen;
        textSpeed = GameSettings.TextSpeed;

        resolutions = new List<Vector2Int>();
        foreach (Resolution r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }
        var current = new Vector2Int(Screen.width, Screen.height);
        if (!resolutions.Contains(current)) resolutions.Add(current);
        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        resolutionIndex = resolutions.IndexOf(current);
    }

    void ResetDraft()
    {
        master = GameSettings.DefaultMasterVolume;
        bgm = GameSettings.DefaultBgmVolume;
        sfx = GameSettings.DefaultSfxVolume;
        brightness = 1f;
        textSpeed = GameSettings.DefaultTextSpeed;
        fullscreen = true;
    }

    void ApplyDraft()
    {
        GameSettings.MasterVolume = master;
        GameSettings.BgmVolume = bgm;
        GameSettings.SfxVolume = sfx;
        GameSettings.BackgroundBrightness = brightness;
        GameSettings.TextSpeed = textSpeed;
        GameSettings.Save();

        Vector2Int size = resolutionIndex >= 0 && resolutionIndex < resolutions.Count ? resolutions[resolutionIndex] : new Vector2Int(Screen.width, Screen.height);
        FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (size.x != Screen.width || size.y != Screen.height || fullscreen != Screen.fullScreen)
            Screen.SetResolution(size.x, size.y, mode);
    }

    void DrawSettings(List<Rect> buttons, List<Rect> tabs, List<Rect[]> sliders)
    {
        GUI.Label(R(530, 318, 620, 50), "설정", Text(32, Title, TextAnchor.MiddleCenter, true));

        for (int i = 0; i < tabs.Count && i < TabNames.Length; i++)
        {
            bool on = i == tab;
            Prefixed(on ? "tab_selected" : "tab_normal", tabs[i]);
            GUI.Label(tabs[i], TabNames[i], Text(23, on ? TabOn : TabOff, TextAnchor.MiddleCenter, on));
            if (GUI.Button(tabs[i], GUIContent.none, GUIStyle.none)) tab = i;
        }

        if (tab == 0)
        {
            string[] names = { "전체 음량", "배경음", "효과음" };
            float[] tops = { 501, 573, 645 };
            for (int i = 0; i < 3 && i < sliders.Count; i++)
            {
                GUI.Label(R(550, tops[i], 150, 45), names[i], Text(23, Cream));
                float value = i == 0 ? master : i == 1 ? bgm : sfx;
                value = Slider(i, sliders[i], value);
                if (i == 0) master = value; else if (i == 1) bgm = value; else sfx = value;
                GUI.Label(R(1045, tops[i], 80, 45), $"{Mathf.RoundToInt(value * 100f)}%", Text(20, Cream));
            }
        }
        else if (tab == 1)
        {
            float[] tops = { 478, 532, 586, 640 };
            Row(tops[0], "화면 모드", fullscreen ? "전체 화면" : "창 모드", () => fullscreen = !fullscreen);
            string res = resolutionIndex >= 0 && resolutionIndex < resolutions.Count ? $"{resolutions[resolutionIndex].x} × {resolutions[resolutionIndex].y}" : "-";
            Row(tops[1], "해상도", res, () => { if (resolutions.Count > 0) resolutionIndex = (resolutionIndex + 1) % resolutions.Count; });
            Row(tops[3], "텍스트 속도", GameSettings.TextSpeedNames[textSpeed], () => textSpeed = (textSpeed + 1) % GameSettings.TextSpeedNames.Length);

            GUI.Label(R(565, tops[2], 200, 45), "밝기", Text(23, Cream));
            Rect track = R(800, tops[2] + 9, 220, 28);
            var parts = new[] { track, R(804, tops[2] + 12, 212, 22), R(0, 0, 44, 44) };
            float t = Mathf.InverseLerp(GameSettings.MinBackgroundBrightness, GameSettings.MaxBackgroundBrightness, brightness);
            t = Slider(3, parts, t);
            brightness = Mathf.Lerp(GameSettings.MinBackgroundBrightness, GameSettings.MaxBackgroundBrightness, t);
            GUI.Label(R(1035, tops[2], 80, 45), $"{Mathf.RoundToInt(brightness * 100f)}%", Text(20, Cream));
        }
        else
        {
            float[] tops = { 470, 520, 570, 620 };
            for (int i = 0; i < KeyNames.Length; i++)
            {
                GUI.Label(R(565, tops[i], 250, 44), KeyNames[i], Text(23, Cream));
                Rect field = R(965, tops[i] + 2, 140, 40);
                Prefixed("key_field", field);
                GUI.Label(field, KeyValues[i], Text(19, Cream, TextAnchor.MiddleCenter));
            }
            GUI.Label(R(565, 664, 540, 30), "키 변경은 준비 중이에요", Text(16, Muted, TextAnchor.MiddleCenter));
        }

        if (buttons.Count >= 3)
        {
            if (Button(buttons[0], "적용")) { ApplyDraft(); Close(); }
            if (Button(buttons[1], "취소")) Close();
            if (Button(buttons[2], "기본값")) ResetDraft();
        }
    }

    /// <summary>드롭다운 모양 칸. 누르면 다음 값으로 넘어간다.</summary>
    void Row(float top, string label, string value, System.Action next)
    {
        GUI.Label(R(565, top, 220, 45), label, Text(23, Cream));
        Rect field = R(805, top + 2, 300, 41);
        Prefixed("dropdown_field", field);
        GUI.Label(new Rect(field.x + 16, field.y, field.width - 56, field.height), value, Text(19, Cream));
        float arrow = field.height * 0.45f;
        Prefixed("dropdown_arrow", new Rect(field.xMax - arrow - 14, field.center.y - arrow / 2f, arrow, arrow));
        if (GUI.Button(field, GUIContent.none, GUIStyle.none)) next();
    }

    /// <summary>parts = [트랙, 채움, 손잡이]. 채움은 값만큼 잘라서 보이고, 손잡이는 따로 움직인다.</summary>
    float Slider(int id, Rect[] parts, float value)
    {
        Rect track = parts[0];
        Rect fill = parts[1].width > 0 ? parts[1] : track;
        float knobSize = parts[2].width > 0 ? parts[2].width : track.height * 1.7f;
        Event e = Event.current;
        Rect hit = new Rect(track.x - knobSize / 2f, track.y - knobSize / 2f, track.width + knobSize, track.height + knobSize);
        if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(mouse)) { dragging = id; e.Use(); }
        if (e.type == EventType.MouseUp && dragging == id) dragging = -1;
        if (dragging == id && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.Repaint))
            value = Mathf.Clamp01((mouse.x - fill.x) / Mathf.Max(1f, fill.width));

        Prefixed("slider_track", track);
        if (value > 0.001f)
        {
            GUI.BeginClip(new Rect(fill.x, fill.y - fill.height, fill.width * value, fill.height * 3f));
            Prefixed("slider_fill", new Rect(0, fill.height, fill.width, fill.height));
            GUI.EndClip();
        }
        float cx = fill.x + fill.width * value;
        Prefixed("slider_knob", new Rect(cx - knobSize / 2f, track.center.y - knobSize / 2f, knobSize, knobSize));
        return value;
    }

    // ───────── 버튼·이미지 ─────────

    /// <summary>
    /// 상태별 버튼(기본·마우스 올림·누름·비활성). 모든 상태 그림의 몸체를 같은 자리(rect 안 가운데)에 맞춘다.
    /// 누름은 따로 그림이 없으면 기본 그림을 어둡게, 0.98배로 그린다.
    /// </summary>
    bool Button(Rect rect, string label, bool enabled = true, string pressedImage = null)
    {
        bool hover = enabled && rect.Contains(mouse);
        bool pressed = hover && Input.GetMouseButton(0);
        string prefix = Prefix[(int)Open];
        if (!enabled) Prefixed("button_disabled", rect);
        else if (pressed && pressedImage != null && tex.ContainsKey(pressedImage)) Body(pressedImage, rect, true);
        else if (pressed)
        {
            Color old = GUI.color;
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            Rect small = new Rect(rect.center.x - rect.width * 0.49f, rect.center.y - rect.height * 0.49f, rect.width * 0.98f, rect.height * 0.98f);
            Prefixed("button_normal", small);
            GUI.color = old;
        }
        else if (hover && tex.ContainsKey(prefix + "button_hover"))
        {
            // 마우스 올림 그림의 몸체를 기본 그림 몸체 자리에 맞춘다(빛 번짐은 바깥으로 퍼진다).
            Rect body = FitRect(prefix + "button_normal", rect);
            Body(prefix + "button_hover", body, false);
        }
        else Prefixed("button_normal", rect);

        GUI.Label(rect, label, Text(22, enabled ? ButtonText : Muted, TextAnchor.MiddleCenter));
        return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    /// <summary>지금 팝업의 이름 앞붙이(continue_ 등)를 붙인 그림을 그린다. 없으면 다른 팝업의 같은 그림을 쓴다.</summary>
    void Prefixed(string id, Rect rect, bool stretchWhole = false)
    {
        string name = Prefix[(int)Open] + id;
        if (!tex.ContainsKey(name))
        {
            name = null;
            foreach (string p in Prefix)
                if (p.Length > 0 && tex.ContainsKey(p + id)) { name = p + id; break; }
            if (name == null) return;
        }
        if (stretchWhole) GUI.DrawTexture(rect, tex[name], ScaleMode.StretchToFill);
        else Body(name, FitRect(name, rect), false);
    }

    /// <summary>rect 안에 그림 몸체의 비율대로 들어가는 가운데 영역.</summary>
    Rect FitRect(string name, Rect rect)
    {
        Bound b;
        if (!bounds.TryGetValue(name, out b) || b.width <= 0 || b.height <= 0) return rect;
        float aspect = (float)b.width / b.height;
        float w = rect.width, h = rect.width / aspect;
        if (h > rect.height) { h = rect.height; w = h * aspect; }
        return new Rect(rect.center.x - w / 2f, rect.center.y - h / 2f, w, h);
    }

    /// <summary>그림 몸체가 target에 꼭 맞도록(투명 여백·빛 번짐은 바깥으로) 그림 전체를 그린다.</summary>
    void Body(string name, Rect target, bool fit)
    {
        Texture2D t;
        if (!tex.TryGetValue(name, out t) || Event.current.type != EventType.Repaint) return;
        if (fit) target = FitRect(name, target);
        Bound b;
        if (!bounds.TryGetValue(name, out b) || b.width <= 0 || b.height <= 0)
        {
            GUI.DrawTexture(target, t, ScaleMode.ScaleToFit);
            return;
        }
        float sx = target.width / b.width, sy = target.height / b.height;
        GUI.DrawTexture(new Rect(target.x - b.x * sx, target.y - b.y * sy, b.texWidth * sx, b.texHeight * sy), t, ScaleMode.StretchToFill);
    }

    GUIStyle Text(float size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
    {
        int px = Font(size);
        string key = $"{px}:{color}:{anchor}:{bold}:{wrap}";
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(GUI.skin.label) { fontSize = px, alignment = anchor, wordWrap = wrap, richText = true, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        style.normal.textColor = color;
        styles[key] = style;
        return style;
    }

    // ───────── 메뉴 표시 ─────────

    /// <summary>메뉴 항목 옆 별 표시와 밑줄(팩의 menu_marker, menu_underline). rect는 메뉴 글자 그림 영역.</summary>
    public void DrawMenuMarker(Rect item)
    {
        float z = LayeredBackground.Zoom;
        float size = 34f * z;
        var marker = new Rect(item.x - 31f * z - size / 2f, item.center.y + 4f * z - size / 2f, size, size);
        var underline = new Rect(item.x - 31f * z, item.center.y + 22f * z, 304f * z, 28f * z);
        Body("continue_menu_underline", underline, true);
        Body("continue_menu_marker", marker, true);
    }
}
