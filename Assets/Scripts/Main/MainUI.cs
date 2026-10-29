using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인(수선소) 화면 UI. 디자인은 "빈티지 꿈 수선 공방"(양피지·리넨·바느질·황동) 시안
/// (ArtSource/Main_UI/reference/시안3.png 배치 + 시안4 타이틀 + 시안1 하단 탭)을 따른다.
/// 조각: Assets/Art/Main/UI/ws_*.png (Artlist 생성 시트 → Tools/Art/build_workshop_ui.py). 9-slice 폭은 ui_borders.json(bx 가로·by 세로).
/// IMGUI, 1080p 기준 좌표. 오른쪽 요소는 화면 오른쪽 끝 기준으로 놓아 화면비가 달라도 붙어 있다.
/// - 왼쪽 위: 로고, 해명도 게이지 / 오른쪽 위: 재화 바(결정·동전·열쇠), 설정
/// - 오른쪽: 의뢰함(전체/진행 중/완료) / 가운데: 의뢰 상세(편지) → "의뢰 시작하기"
/// - 왼쪽 아래: 도하 말풍선 / 아래: 탭(수선소·의뢰함·도감·가방·기술·지도). 도감은 CodexPanel(펼친 책), 기술은 MasteryPanel(수틀 거미줄).
/// </summary>
public class MainUI : MonoBehaviour
{
    enum Filter { All, InProgress, Completed }

    [Serializable]
    class BorderItem { public string name; public int border; public int bx; public int by; }

    [Serializable]
    class BorderList { public BorderItem[] items = new BorderItem[0]; }

    static readonly string[] TabNames = { "수선소", "의뢰함", "도감", "가방", "기술", "지도" };
    static readonly string[] TabIcons = { "ws_icon_workshop", "ws_icon_envelope", "ws_icon_codex", "ws_icon_bag", "ws_icon_mastery", "ws_icon_map" };
    static readonly string[] RewardIcons = { "ws_reward_moon", "ws_reward_thread", "ws_reward_note" };

    // 밝은 글자(어두운 나무·리넨 위), 잉크(양피지 위), 갈색 강조(선택된 탭·도하 말머리)
    static readonly Color Light = Hex("#F3EBDD"), Muted = Hex("#B9AC9A"), Ink = Hex("#2E2219"), InkMuted = Hex("#6B5744"),
        Gold = Hex("#E0BE7C"), Brown = Hex("#8A5520");

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
    /// <summary>도감(수선 기록부). 도감 탭으로 열고 닫는다.</summary>
    readonly CodexPanel codex = new CodexPanel();
    /// <summary>기술(꿈 수틀). 기술 탭으로 열고 닫는다.</summary>
    readonly MasteryPanel mastery = new MasteryPanel();
    /// <summary>화면을 덮는 창: -1 없음, 2 도감, 4 기술(탭 번호와 같음).</summary>
    int overlay = -1;
    bool OverlayOpen => overlay >= 0;
    string toast;
    float toastUntil;

    readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, RectOffset> borders = new Dictionary<string, RectOffset>();
    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
    GUIStyle logo, logoSub;

