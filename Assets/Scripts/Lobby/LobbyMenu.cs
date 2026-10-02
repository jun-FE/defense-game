using System;
using UnityEngine;

/// <summary>
/// 로비(첫 화면) 메뉴: 이어하기 / 새로하기 / 설정 / 종료.
/// 메뉴 글자는 로비 아트(06_logo_menu)에서 잘라낸 이미지이고, 위치는 lobby_layout.json의 "menu"에 있다.
/// 마우스를 올리거나 ↑↓ 키로 고르면 ✦ 표시와 금색 밑줄이 붙고, 클릭·Enter로 실행한다.
/// </summary>
public class LobbyMenu : MonoBehaviour
{
    [Serializable]
    class MenuItem
    {
        public string name;
        public float x, y, width, height;
    }

    [Serializable]
    class Layout
    {
        public float canvasWidth = 1920f;
        public float canvasHeight = 1080f;
        public MenuItem[] menu = new MenuItem[0];
    }

    public TextAsset layoutJson;
    [Tooltip("메뉴 이미지 (이름: menu_continue, menu_new, menu_settings, menu_quit)")]
    public Sprite[] menuSprites = new Sprite[0];

    Layout layout;
    Texture2D[] textures;
    int hovered = -1;
    Vector2 lastMouse = new Vector2(-1f, -1f);
    bool confirmNewGame;
    bool settingsOpen;
    readonly SettingsPanel settings = new SettingsPanel();
    GUIStyle star, dialogText;
    Texture2D lineTexture;

    static bool HasSave => Progress.HighestCleared >= 0 || QuestProgress.HasAny;

    void Start()
    {
        Time.timeScale = 1f;
        layout = layoutJson != null ? JsonUtility.FromJson<Layout>(layoutJson.text) : new Layout();
        textures = new Texture2D[layout.menu.Length];
        for (int i = 0; i < layout.menu.Length; i++)
            foreach (Sprite sprite in menuSprites)
                if (sprite != null && sprite.name == layout.menu[i].name) textures[i] = sprite.texture;
        hovered = HasSave ? IndexOf("menu_continue") : IndexOf("menu_new");
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.Repaint || e.type == EventType.MouseMove)
            LayeredBackground.Pointer = new Vector2(e.mousePosition.x / Mathf.Max(1, Screen.width), e.mousePosition.y / Mathf.Max(1, Screen.height));

        UIKit.Begin();
        EnsureStyles();
        Vector2 mouse = e.mousePosition; // UIKit.Begin 이후라 1080p 기준 좌표
        bool overlay = settingsOpen || confirmNewGame;
        // 실행 중 OnGUI에는 MouseMove 이벤트가 오지 않아서, 다시 그릴 때 마우스가 움직였는지 본다.
        bool mouseMoved = e.type == EventType.Repaint && (mouse - lastMouse).sqrMagnitude > 0.5f;
        if (e.type == EventType.Repaint) lastMouse = mouse;

        for (int i = 0; i < layout.menu.Length; i++)
        {
            Rect rect = ItemRect(layout.menu[i]);
            bool enabled = IsEnabled(layout.menu[i].name);
            if (!overlay && enabled && mouseMoved && rect.Contains(mouse)) hovered = i;

            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, !enabled ? 0.3f : i == hovered ? 1f : 0.72f);
            if (textures[i] != null) GUI.DrawTexture(rect, textures[i], ScaleMode.ScaleToFit);
            GUI.color = old;

            if (i == hovered && enabled && !overlay)
            {
                GUI.Label(new Rect(rect.x - 44f, rect.center.y - 22f, 40f, 44f), "✦", star);
                GUI.DrawTexture(new Rect(rect.x - 10f, rect.yMax - 4f, rect.width + 120f, 2f), lineTexture);
            }

