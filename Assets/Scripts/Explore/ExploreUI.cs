using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 꿈 탐색 화면(회색 프로토타입, IMGUI 1080p 기준). 게임플레이 기획 요약의 "탐색과 개방" 단계.
/// - 이안은 목표(꿈의 중심)에서 출발해 상하좌우로 한 칸씩 움직이며 암흑을 밝힌다. 결정 칸을 밝히면 몽결정을 얻는다.
/// - 막힌 암흑과 맵 밖으로는 못 간다. 결정·출현 지점은 암흑 속에서도 보인다.
/// - 출현 지점까지 길이 이어지면 "전투 준비"로 넘어간다(탐색 제한 없음). 몬스터는 밝힌 칸의 최단 경로로 온다.
///   몬스터 경로는 미리 보여 주지 않는다(기획: 연결 상태를 보고 판단).
/// 조작: WASD·방향키 이동(한 번 누르면 한 칸, 누르고 있으면 잠깐 뒤부터 천천히 반복), 인접 칸 클릭,
///       휠 확대/축소, 마우스 드래그로 화면 이동, F 전체 보기, Esc 메뉴.
/// </summary>
public class ExploreUI : MonoBehaviour
{
    [Tooltip("메인 UI 조각(Assets/Art/Main/UI)과 전투 UI 임시 아이콘(Assets/Art/Battle/UI)")]
    public Sprite[] uiSprites = new Sprite[0];
    public TextAsset uiBorders;

    static readonly Color ColBack = new Color(0.03f, 0.03f, 0.07f);
    static readonly Color ColDark = new Color(0.07f, 0.06f, 0.13f);
    static readonly Color ColFloor = new Color(0.62f, 0.53f, 0.41f);
    static readonly Color ColWall = new Color(0.015f, 0.01f, 0.03f);
    static readonly Color ColWallMark = new Color(0.42f, 0.10f, 0.24f);
    static readonly Color ColSmall = new Color(0.62f, 0.45f, 1f);
    static readonly Color ColLarge = new Color(0.40f, 0.85f, 1f);
    static readonly Color ColSpawn = new Color(0.85f, 0.25f, 0.85f);
    static readonly Color ColGoal = new Color(0.25f, 0.55f, 1f);
    static readonly Color ColPlayer = new Color(1f, 0.85f, 0.45f);
    static readonly Color ColReach = new Color(1f, 0.9f, 0.6f, 0.12f);
    static readonly Color ColLine = new Color(1f, 1f, 1f, 0.06f);

    /// <summary>키를 누르면 바로 한 칸. 계속 누르고 있으면 이만큼 기다린 뒤부터 반복(실수로 두 칸 가지 않게).</summary>
    const float RepeatDelay = 0.4f;
    const float RepeatInterval = 0.18f;

    GridMap map;
    UISkin skin;
    Vector2Int player;
    int opened;
    bool connected;
    bool menuOpen;
    string toast;
    float toastUntil;
    float holdTimer;
    Vector2Int heldDir;

    Vector2 origin;
    float cell = 40f;
    const float DragThreshold = 8f;
    bool panning, pressing;
    Vector2 lastMouse, pressStart;

    void Start()
    {
        skin = new UISkin(uiSprites, uiBorders);
        map = DreamRun.Map;
        if (map == null)
        {
            // 에디터에서 이 씬만 바로 실행했을 때: 예시 맵으로 테스트
            GridMap example = MapStorage.Load("MAP_EXAMPLE_01");
            if (example == null) { SceneFlow.GoToMain(); return; }
            DreamRun.Begin(example, testMode: true);
            map = DreamRun.Map;
        }
        player = map.HasGoal ? new Vector2Int(map.GoalX, map.GoalY) : Vector2Int.zero;
        connected = map.ShortestPath() != null;
        FitView();
        ShowToast("목표(파란 칸)에서 출발해요. 출현 지점(보라 칸)까지 암흑을 밝혀 길을 이어 주세요.", 5f);
    }

    // ───────── 이동·개방 ─────────