    void Start()
    {
        Time.timeScale = 1f;
        foreach (Sprite sprite in uiSprites)
            if (sprite != null) tex[sprite.name] = sprite.texture;
        if (uiBorders != null)
            foreach (BorderItem item in JsonUtility.FromJson<BorderList>(uiBorders.text).items)
            {
                int bx = item.bx > 0 ? item.bx : item.border, by = item.by > 0 ? item.by : item.border;
                borders[item.name] = new RectOffset(bx, bx, by, by);
            }

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

        GUI.enabled = !settingsOpen && !OverlayOpen;
        DrawLogo();
        DrawGauge(new Rect(48, 196, 420, 128));
        DrawResources(w);
        if (questsOpen && !OverlayOpen)
        {
            DrawQuestList(new Rect(w - 470, 112, 440, 800));
            if (selected != null) DrawDetail(new Rect(w - 960, 112, 470, 800));
        }
        DrawDohaBubble(new Rect(48, h - 262, 560, 112));
        GUI.enabled = !settingsOpen;
        if (overlay == 2 && !codex.Draw(w, h)) overlay = -1;
        if (overlay == 4 && !mastery.Draw(w, h)) overlay = -1;
        DrawNav(w, h);
        DrawToast(w, h);
        GUI.enabled = true;

        if (settingsOpen) DrawSettings(w, h);
        UIKit.End();

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            if (OverlayOpen && !settingsOpen) overlay = -1;
            else SetSettings(!settingsOpen);
            e.Use();
        }
    }

    // ───────── 영역 ─────────

    void DrawLogo()
    {
        // 로비 아트에서 잘라낸 붓글씨 로고(Assets/Art/Lobby/logo.png). 없으면 글자로 대신한다.
        if (tex.ContainsKey("ws_logo")) Icon(new Rect(30, 18, 470, 150), "ws_logo");
        else if (tex.ContainsKey("logo")) Icon(new Rect(36, 14, 470, 150), "logo");
        else UIKit.ShadowLabel(new Rect(48, 22, 700, 100), "악몽수선소", logo);
        GUI.Label(new Rect(60, 150, 700, 36), subtitle, logoSub);
    }

    void DrawGauge(Rect r)
    {
        // 해명도: 의뢰의 진실을 밝혀 얻는 점수(기획서 26번). 진행도 연결은 알파 단계.
        float percent = 0f;
        Frame(r, "ws_card_info");
        Icon(new Rect(r.x + 20, r.y + 24, 80, 80), "ws_moon_emblem");
        GUI.Label(new Rect(r.x + 116, r.y + 14, 200, 40), "해명도", Style("label", 28, Ink, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(r.xMax - 120, r.y + 14, 96, 40), $"{percent * 100f:0}%", Style("label", 24, Ink, TextAnchor.MiddleRight));
        var track = new Rect(r.x + 116, r.y + 58, r.width - 140, 26);
        Frame(track, "ws_gauge_track");
        if (percent > 0f) Frame(new Rect(track.x + 6, track.y + 6, (track.width - 12) * percent, track.height - 12), "ws_gauge_fill");
        GUI.Label(new Rect(r.x + 116, r.y + 88, r.width - 130, 30), "의뢰의 진실을 밝혀 획득", Style("label", 18, InkMuted, TextAnchor.MiddleLeft));
    }

    void DrawResources(float w)
    {
        var bar = new Rect(w - 660, 24, 470, 72);
        Frame(bar, "ws_bar");
        // 재화 종류·이름은 기획 확인 중. 저장(Wallet) 연결은 4주차.
        DrawResource(new Rect(bar.x + 34, bar.y + 14, 34, 44), "ws_icon_crystal", PlayerPrefs.GetInt("Wallet.Crystal", 0));
        DrawResource(new Rect(bar.x + 180, bar.y + 16, 40, 40), "ws_icon_coin", PlayerPrefs.GetInt("Wallet.Coin", 0));
        DrawResource(new Rect(bar.x + 326, bar.y + 14, 40, 44), "ws_icon_key", PlayerPrefs.GetInt("Wallet.Key", 0));

        // 바느질 라벨(꼬리표) 모양 작은 버튼
        if (UIKit.ClickButton(new Rect(w - 176, 24, 70, 72), "설정", TagStyle())) SetSettings(true);
        if (UIKit.ClickButton(new Rect(w - 98, 24, 70, 72), "전체", TagStyle())) GameSettings.Fullscreen = !GameSettings.Fullscreen;
    }

    void DrawResource(Rect icon, string iconName, int value)
    {
        Icon(icon, iconName);
        GUI.Label(new Rect(icon.xMax + 12, icon.center.y - 20, 100, 40), value.ToString("N0"), Style("label", 24, Light, TextAnchor.MiddleLeft));
    }

    void DrawQuestList(Rect r)
    {
        // 호두나무 틀 + 리넨. 제목은 위쪽 어두운 나무 띠 위에 밝은 글자로.
        Frame(r, "ws_panel_list");
        Icon(new Rect(r.x + 30, r.y + 12, 42, 32), "ws_envelope_small");
        GUI.Label(new Rect(r.x + 82, r.y + 4, 200, 48), "의뢰함", Style("label", 28, Light, TextAnchor.MiddleLeft, true));
        QuestData[] quests = QuestDatabase.Instance.quests;
        int open = 0;
        foreach (QuestData q in quests) if (q != null && q.stageIndex >= 0) open++;
        GUI.Label(new Rect(r.xMax - 140, r.y + 4, 112, 48), $"{open} / {QuestDatabase.Instance.capacity}", Style("label", 20, Gold, TextAnchor.MiddleRight));

        string[] names = { "전체", "진행 중", "완료" };
        for (int i = 0; i < names.Length; i++)
            if (UIKit.ClickButton(new Rect(r.x + 30 + i * 128, r.y + 76, 120, 48), names[i], TabStyle((int)filter == i))) filter = (Filter)i;

        float y = r.y + 142;
        int shown = 0;
        foreach (QuestData quest in quests)
        {
            if (quest == null || !Matches(quest)) continue;
            float cardHeight = quest == selected ? 112 : 94;
            if (y + cardHeight > r.yMax - 30) break;
            DrawQuestCard(new Rect(r.x + 26, y, r.width - 52, cardHeight), quest);
            y += cardHeight + 12;
            shown++;
        }
        if (shown == 0)
            GUI.Label(new Rect(r.x + 34, y + 10, r.width - 68, 50), "해당하는 의뢰가 없어요.", Style("label", 20, InkMuted, TextAnchor.MiddleLeft));
    }

    void DrawQuestCard(Rect r, QuestData quest)
    {
        bool locked = !quest.IsAvailable && !quest.IsCompleted;
        bool isSelected = quest == selected;
        Color old = GUI.color;
        if (locked) GUI.color = new Color(1f, 1f, 1f, 0.45f);

        if (UIKit.ClickButton(r, GUIContent.none, isSelected ? FrameStyle("ws_card_on") : FrameStyle("ws_card", "ws_card_on")))
        {
            if (locked) ShowToast(quest.stageIndex < 0 ? "아직 도착하지 않은 의뢰예요" : "앞의 의뢰를 먼저 해결해 주세요");
            else selected = quest;
        }

        float textX = r.x + 26;
        if (isSelected)
        {
            var thumb = new Rect(r.x + 18, r.y + 18, r.height - 36, r.height - 36);
            if (quest.thumbnail != null) GUI.DrawTexture(thumb, quest.thumbnail.texture, ScaleMode.ScaleAndCrop);
            else Frame(thumb, "ws_photo");
            textX = thumb.xMax + 18;
        }
        Color titleColor = Ink, tagColor = InkMuted;
        GUI.Label(new Rect(textX, r.y + r.height * 0.16f, r.xMax - textX - 60, 38), quest.title, Style("label", isSelected ? 24 : 21, titleColor, TextAnchor.MiddleLeft, isSelected));
        GUI.Label(new Rect(textX, r.y + r.height * 0.56f, r.xMax - textX - 60, 28), Tags(quest), Style("label", 16, tagColor, TextAnchor.MiddleLeft));

        if (!locked)
        {
            string state = quest.IsCompleted ? "완료" : quest.IsInProgress ? "진행 중" : "";
            if (state.Length > 0)
                GUI.Label(new Rect(r.xMax - 150, r.y + 8, 96, 26), state, Style("label", 15, tagColor, TextAnchor.MiddleRight));
            Icon(new Rect(r.xMax - 54, r.center.y - 16, 32, 32), isSelected ? "ws_diamond_on" : "ws_diamond_off");
        }
        GUI.color = old;
    }

    void DrawDetail(Rect r)
    {
        // 밀랍 봉인이 찍힌 낡은 편지지
        Frame(r, "ws_panel_letter");

        string status = selected.IsCompleted ? "완료" : selected.IsInProgress ? "진행 중" : "진행 가능";
        var badge = new Rect(r.center.x - 70, r.y + 34, 140, 36);
        Frame(badge, "ws_badge");
        GUI.Label(badge, status, Style("label", 16, Light, TextAnchor.MiddleCenter));

        GUI.Label(new Rect(r.x + 24, r.y + 80, r.width - 48, 50), selected.title, Style("label", 32, Ink, TextAnchor.MiddleCenter, true));
        DrawTagChips(new Rect(r.x + 24, r.y + 136, r.width - 48, 28));

        var photo = new Rect(r.x + 40, r.y + 182, r.width - 80, 200);
        Frame(photo, "ws_photo");
        if (selected.photo != null) GUI.DrawTexture(new Rect(photo.x + 18, photo.y + 16, photo.width - 36, photo.height - 32), selected.photo.texture, ScaleMode.ScaleAndCrop);

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
                Frame(slot, "ws_slot");
                Icon(new Rect(slot.x + 20, slot.y + 20, 42, 42), RewardIcons[i % RewardIcons.Length]);
                GUI.Label(new Rect(slot.x - 8, slot.yMax + 2, slot.width + 16, 22), selected.rewards[i], Style("label", 13, InkMuted, TextAnchor.MiddleCenter));
            }
        }

        string buttonText = selected.IsCompleted ? "다시 꿈에 들어가기" : "의뢰 시작하기";
        if (UIKit.ClickButton(new Rect(r.x + 20, r.yMax - 104, r.width - 40, 84), buttonText, PrimaryButton()))
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
            Frame(rect, "ws_chip");
            GUI.Label(rect, "#" + selected.tags[i], chip);
            x += widths[i] + 8f;
        }
    }

    void DrawDohaBubble(Rect r)
    {
        // 핀으로 꽂은 찢어진 양피지 쪽지
        if (UIKit.ClickButton(r, GUIContent.none, FrameStyle("ws_bubble"))) dohaLine = (dohaLine + 1) % Mathf.Max(1, dohaLines.Length);
        GUI.Label(new Rect(r.x + 34, r.y + 16, r.width - 90, 34), "야옹…", Style("label", 22, Brown, TextAnchor.MiddleLeft, true));
        if (dohaLines.Length > 0)
            GUI.Label(new Rect(r.x + 34, r.y + 52, r.width - 90, 44), dohaLines[dohaLine], Style("label", 24, Ink, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(r.xMax - 60, r.y + 34, 40, 44), "›", Style("label", 40, Brown, TextAnchor.MiddleCenter));
    }

    void DrawNav(float w, float h)
    {
        const float bw = 168f, bh = 104f, gap = 10f;
        float x = (w - TabNames.Length * bw - (TabNames.Length - 1) * gap) / 2f;
        float y = h - bh - 6f;
        for (int i = 0; i < TabNames.Length; i++)
        {
            var r = new Rect(x + i * (bw + gap), y, bw, bh);
            bool active = i == (OverlayOpen ? overlay : questsOpen ? 1 : 0);
            bool locked = i == 3 || i == 5;
            Color old = GUI.color;
            if (locked) GUI.color = new Color(1f, 1f, 1f, 0.55f);
            if (UIKit.ClickButton(r, GUIContent.none, GUIStyle.none)) OnTab(i);
            // 천 이름표(바느질 라벨) 모양 탭. 선택된 탭은 금실 테두리.
            var tag = new Rect(r.center.x - 56, r.y - 6, 112, 112);
            Icon(tag, active ? "ws_nav_on" : "ws_nav");
            Icon(new Rect(tag.center.x - 24, tag.y + 18, 48, 44), TabIcons[i]);
            GUI.Label(new Rect(r.x, tag.y + 62, r.width, 34), TabNames[i], Style("label", 20, active ? Brown : Ink, TextAnchor.MiddleCenter, active));
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
        if (UIKit.ClickButton(new Rect(w / 2f - 330f, h - 150f, 300f, 80f), "닫기", PrimaryButton())) SetSettings(false);
        if (UIKit.ClickButton(new Rect(w / 2f + 30f, h - 150f, 300f, 80f), "로비로", PrimaryButton())) SceneFlow.GoToLobby();
    }

    // ───────── 동작 ─────────

    void OnTab(int index)
    {
        switch (index)
        {
            case 0: questsOpen = false; overlay = -1; break;
            case 1: questsOpen = OverlayOpen || !questsOpen; overlay = -1; break;
            case 2:
            case 4: overlay = overlay == index ? -1 : index; break;
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
        RectOffset b;
        if (borders.TryGetValue(normal, out b)) style.border = new RectOffset(b.left, b.right, b.top, b.bottom);
        styles[key] = style;
        return style;
    }

    GUIStyle TabStyle(bool on)
    {
        string key = on ? "tab:on" : "tab:off";
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        // 꺼진 탭: 밝은 리넨(잉크 글자), 켜진 탭: 가죽(밝은 글자)
        style = new GUIStyle(FrameStyle(on ? "ws_tab_on" : "ws_tab_off")) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = style.hover.textColor = style.active.textColor = on ? Light : Ink;
        styles[key] = style;
        return style;
    }

    GUIStyle PrimaryButton()
    {
        GUIStyle style;
        if (styles.TryGetValue("primary", out style)) return style;
        // 데님 리본 버튼. 글자는 리본 가운데(양끝 꼬리 제외)에 오도록 위아래로 살짝 당긴다.
        style = new GUIStyle(FrameStyle("ws_button", "ws_button_hover")) { fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        style.padding = new RectOffset(60, 60, 0, 10);
        style.normal.textColor = style.hover.textColor = style.active.textColor = Light;
        styles["primary"] = style;
        return style;
    }

    GUIStyle TagStyle()
    {
        GUIStyle style;
        if (styles.TryGetValue("tag", out style)) return style;
        style = new GUIStyle(FrameStyle("ws_tag")) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        style.padding = new RectOffset(0, 0, 14, 0); // 위쪽 쇠고리 아래로
        style.normal.textColor = style.hover.textColor = style.active.textColor = Ink;
        styles["tag"] = style;
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