            if (!overlay && enabled && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(mouse))
            {
                e.Use();
                Activate(layout.menu[i].name);
            }
        }

        if (confirmNewGame) DrawConfirm();
        if (settingsOpen) DrawSettings();
        UIKit.End();

        if (e.type == EventType.KeyDown) HandleKey(e);
    }

    void HandleKey(Event e)
    {
        if (e.keyCode == KeyCode.Escape)
        {
            if (settingsOpen) CloseSettings();
            confirmNewGame = false;
            e.Use();
            return;
        }
        if (settingsOpen || confirmNewGame) return;
        if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
        {
            int step = e.keyCode == KeyCode.UpArrow ? -1 : 1;
            for (int n = 0; n < layout.menu.Length; n++)
            {
                hovered = (hovered + step + layout.menu.Length) % layout.menu.Length;
                if (IsEnabled(layout.menu[hovered].name)) break;
            }
            e.Use();
        }
        else if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && hovered >= 0)
        {
            Activate(layout.menu[hovered].name);
            e.Use();
        }
    }

    void Activate(string item)
    {
        switch (item)
        {
            case "menu_continue":
                SceneFlow.GoToMain();
                break;
            case "menu_new":
                if (HasSave) confirmNewGame = true;
                else StartNewGame();
                break;
            case "menu_settings":
                settingsOpen = true;
                break;
            case "menu_quit":
                SceneFlow.QuitGame();
                break;
        }
    }

    static void StartNewGame()
    {
        Progress.ResetAll();
        QuestProgress.ResetAll();
        // 프롤로그(이안의 꿈 → 도하 수선 → 첫 편지)는 4주차에 이 자리에서 재생한다.
        SceneFlow.GoToMain();
    }

    bool IsEnabled(string item) => item != "menu_continue" || HasSave;

    void DrawConfirm()
    {
        float w = UIKit.Width, h = UIKit.Height;
        UIKit.DimScreen(0.6f);
        var panel = new Rect((w - 760f) / 2f, h / 2f - 170f, 760f, 300f);
        UIKit.DrawPanel(panel);
        GUI.Label(new Rect(panel.x + 40f, panel.y + 40f, panel.width - 80f, 120f),
            "새로 시작하면 지금까지의 진행 상황이 지워져요.\n그래도 새로 시작할까요?", dialogText);
        if (UIKit.DrawButton(new Rect(panel.x + 60f, panel.yMax - 110f, 300f, 76f), "새로 시작")) { confirmNewGame = false; StartNewGame(); }
        if (UIKit.DrawButton(new Rect(panel.xMax - 360f, panel.yMax - 110f, 300f, 76f), "취소")) confirmNewGame = false;
    }

    void DrawSettings()
    {
        float w = UIKit.Width, h = UIKit.Height;
        UIKit.DimScreen();
        UIKit.ShadowLabel(new Rect(0f, 70f, w, 80f), "설정", UIKit.Heading);
        settings.Draw(new Rect((w - SettingsPanel.Width) / 2f, 190f, SettingsPanel.Width, SettingsPanel.Height));
        if (UIKit.DrawButton(new Rect((w - 300f) / 2f, h - 150f, 300f, 76f), "닫기")) CloseSettings();
    }

    void CloseSettings()
    {
        settingsOpen = false;
        settings.Close();
    }

    /// <summary>1920×1080 배치 좌표 → 화면 좌표. 넓은 화면에서 배경이 확대되면 메뉴도 같이 커지고 옮겨진다.</summary>
    Rect ItemRect(MenuItem item)
    {
        Vector2 center = LayeredBackground.CanvasToScreen(item.x, item.y);
        float z = LayeredBackground.Zoom;
        return new Rect(center.x - item.width * z / 2f, center.y - item.height * z / 2f, item.width * z, item.height * z);
    }

    int IndexOf(string item) => Array.FindIndex(layout.menu, m => m.name == item);

    void EnsureStyles()
    {
        if (star != null && lineTexture != null) return;
        star = new GUIStyle(UIKit.Body) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
        star.normal.textColor = new Color(1f, 0.85f, 0.5f);
        dialogText = new GUIStyle(UIKit.Body) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        lineTexture = UIKit.MakeTexture(new Color(0.95f, 0.75f, 0.4f, 0.8f));
    }
}
