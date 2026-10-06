using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인(수선소) 화면 UI. 기획 시안(ArtSource/Main_UI/reference/lobby_mockup.jpg)과
/// 메인 UI 벡터 팩(ArtSource/Main_UI/*.svg → Tools/Art/build_main_ui.py → Assets/Art/Main/UI)을 쓴다.
/// IMGUI, 1080p 기준 좌표. 오른쪽 요소는 화면 오른쪽 끝 기준으로 놓아 화면비가 달라도 붙어 있다.
/// - 왼쪽 위: 로고, 해명도 게이지 / 오른쪽 위: 재화 바(결정·동전·열쇠), 설정
/// - 오른쪽: 의뢰함(전체/진행 중/완료) / 가운데: 의뢰 상세(편지) → "의뢰 시작하기"
/// - 왼쪽 아래: 도하 말풍선 / 아래: 탭(수선소·의뢰함·도감·가방·기술·지도)
/// </summary>
public class MainUI : MonoBehaviour
{
    enum Filter { All, InProgress, Completed }

    [Serializable]
    class BorderItem { public string name; public int border; }

    [Serializable]
    class BorderList { public BorderItem[] items = new BorderItem[0]; }

    static readonly string[] TabNames = { "수선소", "의뢰함", "도감", "가방", "기술", "지도" };
    static readonly string[] TabIcons = { "icon_workshop", "icon_envelope", "icon_codex", "icon_bag", "icon_mastery", "icon_map" };
    static readonly string[] RewardIcons = { "reward_moon", "reward_thread", "reward_note" };

    static readonly Color Light = Hex("#F3EBDD"), Muted = Hex("#948AA8"), Ink = Hex("#2A2330"), InkMuted = Hex("#5A4E57"), Gold = Hex("#E0BE7C");

    [Tooltip("Assets/Art/Main/UI 의 조각 이미지")]
    public Sprite[] uiSprites = new Sprite[0];
    [Tooltip("Assets/Art/Main/UI/ui_borders.json (9-slice 모서리 폭)")]
    public TextAsset uiBorders;

    public string subtitle = "잊힌 꿈도, 다시 꿰맬 수 있으니까.";
    [TextArea(1, 3)]
    public string[] dohaLines =
    {
        "오늘은 어떤 꿈을 고쳐볼까?",
        "편지가 또 왔어. 같이 읽어볼래?",
        "옆구리 실이 조금 풀렸어… 이따 꿰매 줄래?",
        "오른쪽 의뢰함에서 편지를 골라 봐.",
    };

    Filter filter;
    QuestData selected;
    int dohaLine;
    bool settingsOpen;
    /// <summary>의뢰함(목록+상세)이 열려 있는지. 의뢰함 탭을 다시 누르거나 수선소 탭을 누르면 닫힌다.</summary>
    bool questsOpen = true;
    readonly SettingsPanel settings = new SettingsPanel();
    string toast;
    float toastUntil;

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, int> borders = new Dictionary<string, int>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
    GUIStyle logo, logoSub;

    void Start()
    {
        Time.timeScale = 1f;
        foreach (Sprite sprite in uiSprites)
            if (sprite != null) tex[sprite.name] = sprite.texture;
        if (uiBorders != null)
            foreach (BorderItem item in JsonUtility.FromJson<BorderList>(uiBorders.text).items)
                borders[item.name] = item.border;

        foreach (QuestData quest in QuestDatabase.Instance.quests)
            if (quest != null && quest.IsAvailable) { selected = quest; break; }
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.Repaint || e.type == EventType.MouseMove)
            LayeredBackground.Pointer = new Vector2(e.mousePosition.x / Mathf.Max(1, Screen.width), e.mousePosition.y / Mathf.Max(1, Screen.height));

        UIKit.Begin();
        EnsureStyles();
        float w = UIKit.Width, h = UIKit.Height;

        GUI.enabled = !settingsOpen;
        DrawLogo();
        DrawGauge(new Rect(48, 196, 420, 128));
        DrawResources(w);
        if (questsOpen)
        {
            DrawQuestList(new Rect(w - 470, 112, 440, 800));
            if (selected != null) DrawDetail(new Rect(w - 960, 112, 470, 800));
        }
        DrawDohaBubble(new Rect(48, h - 262, 560, 112));
        DrawNav(w, h);
        DrawToast(w, h);
        GUI.enabled = true;