    void Update()
    {
        if (map == null || menuOpen) return;
        // 새로 누른 키가 있으면 그 방향으로 바로 한 칸
        Vector2Int pressed = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) pressed = new Vector2Int(0, -1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) pressed = new Vector2Int(0, 1);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) pressed = new Vector2Int(-1, 0);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) pressed = new Vector2Int(1, 0);
        if (pressed != Vector2Int.zero)
        {
            heldDir = pressed;
            holdTimer = RepeatDelay;
            Step(player.x + pressed.x, player.y + pressed.y);
            return;
        }

        // 같은 방향을 계속 누르고 있을 때만, 잠깐 기다린 뒤 천천히 반복
        bool stillHeld = heldDir.y < 0 ? Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)
                       : heldDir.y > 0 ? Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)
                       : heldDir.x < 0 ? Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)
                       : heldDir.x > 0 && (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow));
        if (!stillHeld) { heldDir = Vector2Int.zero; return; }
        holdTimer -= Time.unscaledDeltaTime;
        if (holdTimer > 0f) return;
        holdTimer = RepeatInterval;
        Step(player.x + heldDir.x, player.y + heldDir.y);
    }

    /// <summary>옆 칸으로 한 걸음. 암흑·결정이면 밝히고 들어간다. 막힌 암흑·맵 밖은 못 간다.</summary>
    void Step(int x, int y)
    {
        if (Mathf.Abs(x - player.x) + Mathf.Abs(y - player.y) != 1) return;
        if (!map.InBounds(x, y)) return;
        if (!map.IsOpen(x, y))
        {
            CellType before = map.Get(x, y);
            int value = map.OpenCell(x, y);
            if (value < 0)
            {
                ShowToast("밝힐 수 없는 암흑이에요", 1.2f);
                return;
            }
            opened++;
            if (value > 0)
            {
                DreamRun.Collected += value;
                ShowToast((before == CellType.CrystalLarge ? "큰 몽결정" : "몽결정") + $" +{value}", 1.4f);
            }
            bool wasConnected = connected;
            connected = map.ShortestPath() != null;
            if (connected && !wasConnected) ShowToast("출현 지점까지 길이 이어졌어요! 더 탐색하거나 전투를 준비하세요.", 3f);
        }
        player = new Vector2Int(x, y);
        KeepPlayerInView();
    }

    void StartBattle()
    {
        if (!connected) return;
        SceneFlow.FinishExploration();
    }

    // ───────── 보기 ─────────

    Rect MapArea => new Rect(8f, 76f, UIKit.Width - 16f, UIKit.Height - 76f - 70f);

    void FitView()
    {
        Rect area = MapArea;
        cell = Mathf.Max(6f, Mathf.Min(area.width / map.Width, area.height / map.Height));
        origin = new Vector2(area.x + (area.width - cell * map.Width) / 2f, area.y + (area.height - cell * map.Height) / 2f);
    }

    void KeepPlayerInView()
    {
        Rect area = MapArea;
        Rect r = CellRect(player.x, player.y);
        float margin = cell * 2f;
        if (r.xMin < area.xMin + margin) origin.x += area.xMin + margin - r.xMin;
        if (r.xMax > area.xMax - margin) origin.x -= r.xMax - (area.xMax - margin);
        if (r.yMin < area.yMin + margin) origin.y += area.yMin + margin - r.yMin;
        if (r.yMax > area.yMax - margin) origin.y -= r.yMax - (area.yMax - margin);
    }

    Rect CellRect(int x, int y) => new Rect(origin.x + x * cell, origin.y + y * cell, cell, cell);

    Vector2Int CellAt(Vector2 p) => new Vector2Int(Mathf.FloorToInt((p.x - origin.x) / cell), Mathf.FloorToInt((p.y - origin.y) / cell));

    // ───────── 그리기 ─────────

    void OnGUI()
    {
        if (map == null) return;
        Event e = Event.current;
        Vector2 mouse = e.mousePosition / UIKit.Scale;
        UIKit.Begin();
        Fill(new Rect(0, 0, UIKit.Width, UIKit.Height), ColBack);
        if (!menuOpen) HandleMouse(e, mouse);
        if (e.type == EventType.Repaint) DrawMap();
        DrawHud();
        if (menuOpen) DrawMenu();
        HandleKeys(e);
        UIKit.End();
    }

    void DrawMap()
    {
        Rect area = MapArea;
        GUI.BeginClip(area);
        Vector2 shift = -area.position;
        float px = 1f / Mathf.Max(0.01f, UIKit.Scale);

        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                Rect r = CellRect(x, y);
                r.position += shift;
                if (r.xMax < 0 || r.yMax < 0 || r.x > area.width || r.y > area.height) continue;
                CellType type = map.Get(x, y);
                if (map.IsOpen(x, y) && type == CellType.Open) Fill(r, ColFloor);
                else if (type == CellType.Wall) { Fill(r, ColWall); Fill(Shrink(r, 0.34f), ColWallMark); }
                else
                {
                    Fill(r, ColDark);
                    if (type == CellType.CrystalSmall) Diamond(r, 0.3f, ColSmall);
                    else if (type == CellType.CrystalLarge) Diamond(r, 0.14f, ColLarge);
                }
                if (map.IsSpawn(x, y)) Marker(r, ColSpawn, "출현");
                if (map.IsGoal(x, y)) Marker(r, ColGoal, "목표");
            }

        // 지금 갈 수 있는 옆 칸(살짝 밝게)
        int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };
        for (int d = 0; d < 4; d++)
        {
            int nx = player.x + dx[d], ny = player.y + dy[d];
            if (!map.InBounds(nx, ny) || map.IsOpen(nx, ny) || !map.IsOpenable(nx, ny)) continue;
            Rect r = CellRect(nx, ny);
            r.position += shift;
            Fill(r, ColReach);
        }

        if (cell >= 8f)
        {
            Vector2 o = origin + shift;
            for (int x = 1; x < map.Width; x++) Fill(new Rect(o.x + x * cell, o.y, px, cell * map.Height), ColLine);
            for (int y = 1; y < map.Height; y++) Fill(new Rect(o.x, o.y + y * cell, cell * map.Width, px), ColLine);
        }

        Rect p = CellRect(player.x, player.y);
        p.position += shift;
        if (skin.Has("mock_ian")) skin.Icon(Shrink(p, -0.15f), "mock_ian");
        else Fill(Shrink(p, 0.2f), ColPlayer);
        Outline(Shrink(p, 0.04f), ColPlayer, 2f * px);
        GUI.EndClip();
    }

    void DrawHud()
    {
        float w = UIKit.Width, h = UIKit.Height;
        // 위·아래 얇은 띠만 쓰고 나머지는 전부 맵(화면을 최대한 안 가리게)
        var top = new Rect(8, 8, 520, 60);
        skin.Frame(top, "panel_indigo");
        string title = string.IsNullOrEmpty(map.Name) ? map.Id : map.Name;
        GUI.Label(new Rect(top.x + 18, top.y, top.width - 30, top.height), "꿈 탐색 · " + title + (DreamRun.TestMode ? "  <size=14>(테스트)</size>" : ""), skin.Text(22, UISkin.Light, TextAnchor.MiddleLeft, true));

        var coin = new Rect(w - 268, 8, 260, 60);
        skin.Frame(coin, "panel_indigo");
        skin.Icon(new Rect(coin.x + 12, coin.y + 10, 32, 40), "mock_icon_crystal");
        GUI.Label(new Rect(coin.x + 50, coin.y, 110, coin.height), DreamRun.Collected.ToString(), skin.Text(28, UISkin.Gold, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(coin.x + 120, coin.y, 128, coin.height), $"밝힌 칸 {opened}", skin.Text(15, UISkin.Muted, TextAnchor.MiddleRight));

        var bottom = new Rect(w / 2f - 480f, h - 62, 960, 54);
        skin.Frame(bottom, "panel_indigo");
        string hint = connected
            ? "길이 이어졌어요 · 몬스터는 밝힌 칸 중 가장 짧은 길로 와요"
            : "WASD·방향키 이동 · 드래그 화면 이동 · 휠 확대/축소 · F 전체 보기 · Esc 메뉴";
        GUI.Label(new Rect(bottom.x + 20, bottom.y, bottom.width - 270, bottom.height), hint, skin.Text(16, UISkin.Light));
        GUI.enabled = connected && !menuOpen;
        if (GUI.Button(new Rect(bottom.xMax - 240, bottom.y + 6, 232, 42), connected ? "전투 준비 →" : "출현 지점까지 이어 주세요", skin.Button(connected ? 20 : 15)))
            StartBattle();
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(toast) && Time.unscaledTime < toastUntil)
            UIKit.ShadowLabel(new Rect(0, h - 112, w, 40), toast, skin.Text(22, UISkin.Gold, TextAnchor.MiddleCenter, true));
    }

    void DrawMenu()
    {
        UIKit.DimScreen(0.6f);
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0, 260, w, 90), "탐색 메뉴", UIKit.Heading);
        float x = (w - 420f) / 2f, y = 400f;
        if (GUI.Button(new Rect(x, y, 420, 76), "계속 탐색", skin.Button(26))) menuOpen = false;
        y += 96;
        if (GUI.Button(new Rect(x, y, 420, 76), "처음부터 다시 탐색", skin.Button(26))) SceneFlow.RestartStage();
        y += 96;
        if (DreamRun.TestMode && SceneFlow.DeveloperMode)
        {
            if (GUI.Button(new Rect(x, y, 420, 76), "맵 에디터로", skin.Button(26))) SceneFlow.GoToMapEditor();
            y += 96;
        }
        if (GUI.Button(new Rect(x, y, 420, 76), "수선소로", skin.Button(26))) SceneFlow.GoToMain();
    }

    // ───────── 입력 ─────────

    void HandleMouse(Event e, Vector2 mouse)
    {
        bool overMap = MapArea.Contains(mouse);
        switch (e.type)
        {
            case EventType.ScrollWheel:
                if (!overMap) return;
                float next = Mathf.Clamp(cell * (e.delta.y > 0 ? 0.88f : 1f / 0.88f), 6f, 140f);
                origin = mouse - (mouse - origin) * (next / cell);
                cell = next;
                e.Use();
                break;
            case EventType.MouseDown:
                if (!overMap) return;
                // 누른 채 끌면 화면 이동, 끌지 않고 떼면(왼쪽 버튼) 그 칸으로 한 걸음
                pressing = true;
                panning = false;
                pressStart = lastMouse = mouse;
                e.Use();
                break;
            case EventType.MouseDrag:
                if (!pressing) break;
                if (!panning && (mouse - pressStart).sqrMagnitude > DragThreshold * DragThreshold) panning = true;
                if (panning) { origin += mouse - lastMouse; lastMouse = mouse; }
                e.Use();
                break;
            case EventType.MouseUp:
                if (pressing && !panning && e.button == 0 && overMap)
                {
                    Vector2Int c = CellAt(mouse);
                    Step(c.x, c.y);
                }
                pressing = panning = false;
                break;
        }
    }

    void HandleKeys(Event e)
    {
        if (e.type != EventType.KeyDown) return;
        if (e.keyCode == KeyCode.Escape) { menuOpen = !menuOpen; e.Use(); }
        else if (!menuOpen && e.keyCode == KeyCode.F) { FitView(); e.Use(); }
        else if (!menuOpen && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && connected) { StartBattle(); e.Use(); }
    }

    void ShowToast(string text, float seconds)
    {
        toast = text;
        toastUntil = Time.unscaledTime + seconds;
    }

    // ───────── 도움 ─────────

    static void Fill(Rect r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Outline(Rect r, Color color, float t)
    {
        Fill(new Rect(r.x, r.y, r.width, t), color);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), color);
        Fill(new Rect(r.x, r.y, t, r.height), color);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), color);
    }

    /// <summary>결정 표시: 마름모 대신 가운데 정사각형 두 개(겹쳐서 반짝이는 느낌).</summary>
    static void Diamond(Rect r, float inset, Color color)
    {
        Fill(Shrink(r, inset), color);
        Fill(Shrink(r, inset + (0.5f - inset) * 0.5f), Color.Lerp(color, Color.white, 0.6f));
    }

    void Marker(Rect r, Color color, string label)
    {
        Fill(Shrink(r, 0.06f), color);
        if (cell >= 22f)
            GUI.Label(r, label, skin.Text(Mathf.Clamp(Mathf.RoundToInt(cell * 0.32f), 10, 24), Color.white, TextAnchor.MiddleCenter, true));
    }

    static Rect Shrink(Rect r, float ratio) =>
        new Rect(r.x + r.width * ratio, r.y + r.height * ratio, r.width * (1f - 2f * ratio), r.height * (1f - 2f * ratio));
}
