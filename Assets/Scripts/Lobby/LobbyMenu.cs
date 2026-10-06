using System;
using UnityEngine;

/// <summary>
/// 로비(첫 화면) 메뉴: 이어하기 / 새로하기 / 설정 / 종료.
/// 메뉴 글자는 로비 아트(06_logo_menu)에서 잘라낸 이미지이고, 위치는 lobby_layout.json의 "menu"에 있다.
/// 마우스를 올리거나 ↑↓ 키로 고르면 별 표시와 금색 밑줄이 붙고, 클릭·Enter로 해당 팝업(LobbyPopups)을 연다.
/// 팝업: 이어하기(저장 기록) · 새로하기(새로운 이야기) · 설정(사운드·화면·조작) · 종료. Esc로 닫는다.
/// 팝업 아트가 없으면 예전처럼 바로 실행한다.
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

    [Header("팝업 (Assets/Art/Lobby/UI/Popups)")]
    public Sprite[] popupSprites = new Sprite[0];
    public TextAsset popupBounds;
    public TextAsset continueLayout, newLayout, settingsLayout, quitLayout;

    Layout layout;
    Texture2D[] textures;
    int hovered = -1;
    Vector2 lastMouse = new Vector2(-1f, -1f);
    LobbyPopups popups;
    LobbyPopups.Kind popupItem;
    GUIStyle star;
    Texture2D lineTexture;

    public static bool HasSave => Progress.HighestCleared >= 0 || QuestProgress.HasAny;

    void Start()
    {
        Time.timeScale = 1f;
        layout = layoutJson != null ? JsonUtility.FromJson<Layout>(layoutJson.text) : new Layout();
        textures = new Texture2D[layout.menu.Length];
        for (int i = 0; i < layout.menu.Length; i++)
            foreach (Sprite sprite in menuSprites)
                if (sprite != null && sprite.name == layout.menu[i].name) textures[i] = sprite.texture;
        hovered = HasSave ? IndexOf("menu_continue") : IndexOf("menu_new");
        popups = new LobbyPopups(popupSprites, popupBounds, continueLayout, newLayout, settingsLayout, quitLayout);
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.Repaint || e.type == EventType.MouseMove)
            LayeredBackground.Pointer = new Vector2(e.mousePosition.x / Mathf.Max(1, Screen.width), e.mousePosition.y / Mathf.Max(1, Screen.height));

        UIKit.Begin();
        EnsureStyles();
        Vector2 mouse = e.mousePosition; // UIKit.Begin 이후라 1080p 기준 좌표
        bool overlay = popups.IsOpen;
        // 실행 중 OnGUI에는 MouseMove 이벤트가 오지 않아서, 다시 그릴 때 마우스가 움직였는지 본다.
        bool mouseMoved = e.type == EventType.Repaint && (mouse - lastMouse).sqrMagnitude > 0.5f;
        if (e.type == EventType.Repaint) lastMouse = mouse;

        // 마우스를 움직이면 마우스 아래 항목만 선택된 것으로 본다(메뉴 밖으로 나가면 별·밑줄이 사라짐).
        // 키보드 ↑↓로 고른 선택은 마우스를 움직이기 전까지 유지된다.
        if (!overlay && mouseMoved)
        {
            hovered = -1;
            for (int i = 0; i < layout.menu.Length; i++)
                if (IsEnabled(layout.menu[i].name) && ItemRect(layout.menu[i]).Contains(mouse)) hovered = i;
        }

        for (int i = 0; i < layout.menu.Length; i++)
        {
            Rect rect = ItemRect(layout.menu[i]);
            bool enabled = IsEnabled(layout.menu[i].name);

            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, !enabled ? 0.3f : i == hovered ? 1f : 0.72f);
            if (textures[i] != null) GUI.DrawTexture(rect, textures[i], ScaleMode.ScaleToFit);
            GUI.color = old;

            bool marked = overlay ? KindOf(layout.menu[i].name) == popupItem : i == hovered && enabled;
            if (marked)
            {
                if (popups.HasTexture("continue_menu_marker")) popups.DrawMenuMarker(rect);
                else
                {
                    GUI.Label(new Rect(rect.x - 44f, rect.center.y - 22f, 40f, 44f), "✦", star);
                    GUI.DrawTexture(new Rect(rect.x - 10f, rect.yMax - 4f, rect.width + 120f, 2f), lineTexture);
                }
            }

            if (!overlay && enabled && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(mouse))
            {
                e.Use();
                Activate(layout.menu[i].name);
            }
        }

        switch (popups.Draw(mouse))
        {
            case LobbyPopups.Action.Continue: SceneFlow.GoToMain(); break;
            case LobbyPopups.Action.StartNewGame: StartNewGame(); break;
            case LobbyPopups.Action.Quit: SceneFlow.QuitGame(); break;
        }
        UIKit.End();

        if (e.type == EventType.KeyDown) HandleKey(e);
    }

    void HandleKey(Event e)
    {
        if (e.keyCode == KeyCode.Escape)
        {
            popups.Close();
            e.Use();
            return;
        }
        if (popups.IsOpen) return;
        if (e.keyCode == KeyCode.F12 && SceneFlow.DeveloperMode)
        {
            SceneFlow.GoToMapEditor();
            e.Use();
            return;
        }
        if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
        {
            int step = e.keyCode == KeyCode.UpArrow ? -1 : 1;
            if (hovered < 0) hovered = step > 0 ? -1 : layout.menu.Length; // 아무것도 안 골랐으면 맨 위/아래부터
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
        LobbyPopups.Kind kind = KindOf(item);
        if (kind == LobbyPopups.Kind.None) return;
        if (popups.HasArt)
        {
            popupItem = kind;
            popups.Show(kind);
            return;
        }
        // 팝업 아트가 없을 때(예전 동작)
        if (kind == LobbyPopups.Kind.Continue) SceneFlow.GoToMain();
        else if (kind == LobbyPopups.Kind.New) StartNewGame();
        else if (kind == LobbyPopups.Kind.Quit) SceneFlow.QuitGame();
    }

    static LobbyPopups.Kind KindOf(string item)
    {
        switch (item)
        {
            case "menu_continue": return LobbyPopups.Kind.Continue;
            case "menu_new": return LobbyPopups.Kind.New;
            case "menu_settings": return LobbyPopups.Kind.Settings;
            case "menu_quit": return LobbyPopups.Kind.Quit;
            default: return LobbyPopups.Kind.None;
        }
    }

    static void StartNewGame()
    {
        Progress.ResetAll();
        QuestProgress.ResetAll();
        SaveInfo.ResetAll();
        // 프롤로그(이안의 꿈 → 도하 수선 → 첫 편지)는 4주차에 이 자리에서 재생한다.
        SceneFlow.GoToMain();
    }

    bool IsEnabled(string item) => item != "menu_continue" || HasSave;

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
        lineTexture = UIKit.MakeTexture(new Color(0.95f, 0.75f, 0.4f, 0.8f));
    }
}
