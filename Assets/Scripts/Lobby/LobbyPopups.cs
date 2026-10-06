using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로비 메뉴의 팝업 4종(이어하기·새로하기·설정·종료). 아트는 로비 팝업 리소스 팩
/// (ArtSource/Lobby_popups → Tools/Art/import_lobby_popups.py → Assets/Art/Lobby/UI/Popups)이고,
/// 배치는 팩의 layout.json(1672×941 기준 좌표)을 그대로 쓴다. 글자는 이미지에 없고 여기서 그린다.
/// 이미지마다 실제 몸체 영역(popup_bounds.json)을 맞춰 그려서, 빛 번짐이 있는 '마우스 올림' 버튼도 같은 자리에 겹친다.
/// 새로하기·설정·종료는 새로하기의 일기장 틀로 통일하고, 모든 팝업은 화면 가운데에 놓는다.
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

    /// <summary>팝업을 화면 가운데로 옮기는 값(시안 좌표). 팝업마다 틀(패널+제목판) 범위로 계산한다.</summary>
    static float offX, offY;

    /// <summary>시안 좌표(1672×941) → 로비 배경(1920×1080) → 화면(UIKit) 좌표. 배경이 확대되면 같이 커진다.</summary>
    static Rect R(float x, float y, float w, float h)
    {
        const float k = 1920f / 1672f;
        Vector2 p = LayeredBackground.CanvasToScreen((x + offX) * k, (y + offY) * k);
        float z = k * LayeredBackground.Zoom;
        return new Rect(p.x, p.y, w * z, h * z);
    }

    static int Font(float size) => Mathf.Max(10, Mathf.RoundToInt(size * 1920f / 1672f * LayeredBackground.Zoom));

    static Rect R(Element e) => R(e.x, e.y, e.width, e.height);

    /// <summary>이어하기는 책 모양, 나머지(새로하기·설정·종료)는 새로하기 창의 일기장 모양으로 통일한다.</summary>
    string ArtPrefix => Open == Kind.Continue ? "continue_" : "new_";

    /// <summary>틀로 쓰는 배치: 이어하기는 자기 것, 나머지는 새로하기 배치.</summary>
    PopupLayout FrameLayout => Open == Kind.Continue ? layouts[Kind.Continue] : layouts[Kind.New];

    // ───────── 그리기 ─────────

    /// <summary>팝업을 그리고 눌린 버튼에 따른 할 일을 돌려준다. UIKit.Begin 안에서 부른다.</summary>
    public Action Draw(Vector2 mousePosition)
    {
        if (!IsOpen) return Action.None;
        mouse = mousePosition;
        PopupLayout layout = FrameLayout;
        CenterOn(layout);

        // 화면 전체를 어둡게(팝업이 열린 동안 로비 메뉴는 클릭을 받지 않는다).
        Prefixed("backdrop_dim", new Rect(0, 0, UIKit.Width, UIKit.Height), stretchWhole: true);
        var buttons = new List<Rect>();
        foreach (Element e in layout.elements)
        {
            if (e.id == "menu_marker" || e.id == "menu_underline") continue;
            if (e.id.StartsWith("button_")) { buttons.Add(R(e)); continue; }
            if (Open != Kind.New && e.id == "writing_divider") continue; // 새로하기 본문용 줄
            Prefixed(e.id, R(e));
        }
        SameSize(buttons);

        switch (Open)
        {
            case Kind.Continue: return DrawContinue(layout, buttons);
            case Kind.New: return DrawNew(layout, buttons);
            case Kind.Settings: DrawSettings(); return Action.None;
            case Kind.Quit: return DrawQuit(layout, buttons);
        }
        return Action.None;
    }

    /// <summary>패널·제목판 범위의 가운데가 화면(시안 1672×941) 가운데에 오도록 옮긴다.</summary>
    static void CenterOn(PopupLayout layout)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (Element e in layout.elements)
        {
            if (!e.id.StartsWith("panel_") && e.id != "header_plaque") continue;
            x0 = Mathf.Min(x0, e.x); y0 = Mathf.Min(y0, e.y);
            x1 = Mathf.Max(x1, e.x + e.width); y1 = Mathf.Max(y1, e.y + e.height);
        }
        if (x0 > x1) { offX = offY = 0f; return; }
        offX = 1672f / 2f - (x0 + x1) / 2f;
        offY = 941f / 2f - (y0 + y1) / 2f;
    }

    /// <summary>한 팝업 안의 버튼은 모두 같은 크기(가장 작은 버튼 크기)로, 자리(가운데)는 그대로.</summary>
    static void SameSize(List<Rect> buttons)
    {
        if (buttons.Count < 2) return;
        float w = float.MaxValue, h = float.MaxValue;
        foreach (Rect r in buttons) { w = Mathf.Min(w, r.width); h = Mathf.Min(h, r.height); }
        for (int i = 0; i < buttons.Count; i++)
            buttons[i] = new Rect(buttons[i].center.x - w / 2f, buttons[i].center.y - h / 2f, w, h);
    }

    Rect TitleRect(PopupLayout layout)
    {
        Element plaque = Array.Find(layout.elements, e => e.id == "header_plaque");
        if (plaque == null) return R(530, 315, 620, 60);
        // 새로하기 제목판은 위쪽에 달 장식이 있어서 글자는 아래쪽 80%에 둔다.
        return Open == Kind.Continue ? R(plaque) : R(plaque.x, plaque.y + plaque.height * 0.2f, plaque.width, plaque.height * 0.8f);
    }

    Action DrawContinue(PopupLayout layout, List<Rect> buttons)
    {
        GUI.Label(TitleRect(layout), "저장 기록", Text(32, Title, TextAnchor.MiddleCenter, true));

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
        GUI.Label(TitleRect(layout), "새로운 이야기", Text(32, Title, TextAnchor.MiddleCenter, true));
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

    Action DrawQuit(PopupLayout layout, List<Rect> buttons)
    {
        GUI.Label(TitleRect(layout), "종료", Text(32, Title, TextAnchor.MiddleCenter, true));
        GUI.Label(R(550, 470, 570, 60), "게임을 종료할까요?", Text(22, Ink, TextAnchor.MiddleCenter));
        if (buttons.Count >= 2)
        {
            if (Button(buttons[0], "종료")) return Action.Quit;
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

    /// <summary>설정 창: 새로하기와 같은 일기장 틀 안에 탭·항목을 배치한다(좌표는 새로하기 시안 기준).</summary>
    void DrawSettings()
    {
        GUI.Label(TitleRect(FrameLayout), "설정", Text(32, Title, TextAnchor.MiddleCenter, true));

        for (int i = 0; i < TabNames.Length; i++)
        {
            Rect r = R(570 + i * 190, 452, 170, 56);
            bool on = i == tab;
            Prefixed(on ? "tab_selected" : "tab_normal", r);
            GUI.Label(r, TabNames[i], Text(21, on ? TabOn : TabOff, TextAnchor.MiddleCenter, on));
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) tab = i;
        }

        if (tab == 0)
        {
            string[] names = { "전체 음량", "배경음", "효과음" };
            float[] tops = { 528, 584, 640 };
            for (int i = 0; i < 3; i++)
            {
                GUI.Label(R(580, tops[i], 150, 44), names[i], Text(21, Ink));
                float value = i == 0 ? master : i == 1 ? bgm : sfx;
                value = Slider(i, R(735, tops[i] + 9, 290, 26), value);
                if (i == 0) master = value; else if (i == 1) bgm = value; else sfx = value;
                GUI.Label(R(1040, tops[i], 80, 44), $"{Mathf.RoundToInt(value * 100f)}%", Text(19, Ink));
            }
        }
        else if (tab == 1)
        {
            float[] tops = { 522, 570, 618, 666 };
            Row(tops[0], "화면 모드", fullscreen ? "전체 화면" : "창 모드", () => fullscreen = !fullscreen);
            string res = resolutionIndex >= 0 && resolutionIndex < resolutions.Count ? $"{resolutions[resolutionIndex].x} × {resolutions[resolutionIndex].y}" : "-";
            Row(tops[1], "해상도", res, () => { if (resolutions.Count > 0) resolutionIndex = (resolutionIndex + 1) % resolutions.Count; });
            GUI.Label(R(580, tops[2], 200, 44), "밝기", Text(21, Ink));
            float t = Mathf.InverseLerp(GameSettings.MinBackgroundBrightness, GameSettings.MaxBackgroundBrightness, brightness);
            t = Slider(3, R(800, tops[2] + 9, 225, 26), t);
            brightness = Mathf.Lerp(GameSettings.MinBackgroundBrightness, GameSettings.MaxBackgroundBrightness, t);
            GUI.Label(R(1040, tops[2], 80, 44), $"{Mathf.RoundToInt(brightness * 100f)}%", Text(19, Ink));
            Row(tops[3], "텍스트 속도", GameSettings.TextSpeedNames[textSpeed], () => textSpeed = (textSpeed + 1) % GameSettings.TextSpeedNames.Length);
        }
        else
        {
            float[] tops = { 522, 568, 614, 660 };
            for (int i = 0; i < KeyNames.Length; i++)
            {
                GUI.Label(R(580, tops[i], 250, 42), KeyNames[i], Text(21, Ink));
                Rect field = R(930, tops[i] + 3, 190, 38);
                Box("key_field", field);
                GUI.Label(field, KeyValues[i], Text(18, Cream, TextAnchor.MiddleCenter));
            }
            GUI.Label(R(580, 700, 540, 26), "키 변경은 준비 중이에요", Text(15, InkSoft, TextAnchor.MiddleCenter));
        }

        // 버튼 세 개(같은 크기)
        string[] labels = { "적용", "취소", "기본값" };
        for (int i = 0; i < 3; i++)
        {
            Rect r = R(845 - 100 + (i - 1) * 210, 735, 200, 90);
            if (!Button(r, labels[i])) continue;
            if (i == 0) { ApplyDraft(); Close(); }
            else if (i == 1) Close();
            else ResetDraft();
        }
    }

    /// <summary>드롭다운 모양 칸. 누르면 다음 값으로 넘어간다. 칸은 늘려 그려서 글자가 안에 들어가게.</summary>
    void Row(float top, string label, string value, System.Action next)
    {
        GUI.Label(R(580, top, 220, 44), label, Text(21, Ink));
        Rect field = R(800, top + 3, 320, 38);
        Box("dropdown_field", field);
        GUI.Label(new Rect(field.x + field.height * 0.4f, field.y, field.width - field.height * 1.4f, field.height), value, Text(18, Cream));
        float arrow = field.height * 0.42f;
        Prefixed("dropdown_arrow", new Rect(field.xMax - arrow - field.height * 0.35f, field.center.y - arrow / 2f, arrow, arrow));
        if (GUI.Button(field, GUIContent.none, GUIStyle.none)) next();
    }

    /// <summary>
    /// 슬라이더: 트랙 위에 채움을 값만큼 잘라서 보이고, 손잡이는 따로 움직인다.
    /// 채움 그림은 트랙 안쪽에 꽉 차게 늘려 그려서 0%부터 바로 색이 칠해진다.
    /// </summary>
    float Slider(int id, Rect track, float value)
    {
        float inset = track.height * 0.14f;
        var fill = new Rect(track.x + inset, track.y + inset, track.width - inset * 2f, track.height - inset * 2f);
        float knobSize = track.height * 1.7f;
        Event e = Event.current;
        var hit = new Rect(track.x - knobSize / 2f, track.y - knobSize / 2f, track.width + knobSize, track.height + knobSize);
        if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(mouse)) { dragging = id; e.Use(); }
        if (e.type == EventType.MouseUp && dragging == id) dragging = -1;
        if (dragging == id && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.Repaint))
            value = Mathf.Clamp01((mouse.x - fill.x) / Mathf.Max(1f, fill.width));

        Body(FindName("slider_track"), track, false);
        if (value > 0.001f)
        {
            GUI.BeginClip(new Rect(fill.x, fill.y - fill.height, fill.width * value, fill.height * 3f));
            Body(FindName("slider_fill"), new Rect(0, fill.height, fill.width, fill.height), false);
            GUI.EndClip();
        }
        float cx = fill.x + fill.width * value;
        Prefixed("slider_knob", new Rect(cx - knobSize / 2f, track.center.y - knobSize / 2f, knobSize, knobSize));
        return value;
    }

    /// <summary>단순한 상자 그림(드롭다운·키 입력칸)을 모서리는 그대로 두고 늘려 그린다(9-slice).</summary>
    void Box(string id, Rect rect)
    {
        string name = FindName(id);
        Texture2D t;
        if (name == null || !tex.TryGetValue(name, out t)) return;
        string key = "box:" + name;
        GUIStyle style;
        if (!styles.TryGetValue(key, out style))
        {
            style = new GUIStyle { normal = { background = t }, border = new RectOffset(14, 14, 14, 14) };
            styles[key] = style;
        }
        if (Event.current.type == EventType.Repaint) style.Draw(rect, false, false, false, false);
    }

    // ───────── 버튼·이미지 ─────────

    /// <summary>
    /// 상태별 버튼(기본·마우스 올림·누름·비활성). 모든 상태 그림의 몸체를 같은 자리(rect 안 가운데)에 맞춘다.
    /// 마우스 올림은 크기 변화 없이 빛만 더해지고, 누름은 기본 그림을 살짝 어둡게 그린다.
    /// </summary>
    bool Button(Rect rect, string label, bool enabled = true)
    {
        bool hover = enabled && rect.Contains(mouse);
        bool pressed = hover && Input.GetMouseButton(0);
        string normal = ArtPrefix + "button_normal";
        Rect body = FitRect(normal, rect);
        if (!enabled) Body(ArtPrefix + "button_disabled", body, false);
        else if (pressed)
        {
            Color old = GUI.color;
            GUI.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            Body(normal, body, false);
            GUI.color = old;
        }
        else if (hover && tex.ContainsKey(ArtPrefix + "button_hover")) Body(ArtPrefix + "button_hover", body, false);
        else Body(normal, body, false);

        GUI.Label(body, label, Text(22, enabled ? ButtonText : Muted, TextAnchor.MiddleCenter));
        return enabled && GUI.Button(body, GUIContent.none, GUIStyle.none);
    }

    /// <summary>그림 이름 찾기: 지금 팝업 모양(ArtPrefix) → 아무 팝업이나 같은 이름.</summary>
    string FindName(string id)
    {
        if (tex.ContainsKey(ArtPrefix + id)) return ArtPrefix + id;
        foreach (string p in Prefix)
            if (p.Length > 0 && tex.ContainsKey(p + id)) return p + id;
        return null;
    }

    void Prefixed(string id, Rect rect, bool stretchWhole = false)
    {
        string name = FindName(id);
        if (name == null) return;
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
        // 글자는 클릭 대상이 아니므로 마우스를 올려도 색이 바뀌지 않게 모든 상태를 같은 색으로.
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
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
