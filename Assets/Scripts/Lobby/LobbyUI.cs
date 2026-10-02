using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 수선소(로비) 화면 UI. 기획 시안(ArtSource/Lobby/reference/lobby_mockup.jpg) 배치를 따른다.
/// UI 아트가 오기 전까지 임시 IMGUI로 그린다(1080p 기준 좌표, 오른쪽 요소는 화면 오른쪽 끝 기준).
/// - 왼쪽 위 로고, 오른쪽 위 재화·설정
/// - 오른쪽 의뢰함(전체/진행 중/완료) → 가운데 편지 카드 → "의뢰 시작하기"
/// - 왼쪽 아래 도하 말풍선(클릭하면 다음 말)
/// - 아래 탭 바: 수선소·의뢰함·도감·가방·기술·지도
/// </summary>
public class LobbyUI : MonoBehaviour
{
    enum Filter { All, InProgress, Completed }

    static readonly string[] Tabs = { "수선소", "의뢰함", "도감", "가방", "기술", "지도" };

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
    readonly SettingsPanel settings = new SettingsPanel();
    string toast;
    float toastUntil;

    GUIStyle logo, logoSub, paperTitle, paperTag, paperBody, paperSmall, cardTitle, cardTag, chip, bubble, tabStyle, tabSelected, header;
    Texture2D paperTexture;

    void Start()
    {
        Time.timeScale = 1f;
        foreach (QuestData quest in QuestDatabase.Instance.quests)
        {
            if (quest != null && quest.IsAvailable) { selected = quest; break; }
        }
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.Repaint || e.type == EventType.MouseMove)
            LobbyBackground.Pointer = new Vector2(e.mousePosition.x / Mathf.Max(1, Screen.width), e.mousePosition.y / Mathf.Max(1, Screen.height));

        UIKit.Begin();
        EnsureStyles();
        float w = UIKit.Width, h = UIKit.Height;

        GUI.enabled = !settingsOpen;
        DrawLogo();
        DrawTopRight(w);
        DrawQuestBoard(w);
        if (selected != null) DrawLetter(w);
        DrawDohaBubble(h);
        DrawTabBar(w, h);
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

    // ───────── 영역별 그리기 ─────────

    void DrawLogo()
    {
        UIKit.ShadowLabel(new Rect(48, 22, 700, 110), "악몽수선소", logo);
        GUI.Label(new Rect(62, 124, 700, 36), subtitle, logoSub);
    }

    void DrawTopRight(float w)
    {
        var bar = new Rect(w - 560, 22, 300, 60);
        GUI.Box(bar, GUIContent.none, UIKit.Panel);
        // 재화 종류는 기획 확정 전(몽결정 가정). 4주차 Wallet 연결 전까지는 0.
        GUI.Label(new Rect(bar.x + 20, bar.y + 6, 270, 48), $"◆ 몽결정  {PlayerPrefs.GetInt("Wallet.DreamCrystal", 0):N0}", header);

        if (GUI.Button(new Rect(w - 240, 22, 100, 60), "설정", UIKit.ButtonSmall)) SetSettings(true);
        if (GUI.Button(new Rect(w - 128, 22, 100, 60), "전체", UIKit.ButtonSmall)) GameSettings.Fullscreen = !GameSettings.Fullscreen;
    }

    void DrawQuestBoard(float w)
    {
        var panel = new Rect(w - 420, 104, 392, 812);
        GUI.Box(panel, GUIContent.none, UIKit.Panel);

        QuestData[] quests = QuestDatabase.Instance.quests;
        int open = 0;
        foreach (QuestData q in quests) if (q != null && q.stageIndex >= 0) open++;
        GUI.Label(new Rect(panel.x + 24, panel.y + 16, 240, 56), "의뢰함", new GUIStyle(header) { fontSize = 40 });
        GUI.Label(new Rect(panel.xMax - 140, panel.y + 26, 116, 40), $"{open} / {QuestDatabase.Instance.capacity}", new GUIStyle(header) { alignment = TextAnchor.MiddleRight, fontSize = 26 });

        string[] names = { "전체", "진행 중", "완료" };
        for (int i = 0; i < names.Length; i++)
        {
            var rect = new Rect(panel.x + 20 + i * 120, panel.y + 84, 112, 50);
            if (GUI.Button(rect, names[i], (int)filter == i ? UIKit.ButtonSelected : UIKit.ButtonSmall)) filter = (Filter)i;
        }

        float y = panel.y + 150;
        foreach (QuestData quest in quests)
        {
            if (quest == null || !Matches(quest)) continue;
            if (y + 112 > panel.yMax - 12) break;
            DrawQuestCard(new Rect(panel.x + 16, y, panel.width - 32, 112), quest);
            y += 124;
        }
        if (y == panel.y + 150)
            GUI.Label(new Rect(panel.x + 24, y + 20, panel.width - 48, 60), "해당하는 의뢰가 없어요.", cardTag);
    }