        if (settingsOpen) DrawSettings(w, h);
        UIKit.End();

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            SetSettings(!settingsOpen);
            e.Use();
        }
    }

    // ───────── 영역 ─────────

    void DrawLogo()
    {
        // 로비 아트에서 잘라낸 붓글씨 로고(Assets/Art/Lobby/logo.png). 없으면 글자로 대신한다.
        if (tex.ContainsKey("logo")) Icon(new Rect(36, 14, 470, 150), "logo");
        else UIKit.ShadowLabel(new Rect(48, 22, 700, 100), "악몽수선소", logo);
        GUI.Label(new Rect(60, 150, 700, 36), subtitle, logoSub);
    }

    void DrawGauge(Rect r)
    {
        // 해명도: 의뢰의 진실을 밝혀 얻는 점수(기획서 26번). 진행도 연결은 알파 단계.
        float percent = 0f;
        Frame(r, "panel_indigo");
        Icon(new Rect(r.x + 16, r.y + 22, 84, 84), "moon_emblem");
        GUI.Label(new Rect(r.x + 116, r.y + 14, 200, 40), "해명도", Style("label", 28, Light, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(r.xMax - 120, r.y + 14, 96, 40), $"{percent * 100f:0}%", Style("label", 24, Light, TextAnchor.MiddleRight));
        var track = new Rect(r.x + 116, r.y + 60, r.width - 140, 24);
        Frame(track, "gauge_track");
        if (percent > 0f) Frame(new Rect(track.x + 4, track.y + 4, (track.width - 8) * percent, track.height - 8), "gauge_fill");
        GUI.Label(new Rect(r.x + 116, r.y + 88, r.width - 130, 30), "의뢰의 진실을 밝혀 획득", Style("label", 18, Muted, TextAnchor.MiddleLeft));
    }

    void DrawResources(float w)
    {
        var bar = new Rect(w - 660, 24, 470, 72);
        Frame(bar, "bar_indigo");
        // 재화 종류·이름은 기획 확인 중. 저장(Wallet) 연결은 4주차.
        DrawResource(new Rect(bar.x + 30, bar.y + 14, 26, 44), "icon_crystal", PlayerPrefs.GetInt("Wallet.Crystal", 0));
        DrawResource(new Rect(bar.x + 176, bar.y + 18, 36, 36), "icon_coin", PlayerPrefs.GetInt("Wallet.Coin", 0));
        DrawResource(new Rect(bar.x + 330, bar.y + 22, 46, 28), "icon_key", PlayerPrefs.GetInt("Wallet.Key", 0));

        if (GUI.Button(new Rect(w - 176, 24, 70, 72), "설정", TabStyle(false))) SetSettings(true);
        if (GUI.Button(new Rect(w - 98, 24, 70, 72), "전체", TabStyle(false))) GameSettings.Fullscreen = !GameSettings.Fullscreen;
    }

    void DrawResource(Rect icon, string iconName, int value)
    {
        Icon(icon, iconName);
        GUI.Label(new Rect(icon.xMax + 12, icon.center.y - 20, 100, 40), value.ToString("N0"), Style("label", 24, Light, TextAnchor.MiddleLeft));
    }

    void DrawQuestList(Rect r)
    {
        Frame(r, "panel_indigo");
        Icon(new Rect(r.x + 28, r.y + 26, 48, 34), "envelope_small");
        GUI.Label(new Rect(r.x + 90, r.y + 18, 200, 48), "의뢰함", Style("label", 30, Light, TextAnchor.MiddleLeft, true));
        QuestData[] quests = QuestDatabase.Instance.quests;
        int open = 0;
        foreach (QuestData q in quests) if (q != null && q.stageIndex >= 0) open++;
        GUI.Label(new Rect(r.xMax - 140, r.y + 18, 112, 48), $"{open} / {QuestDatabase.Instance.capacity}", Style("label", 20, Muted, TextAnchor.MiddleRight));
        Frame(new Rect(r.x + 26, r.y + 76, r.width - 52, 2), "line_gold");

        string[] names = { "전체", "진행 중", "완료" };
        for (int i = 0; i < names.Length; i++)
            if (GUI.Button(new Rect(r.x + 26 + i * 132, r.y + 94, 120, 46), names[i], TabStyle((int)filter == i))) filter = (Filter)i;

        float y = r.y + 160;
        int shown = 0;
        foreach (QuestData quest in quests)
        {
            if (quest == null || !Matches(quest)) continue;
            float cardHeight = quest == selected ? 112 : 94;
            if (y + cardHeight > r.yMax - 16) break;
            DrawQuestCard(new Rect(r.x + 22, y, r.width - 44, cardHeight), quest);
            y += cardHeight + 14;
            shown++;
        }
        if (shown == 0)
            GUI.Label(new Rect(r.x + 30, y + 10, r.width - 60, 50), "해당하는 의뢰가 없어요.", Style("label", 20, Muted, TextAnchor.MiddleLeft));
    }

    void DrawQuestCard(Rect r, QuestData quest)
    {
        bool locked = !quest.IsAvailable && !quest.IsCompleted;
        bool isSelected = quest == selected;
        Color old = GUI.color;
        if (locked) GUI.color = new Color(1f, 1f, 1f, 0.45f);

        if (GUI.Button(r, GUIContent.none, isSelected ? FrameStyle("card_paper") : FrameStyle("card_dark", "card_dark_hover")))
        {
            if (locked) ShowToast(quest.stageIndex < 0 ? "아직 도착하지 않은 의뢰예요" : "앞의 의뢰를 먼저 해결해 주세요");
            else selected = quest;
        }

        float textX = r.x + 26;
        if (isSelected)
        {
            var thumb = new Rect(r.x + 14, r.y + 14, r.height - 28, r.height - 28);
            if (quest.thumbnail != null) GUI.DrawTexture(thumb, quest.thumbnail.texture, ScaleMode.ScaleAndCrop);
            else Icon(thumb, "thumb_placeholder");
            textX = thumb.xMax + 18;
        }
        Color titleColor = isSelected ? Ink : Light, tagColor = isSelected ? InkMuted : Muted;
        GUI.Label(new Rect(textX, r.y + r.height * 0.16f, r.xMax - textX - 60, 38), quest.title, Style("label", isSelected ? 24 : 21, titleColor, TextAnchor.MiddleLeft, isSelected));
        GUI.Label(new Rect(textX, r.y + r.height * 0.56f, r.xMax - textX - 60, 28), Tags(quest), Style("label", 16, tagColor, TextAnchor.MiddleLeft));

        if (!locked)
        {
            string state = quest.IsCompleted ? "완료" : quest.IsInProgress ? "진행 중" : "";
            if (state.Length > 0)
                GUI.Label(new Rect(r.xMax - 150, r.y + 8, 96, 26), state, Style("label", 15, tagColor, TextAnchor.MiddleRight));
            Icon(new Rect(r.xMax - 50, r.center.y - 17, 34, 34), isSelected ? "diamond_on" : "diamond_off");
        }
        GUI.color = old;
    }

    void DrawDetail(Rect r)
    {
        Frame(r, "panel_paper");
        Frame(new Rect(r.x + 28, r.y + 52, r.width - 56, 2), "line_gold");

        string status = selected.IsCompleted ? "완료" : selected.IsInProgress ? "진행 중" : "진행 가능";
        var badge = new Rect(r.center.x - 76, r.y + 30, 152, 38);
        Frame(badge, "badge");
        GUI.Label(badge, status, Style("label", 16, Light, TextAnchor.MiddleCenter));

        GUI.Label(new Rect(r.x + 24, r.y + 80, r.width - 48, 50), selected.title, Style("label", 32, Ink, TextAnchor.MiddleCenter, true));
        DrawTagChips(new Rect(r.x + 24, r.y + 136, r.width - 48, 28));

        var photo = new Rect(r.x + 36, r.y + 182, r.width - 72, 200);
        if (selected.photo != null) GUI.DrawTexture(photo, selected.photo.texture, ScaleMode.ScaleAndCrop);
        else Icon(photo, "photo_placeholder");

        GUIStyle body = Style("body", 20, Ink, TextAnchor.UpperLeft, false, true);
        float letterHeight = Mathf.Min(130f, body.CalcHeight(new GUIContent(selected.letter), r.width - 80));
        GUI.Label(new Rect(r.x + 40, r.y + 398, r.width - 80, letterHeight), selected.letter, body);

        if (selected.rewards.Length > 0)
        {
            float y = r.y + 538;
            GUI.Label(new Rect(r.x + 40, y, 200, 30), "의뢰 보상", Style("label", 20, Ink, TextAnchor.MiddleLeft, true));
            for (int i = 0; i < selected.rewards.Length && i < 4; i++)
            {
                var slot = new Rect(r.x + 40 + i * 98, y + 38, 82, 82);
                Frame(slot, "slot_reward");
                Icon(new Rect(slot.x + 17, slot.y + 17, 48, 48), RewardIcons[i % RewardIcons.Length]);
                GUI.Label(new Rect(slot.x - 8, slot.yMax + 2, slot.width + 16, 22), selected.rewards[i], Style("label", 13, InkMuted, TextAnchor.MiddleCenter));
            }
        }

        string buttonText = selected.IsCompleted ? "다시 꿈에 들어가기" : "의뢰 시작하기";
        if (GUI.Button(new Rect(r.x + 30, r.yMax - 100, r.width - 60, 80), buttonText, PrimaryButton()))
            SceneFlow.StartQuest(selected);
    }

    void DrawTagChips(Rect row)
    {
        GUIStyle chip = Style("chip", 14, InkMuted, TextAnchor.MiddleCenter);
        var widths = new List<float>();
        float total = 0f;
        foreach (string tag in selected.tags)
        {
            float cw = chip.CalcSize(new GUIContent("#" + tag)).x + 28f;
            widths.Add(cw);
            total += cw + 8f;
        }
        float x = row.center.x - (total - 8f) / 2f;
        for (int i = 0; i < selected.tags.Length; i++)
        {
            var rect = new Rect(x, row.y, widths[i], row.height);
            Frame(rect, "chip_tag");
            GUI.Label(rect, "#" + selected.tags[i], chip);
            x += widths[i] + 8f;
        }
    }

    void DrawDohaBubble(Rect r)
    {
        if (GUI.Button(r, GUIContent.none, FrameStyle("panel_indigo"))) dohaLine = (dohaLine + 1) % Mathf.Max(1, dohaLines.Length);
        GUI.Label(new Rect(r.x + 28, r.y + 14, r.width - 80, 36), "야옹…", Style("label", 22, Gold, TextAnchor.MiddleLeft));
        if (dohaLines.Length > 0)
            GUI.Label(new Rect(r.x + 28, r.y + 52, r.width - 80, 44), dohaLines[dohaLine], Style("label", 24, Light, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(r.xMax - 52, r.y + 34, 40, 44), "›", Style("label", 40, Gold, TextAnchor.MiddleCenter));
    }

    void DrawNav(float w, float h)
    {
        const float bw = 168f, bh = 104f, gap = 10f;
        float x = (w - TabNames.Length * bw - (TabNames.Length - 1) * gap) / 2f;
        float y = h - bh - 6f;
        for (int i = 0; i < TabNames.Length; i++)
        {
            var r = new Rect(x + i * (bw + gap), y, bw, bh);
            bool active = i == (questsOpen ? 1 : 0);
            bool locked = i == 2 || i == 3 || i == 5;
            Color old = GUI.color;
            if (locked) GUI.color = new Color(1f, 1f, 1f, 0.55f);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) OnTab(i);
            Icon(r, active ? "nav_button_on" : "nav_button");
            var emblem = new Rect(r.center.x - 25, r.y + 6, 50, 50);
            Icon(emblem, "nav_emblem");
            Icon(new Rect(emblem.x + 11, emblem.y + 11, 28, 28), TabIcons[i]);
            GUI.Label(new Rect(r.x, r.y + 64, r.width, 34), TabNames[i], Style("label", 20, active ? Gold : Light, TextAnchor.MiddleCenter));
            GUI.color = old;
        }
    }

    void DrawToast(float w, float h)
    {
        if (string.IsNullOrEmpty(toast) || Time.unscaledTime > toastUntil) return;
        UIKit.ShadowLabel(new Rect(0, h - 170, w, 50), toast, Style("label", 28, Gold, TextAnchor.MiddleCenter, true));
    }

    void DrawSettings(float w, float h)
    {
        UIKit.DimScreen();
        UIKit.ShadowLabel(new Rect(0f, 70f, w, 80f), "설정", UIKit.Heading);
        settings.Draw(new Rect((w - SettingsPanel.Width) / 2f, 190f, SettingsPanel.Width, SettingsPanel.Height));
        if (GUI.Button(new Rect(w / 2f - 330f, h - 150f, 300f, 80f), "닫기", PrimaryButton())) SetSettings(false);
        if (GUI.Button(new Rect(w / 2f + 30f, h - 150f, 300f, 80f), "로비로", PrimaryButton())) SceneFlow.GoToLobby();
    }

    // ───────── 동작 ─────────

    void OnTab(int index)
    {
        switch (index)
        {
            case 0: questsOpen = false; break;
            case 1: questsOpen = !questsOpen; break;
            case 4: ShowToast("기술(마스터리)은 준비 중이에요"); break;
            default: ShowToast("아직 열리지 않았어요"); break;
        }
    }

    bool Matches(QuestData quest)
    {
        switch (filter)
        {
            case Filter.InProgress: return quest.IsInProgress;
            case Filter.Completed: return quest.IsCompleted;
            default: return true;
        }
    }

    void SetSettings(bool open)
    {
        if (settingsOpen && !open) settings.Close();
        settingsOpen = open;
    }

    void ShowToast(string text)
    {
        toast = text;
        toastUntil = Time.unscaledTime + 1.8f;
    }

    static string Tags(QuestData quest)
    {
        var parts = new List<string>();
        foreach (string tag in quest.tags) if (!string.IsNullOrEmpty(tag)) parts.Add("#" + tag);
        return string.Join("  ", parts);
    }

    // ───────── 그리기 도구 ─────────

    /// <summary>9-slice 틀을 rect 크기로 늘려 그린다(모서리는 늘리지 않는다).</summary>
    void Frame(Rect r, string name)
    {
        if (Event.current.type != EventType.Repaint) return;
        FrameStyle(name).Draw(r, false, false, false, false);
    }

    void Icon(Rect r, string name)
    {
        Texture2D t;
        if (tex.TryGetValue(name, out t)) GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
    }

    GUIStyle FrameStyle(string normal, string hover = null)
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

    GUIStyle TabStyle(bool on)
    {
        string key = on ? "tab:on" : "tab:off";
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(FrameStyle(on ? "tab_on" : "tab_off")) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = style.hover.textColor = on ? Light : Muted;
        if (!on) style.hover.background = FrameStyle("tab_on").normal.background;
        styles[key] = style;
        return style;
    }

    GUIStyle PrimaryButton()
    {
        GUIStyle style;
        if (styles.TryGetValue("primary", out style)) return style;
        style = new GUIStyle(FrameStyle("button_primary", "button_primary_hover")) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = style.hover.textColor = Light;
        styles["primary"] = style;
        return style;
    }

    GUIStyle Style(string kind, int size, Color color, TextAnchor anchor, bool bold = false, bool wrap = false)
    {
        string key = $"{kind}:{size}:{color}:{anchor}:{bold}:{wrap}";
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, wordWrap = wrap, richText = true, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        // 글자는 클릭 대상이 아니므로 마우스를 올려도 색이 바뀌지 않게 모든 상태를 같은 색으로.
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
        styles[key] = style;
        return style;
    }

    void EnsureStyles()
    {
        if (logo != null) return;
        logo = new GUIStyle(UIKit.Title) { alignment = TextAnchor.MiddleLeft, fontSize = 84 };
        logo.normal.textColor = new Color(1f, 0.93f, 0.8f);
        logoSub = new GUIStyle(UIKit.Small) { alignment = TextAnchor.MiddleLeft, fontSize = 24 };
    }

    static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}
