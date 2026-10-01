using UnityEngine;

/// <summary>타이틀 화면: 시작하기 / 스테이지 선택 / 설정 / 종료.</summary>
public class TitleMenu : MonoBehaviour
{
    enum Page { Main, StageSelect, Settings }

    public string gameTitle = "디펜스 게임";
    public string subtitle = "마지막 왕국을 지켜라";

    Page page = Page.Main;
    bool confirmReset;
    GUIStyle stageCardStyle;

    void Awake()
    {
        Time.timeScale = 1f;
        if (SceneFlow.OpenStageSelectOnTitle)
        {
            SceneFlow.OpenStageSelectOnTitle = false;
            page = Page.StageSelect;
        }
    }

    void OnGUI()
    {
        UIKit.Begin();
        switch (page)
        {
            case Page.Main: DrawMain(); break;
            case Page.StageSelect: DrawStageSelect(); break;
            case Page.Settings: DrawSettings(); break;
        }
        GUI.Label(new Rect(UIKit.Width - 220f, UIKit.Height - 50f, 200f, 40f), $"v{Application.version}", UIKit.Small);
        UIKit.End();

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && page != Page.Main)
        {
            OpenPage(Page.Main);
            e.Use();
        }
    }

    void DrawMain()
    {
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0f, 180f, w, 130f), gameTitle, UIKit.Title);
        GUI.Label(new Rect(0f, 310f, w, 50f), subtitle, UIKit.Small);

        const float buttonWidth = 440f;
        const float buttonHeight = 84f;
        float x = (w - buttonWidth) * 0.5f;
        float y = 480f;

        if (UIKit.DrawButton(new Rect(x, y, buttonWidth, buttonHeight), "시작하기")) SceneFlow.StartStage(0);
        y += 104f;
        if (UIKit.DrawButton(new Rect(x, y, buttonWidth, buttonHeight), "스테이지 선택")) OpenPage(Page.StageSelect);
        y += 104f;
        if (UIKit.DrawButton(new Rect(x, y, buttonWidth, buttonHeight), "설정")) OpenPage(Page.Settings);
        y += 104f;
        if (UIKit.DrawButton(new Rect(x, y, buttonWidth, buttonHeight), "종료")) SceneFlow.QuitGame();
    }

    void DrawStageSelect()
    {
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0f, 70f, w, 80f), "스테이지 선택", UIKit.Heading);

        StageDatabase db = StageDatabase.Instance;
        const float cardWidth = 320f;
        const float cardHeight = 190f;
        const float gap = 40f;
        const int perRow = 4;

        for (int i = 0; i < db.Count; i++)
        {
            int row = i / perRow;
            int column = i % perRow;
            int inRow = Mathf.Min(perRow, db.Count - row * perRow);
            float rowWidth = inRow * cardWidth + (inRow - 1) * gap;
            var rect = new Rect((w - rowWidth) * 0.5f + column * (cardWidth + gap), 220f + row * (cardHeight + gap), cardWidth, cardHeight);

            StageData stage = db.Get(i);
            if (stage == null) continue;
            bool unlocked = Progress.IsUnlocked(i);
            string status = Progress.IsCleared(i) ? "<color=#7CFC8A>클리어 ✓</color>" : unlocked ? "도전 가능" : "<color=#888888>잠김</color>";
            string text = $"<size=44>{stage.stageId}</size>\n{stage.title}\n<size=22>{status}</size>";

            if (stageCardStyle == null) stageCardStyle = new GUIStyle(UIKit.Button) { richText = true, fontSize = 28 };
            GUI.enabled = unlocked;
            if (GUI.Button(rect, text, stageCardStyle)) SceneFlow.StartStage(i);
            GUI.enabled = true;
        }

        if (UIKit.DrawButton(new Rect(60f, UIKit.Height - 130f, 220f, 70f), "← 뒤로", UIKit.ButtonSmall)) OpenPage(Page.Main);
    }

    void DrawSettings()
    {
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0f, 70f, w, 80f), "설정", UIKit.Heading);

        var panel = new Rect((w - 1000f) * 0.5f, 200f, 1000f, 620f);
        UIKit.DrawPanel(panel);
        float labelX = panel.x + 60f;
        float valueX = panel.x + 380f;
        float y = panel.y + 60f;

        // 볼륨
        GUI.Label(new Rect(labelX, y, 300f, 60f), "마스터 볼륨", UIKit.Body);
        float volume = GUI.HorizontalSlider(new Rect(valueX, y + 24f, 420f, 30f), GameSettings.MasterVolume, 0f, 1f);
        if (!Mathf.Approximately(volume, GameSettings.MasterVolume)) GameSettings.MasterVolume = volume;
        GUI.Label(new Rect(valueX + 440f, y, 120f, 60f), $"{Mathf.RoundToInt(volume * 100f)}%", UIKit.Body);
        y += 120f;

        // 텍스트 속도
        GUI.Label(new Rect(labelX, y, 300f, 60f), "스토리 텍스트 속도", UIKit.Body);
        for (int i = 0; i < GameSettings.TextSpeedNames.Length; i++)
        {
            GUIStyle style = i == GameSettings.TextSpeed ? UIKit.ButtonSelected : UIKit.ButtonSmall;
            if (GUI.Button(new Rect(valueX + i * 135f, y + 4f, 125f, 56f), GameSettings.TextSpeedNames[i], style))
                GameSettings.TextSpeed = i;
        }
        y += 120f;

        // 전체 화면
        GUI.Label(new Rect(labelX, y, 300f, 60f), "전체 화면", UIKit.Body);
        bool fullscreen = GameSettings.Fullscreen;
        if (GUI.Button(new Rect(valueX, y + 4f, 125f, 56f), "켜기", fullscreen ? UIKit.ButtonSelected : UIKit.ButtonSmall)) GameSettings.Fullscreen = true;
        if (GUI.Button(new Rect(valueX + 135f, y + 4f, 125f, 56f), "끄기", fullscreen ? UIKit.ButtonSmall : UIKit.ButtonSelected)) GameSettings.Fullscreen = false;
        y += 120f;

        // 진행 초기화
        GUI.Label(new Rect(labelX, y, 300f, 60f), "진행 상황", UIKit.Body);
        string resetText = confirmReset ? "정말 초기화할까요?" : "초기화";
        if (GUI.Button(new Rect(valueX, y + 4f, 395f, 56f), resetText, confirmReset ? UIKit.ButtonSelected : UIKit.ButtonSmall))
        {
            if (confirmReset) Progress.ResetAll();
            confirmReset = !confirmReset;
        }

        if (UIKit.DrawButton(new Rect(60f, UIKit.Height - 130f, 220f, 70f), "← 뒤로", UIKit.ButtonSmall)) OpenPage(Page.Main);
    }

    void OpenPage(Page next)
    {
        if (page == Page.Settings) GameSettings.Save();
        confirmReset = false;
        page = next;
    }
}