    void DrawQuestCard(Rect rect, QuestData quest)
    {
        bool locked = !quest.IsAvailable && !quest.IsCompleted;
        bool isSelected = quest == selected;

        Color old = GUI.color;
        if (locked) GUI.color = new Color(1f, 1f, 1f, 0.45f);
        if (GUI.Button(rect, GUIContent.none, isSelected ? UIKit.ButtonSelected : UIKit.ButtonSmall))
        {
            if (locked) ShowToast(quest.stageIndex < 0 ? "아직 도착하지 않은 의뢰예요" : "앞의 의뢰를 먼저 해결해 주세요");
            else selected = quest;
        }
        float textX = rect.x + 18;
        if (quest.thumbnail != null)
        {
            GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 10, 92, 92), quest.thumbnail.texture, ScaleMode.ScaleToFit);
            textX = rect.x + 114;
        }
        GUI.Label(new Rect(textX, rect.y + 14, rect.xMax - textX - 12, 44), quest.title, cardTitle);
        GUI.Label(new Rect(textX, rect.y + 60, rect.xMax - textX - 12, 36), Tags(quest), cardTag);
        string state = locked ? "잠김" : quest.IsCompleted ? "완료" : quest.IsInProgress ? "진행 중" : "";
        if (state.Length > 0)
            GUI.Label(new Rect(rect.xMax - 110, rect.y + 14, 96, 30), state, new GUIStyle(cardTag) { alignment = TextAnchor.MiddleRight });
        GUI.color = old;
    }

    void DrawLetter(float w)
    {
        var card = new Rect(w - 880, 196, 440, 720);
        GUI.DrawTexture(card, paperTexture);
        float x = card.x + 36, inner = card.width - 72, y = card.y + 30;

        GUI.Label(new Rect(x, y, inner, 50), selected.title, paperTitle);
        y += 52;
        GUI.Label(new Rect(x, y, inner, 32), Tags(selected), paperTag);
        y += 44;

        if (selected.photo != null)
        {
            GUI.DrawTexture(new Rect(x, y, inner, 200), selected.photo.texture, ScaleMode.ScaleAndCrop);
            y += 216;
        }

        float letterHeight = paperBody.CalcHeight(new GUIContent(selected.letter), inner);
        GUI.Label(new Rect(x, y, inner, letterHeight), selected.letter, paperBody);
        y += letterHeight + 24;

        if (selected.rewards.Length > 0)
        {
            GUI.Label(new Rect(x, y, inner, 32), "◇ 의뢰 보상", paperSmall);
            y += 38;
            float cx = x;
            foreach (string reward in selected.rewards)
            {
                float cw = chip.CalcSize(new GUIContent(reward)).x + 28;
                if (cx + cw > x + inner) { cx = x; y += 46; }
                GUI.Label(new Rect(cx, y, cw, 38), reward, chip);
                cx += cw + 10;
            }
        }

        string buttonText = selected.IsCompleted ? "다시 꿈에 들어가기" : "의뢰 시작하기";
        if (UIKit.DrawButton(new Rect(card.x + 30, card.yMax - 100, card.width - 60, 76), buttonText))
            SceneFlow.StartQuest(selected);
    }

    void DrawDohaBubble(float h)
    {
        var rect = new Rect(48, h - 240, 600, 110);
        if (GUI.Button(rect, GUIContent.none, UIKit.ButtonSmall)) dohaLine = (dohaLine + 1) % Mathf.Max(1, dohaLines.Length);
        GUI.Label(new Rect(rect.x + 28, rect.y + 12, rect.width - 80, 36), "야옹…", bubble);
        if (dohaLines.Length > 0)
            GUI.Label(new Rect(rect.x + 28, rect.y + 52, rect.width - 80, 44), dohaLines[dohaLine], bubble);
        GUI.Label(new Rect(rect.xMax - 52, rect.y + 34, 40, 40), "›", new GUIStyle(bubble) { fontSize = 40 });
    }

    void DrawTabBar(float w, float h)
    {
        var bar = new Rect(0, h - 104, w, 104);
        GUI.Box(bar, GUIContent.none, UIKit.Panel);
        const float tabW = 200f;
        float x = (w - Tabs.Length * tabW) / 2f;
        for (int i = 0; i < Tabs.Length; i++)
        {
            var rect = new Rect(x + i * tabW + 8, bar.y + 14, tabW - 16, 76);
            if (GUI.Button(rect, Tabs[i], i == 0 ? tabSelected : tabStyle)) OnTab(i);
        }
    }

    void DrawToast(float w, float h)
    {
        if (string.IsNullOrEmpty(toast) || Time.unscaledTime > toastUntil) return;
        UIKit.ShadowLabel(new Rect(0, h - 170, w, 50), toast, new GUIStyle(header) { alignment = TextAnchor.MiddleCenter, fontSize = 30 });
    }

    void DrawSettings(float w, float h)
    {
        UIKit.DimScreen();
        UIKit.ShadowLabel(new Rect(0f, 70f, w, 80f), "설정", UIKit.Heading);
        settings.Draw(new Rect((w - SettingsPanel.Width) / 2f, 190f, SettingsPanel.Width, SettingsPanel.Height));
        if (UIKit.DrawButton(new Rect(w / 2f - 330f, h - 150f, 300f, 76f), "닫기")) SetSettings(false);
        if (UIKit.DrawButton(new Rect(w / 2f + 30f, h - 150f, 300f, 76f), "타이틀로")) SceneFlow.GoToTitle();
    }

    // ───────── 동작 ─────────

    void OnTab(int index)
    {
        switch (index)
        {
            case 0: break;
            case 1: filter = Filter.All; ShowToast("오른쪽 의뢰함에서 편지를 골라 주세요"); break;
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

    void EnsureStyles()
    {
        if (logo != null && paperTexture != null) return;
        var ink = new Color(0.24f, 0.17f, 0.12f);
        var gold = new Color(1f, 0.86f, 0.6f);

        logo = new GUIStyle(UIKit.Title) { alignment = TextAnchor.MiddleLeft, fontSize = 88 };
        logo.normal.textColor = new Color(1f, 0.93f, 0.8f);
        logoSub = new GUIStyle(UIKit.Small) { alignment = TextAnchor.MiddleLeft, fontSize = 24 };
        header = new GUIStyle(UIKit.Body) { fontSize = 28, fontStyle = FontStyle.Bold };
        header.normal.textColor = gold;

        paperTexture = UIKit.MakeTexture(new Color(0.91f, 0.85f, 0.72f, 0.97f));
        paperTitle = new GUIStyle(UIKit.Body) { fontSize = 36, fontStyle = FontStyle.Bold, wordWrap = true };
        paperTitle.normal.textColor = ink;
        paperTag = new GUIStyle(UIKit.Body) { fontSize = 22 };
        paperTag.normal.textColor = new Color(0.45f, 0.33f, 0.24f);
        paperBody = new GUIStyle(UIKit.Body) { fontSize = 24, wordWrap = true, alignment = TextAnchor.UpperLeft };
        paperBody.normal.textColor = ink;
        paperSmall = new GUIStyle(paperTag) { fontStyle = FontStyle.Bold };
        chip = new GUIStyle(UIKit.Small) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        chip.normal.background = UIKit.MakeTexture(new Color(0.22f, 0.18f, 0.32f, 0.92f));
        chip.normal.textColor = gold;

        cardTitle = new GUIStyle(UIKit.Body) { fontSize = 28, fontStyle = FontStyle.Bold };
        cardTitle.normal.textColor = new Color(1f, 0.95f, 0.88f);
        cardTag = new GUIStyle(UIKit.Small) { alignment = TextAnchor.MiddleLeft, fontSize = 20 };
        bubble = new GUIStyle(UIKit.Body) { fontSize = 26 };

        tabStyle = new GUIStyle(UIKit.ButtonSmall) { fontSize = 26 };
        tabSelected = new GUIStyle(UIKit.ButtonSelected) { fontSize = 26 };
    }
}
