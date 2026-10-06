using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 개발자용 맵 에디터(IMGUI, 1080p 기준 좌표). 로비에서 F12(에디터·개발 빌드에서만) 또는 메뉴 Defense → 맵 에디터 열기.
/// - 왼쪽 패널: 맵 ID·이름, 모눈 크기(가로×세로, 자주 쓰는 크기 버튼), 도구, 결정 값, 저장·불러오기, 검사 결과
/// - 오른쪽: 모눈. 왼쪽 클릭·드래그로 칠하기, 휠로 확대/축소(마우스 위치 기준),
///   오른쪽·가운데 드래그 또는 Alt+드래그로 이동(맥 트랙패드), F 전체 보기
/// - 단축키: 1~6 도구, Ctrl+Z 되돌리기, Ctrl+S 저장, P 경로 미리보기
/// 맵은 전부 암흑에서 시작한다. 길은 플레이어가 탐색(결정 수집)하며 직접 여는 것이라 에디터에서는 그리지 않고,
/// 결정·막힌 암흑·크기·시작점·끝점만 정한다. 경로 미리보기는 "막힌 암흑만 빼고 전부 밝혔을 때"의 최단 경로다.
/// </summary>
public class MapEditorUI : MonoBehaviour
{
    enum Tool { Dark, Wall, CrystalSmall, CrystalLarge, Spawn, Goal }

    static readonly string[] ToolNames = { "암흑 (지우개)", "막힌 암흑 (벽)", "작은 결정", "큰 결정", "시작점 (몬스터 출현)", "끝점 (목표)" };
    static readonly int[,] Presets = { { 16, 9 }, { 24, 13 }, { 32, 18 }, { 48, 27 } };

    static readonly Color ColDark = new Color(0.10f, 0.09f, 0.18f);
    static readonly Color ColOpen = new Color(0.80f, 0.70f, 0.54f);
    static readonly Color ColWall = new Color(0.03f, 0.02f, 0.05f);
    static readonly Color ColWallMark = new Color(0.55f, 0.15f, 0.30f);
    static readonly Color ColSmall = new Color(0.62f, 0.45f, 1f);
    static readonly Color ColLarge = new Color(0.40f, 0.85f, 1f);
    static readonly Color ColSpawn = new Color(0.85f, 0.25f, 0.85f);
    static readonly Color ColGoal = new Color(0.25f, 0.55f, 1f);
    static readonly Color ColPath = new Color(1f, 0.80f, 0.30f);
    static readonly Color ColGridLine = new Color(1f, 1f, 1f, 0.09f);
    static readonly Color ColGridLine5 = new Color(1f, 1f, 1f, 0.2f);
    static readonly Color ColBorder = new Color(0.88f, 0.75f, 0.49f, 0.9f);

    const float PanelWidth = 380f;
    const float StatusHeight = 40f;
    const int UndoLimit = 60;

    GridMap map;
    Tool tool = Tool.Wall;
    readonly List<GridMap> undo = new List<GridMap>();
    bool dirty;
    bool showPath = true;
    string widthText, heightText, smallText, largeText;
    string message = "";
    float messageUntil;
    bool loadOpen;
    Vector2 loadScroll;
    List<string> mapIds = new List<string>();

    // 보기: 칸(0,0)의 왼쪽 위 화면 좌표와 칸 크기(1080p 기준 픽셀)
    Vector2 origin;
    float cell = 24f;
    bool panning, painting;
    Vector2 lastMouse;
    Vector2Int hoverCell = new Vector2Int(-1, -1);

    List<GridPoint> pathAll;
    List<string> errors = new List<string>();

    /// <summary>테스트 플레이에서 돌아왔을 때 이어서 편집할 맵.</summary>
    static GridMap resume;
    static bool resumeDirty;

    void Start()
    {
        if (resume != null)
        {
            map = resume;
            dirty = resumeDirty;
            resume = null;
            AfterChange();
            FitView();
            return;
        }
        NewMap(32, 18);
    }

    /// <summary>지금 맵으로 탐색 → 전투를 해 본다(첫 스테이지의 웨이브·타워 사용). 끝나면 "맵 에디터로"로 돌아온다.</summary>
    void TestPlay()
    {
        Recalculate();
        if (errors.Count > 0) { ShowMessage("검사 오류를 먼저 고쳐 주세요: " + errors[0]); return; }
        resume = map;
        resumeDirty = dirty;
        SceneFlow.StartDreamTest(map.Clone());
    }

    // ───────── 맵 상태 ─────────

    void NewMap(int width, int height)
    {
        map = new GridMap(width, height) { Id = "MAP_NEW", Name = "새 맵" };
        undo.Clear();
        dirty = false;
        AfterChange();
        FitView();
    }

    void AfterChange()
    {
        widthText = map.Width.ToString();
        heightText = map.Height.ToString();
        smallText = map.SmallCrystalValue.ToString();
        largeText = map.LargeCrystalValue.ToString();
        Recalculate();
    }

    void Recalculate()
    {
        pathAll = map.ShortestPathIfAllOpened();
        errors = map.Validate();
    }

    void PushUndo()
    {
        undo.Add(map.Clone());
        if (undo.Count > UndoLimit) undo.RemoveAt(0);
    }

    void Undo()
    {
        if (undo.Count == 0) return;
        map = undo[undo.Count - 1];
        undo.RemoveAt(undo.Count - 1);
        dirty = true;
        AfterChange();
    }

    void Apply(int x, int y)
    {
        if (!map.InBounds(x, y)) return;
        switch (tool)
        {
            case Tool.Spawn:
                map.SpawnX = x; map.SpawnY = y;
                if (map.Get(x, y) == CellType.Wall) map.Set(x, y, CellType.Dark);
                break;
            case Tool.Goal:
                map.GoalX = x; map.GoalY = y;
                if (map.Get(x, y) == CellType.Wall) map.Set(x, y, CellType.Dark);
                break;
            default:
                CellType type = ToolCell(tool);
                if (map.Get(x, y) == type) return;
                map.Set(x, y, type);
                break;
        }
        dirty = true;
        Recalculate();
    }

    static CellType ToolCell(Tool t)
    {
        switch (t)
        {
            case Tool.Wall: return CellType.Wall;
            case Tool.CrystalSmall: return CellType.CrystalSmall;
            case Tool.CrystalLarge: return CellType.CrystalLarge;
            default: return CellType.Dark;
        }
    }

    void ResizeTo(int width, int height)
    {
        PushUndo();
        map.Resize(width, height);
        dirty = true;
        AfterChange();
        FitView();
        ShowMessage($"크기를 {map.Width} × {map.Height}칸으로 바꿨어요");
    }

    void Save()
    {
        Recalculate();
        string path = MapStorage.Save(map);
        dirty = false;
        ShowMessage(errors.Count == 0 ? $"저장했어요: {path}" : $"저장했어요(검사 오류 {errors.Count}개 남음): {path}");
    }

    void Load(string id)
    {
        GridMap loaded = MapStorage.Load(id);
        if (loaded == null) { ShowMessage($"불러오지 못했어요: {id}"); return; }
        map = loaded;
        undo.Clear();
        dirty = false;
        loadOpen = false;
        AfterChange();
        FitView();
        ShowMessage($"불러왔어요: {id} ({map.Width}×{map.Height})");
    }

    void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 4f;
    }

    // ───────── 보기 ─────────

    Rect CanvasRect => new Rect(PanelWidth, 0f, UIKit.Width - PanelWidth, UIKit.Height - StatusHeight);

    void FitView()
    {
        Rect area = CanvasRect;
        // 좌표 숫자·크기 표시가 들어갈 여백을 둔다.
        cell = Mathf.Max(4f, Mathf.Min((area.width - 110f) / map.Width, (area.height - 110f) / map.Height));
        origin = new Vector2(area.x + (area.width - cell * map.Width) / 2f, area.y + (area.height - cell * map.Height) / 2f);
    }

    void ZoomAt(Vector2 mouse, float delta)
    {
        float next = Mathf.Clamp(cell * (delta > 0 ? 0.88f : 1f / 0.88f), 4f, 160f);
        origin = mouse - (mouse - origin) * (next / cell);
        cell = next;
    }

    Vector2Int CellAt(Vector2 p) => new Vector2Int(Mathf.FloorToInt((p.x - origin.x) / cell), Mathf.FloorToInt((p.y - origin.y) / cell));

    Rect CellRect(int x, int y) => new Rect(origin.x + x * cell, origin.y + y * cell, cell, cell);

    // ───────── 그리기 ─────────

    void OnGUI()
    {
        if (map == null) return;
        Event e = Event.current;
        Vector2 mouse = e.mousePosition / UIKit.Scale; // 화면 픽셀 → 1080p 기준 좌표
        UIKit.Begin();

        HandleCanvasInput(e, mouse);
        if (e.type == EventType.Repaint) DrawGrid();
        DrawPanel();
        DrawStatus();
        if (loadOpen) DrawLoadList();
        HandleKeys(e);
        UIKit.End();
    }

    void DrawGrid()
    {
        Rect area = CanvasRect;
        Fill(area, new Color(0.04f, 0.035f, 0.08f));
        GUI.BeginClip(area);
        Vector2 shift = new Vector2(-area.x, -area.y);

        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                Rect r = CellRect(x, y);
                r.position += shift;
                if (r.xMax < 0 || r.yMax < 0 || r.x > area.width || r.y > area.height) continue;
                Rect inner = r;
                CellType type = map.Get(x, y);
                switch (type)
                {
                    case CellType.Open: Fill(inner, ColOpen); break;
                    case CellType.Wall:
                        Fill(inner, ColWall);
                        Fill(Shrink(inner, 0.3f), ColWallMark);
                        break;
                    case CellType.CrystalSmall:
                        Fill(inner, ColDark);
                        Fill(Shrink(inner, 0.3f), ColSmall);
                        break;
                    case CellType.CrystalLarge:
                        Fill(inner, ColDark);
                        Fill(Shrink(inner, 0.12f), ColLarge);
                        break;
                    default: Fill(inner, ColDark); break;
                }
                if (map.IsSpawn(x, y)) Marker(inner, ColSpawn, "시작");
                if (map.IsGoal(x, y)) Marker(inner, ColGoal, "끝");
            }

        DrawGridLines(shift);
        if (showPath) DrawPath(pathAll, ColPath, 0.28f, shift);

        if (map.InBounds(hoverCell.x, hoverCell.y))
        {
            Rect r = CellRect(hoverCell.x, hoverCell.y);
            r.position += shift;
            Outline(r, Color.white);
        }
        GUI.EndClip();
    }

    /// <summary>칸 경계선(연하게), 5칸마다 조금 진하게, 바깥 테두리, 5칸마다 좌표 숫자.</summary>
    void DrawGridLines(Vector2 shift)
    {
        float px = 1f / Mathf.Max(0.01f, UIKit.Scale); // 실제 화면 1픽셀
        Vector2 o = origin + shift;
        float w = cell * map.Width, h = cell * map.Height;
        if (cell >= 6f)
        {
            for (int x = 1; x < map.Width; x++)
                Fill(new Rect(o.x + x * cell - px / 2f, o.y, px, h), x % 5 == 0 ? ColGridLine5 : ColGridLine);
            for (int y = 1; y < map.Height; y++)
                Fill(new Rect(o.x, o.y + y * cell - px / 2f, w, px), y % 5 == 0 ? ColGridLine5 : ColGridLine);
        }
        float b = 2f * px;
        Fill(new Rect(o.x - b, o.y - b, w + 2 * b, b), ColBorder);
        Fill(new Rect(o.x - b, o.y + h, w + 2 * b, b), ColBorder);
        Fill(new Rect(o.x - b, o.y, b, h), ColBorder);
        Fill(new Rect(o.x + w, o.y, b, h), ColBorder);

        GUIStyle label = Text(14, new Color(1f, 1f, 1f, 0.55f), TextAnchor.LowerCenter);
        GUIStyle side = Text(14, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleRight);
        for (int x = 0; x < map.Width; x += 5)
            GUI.Label(new Rect(o.x + x * cell, o.y - 22f, Mathf.Max(cell, 24f), 20f), x.ToString(), label);
        for (int y = 0; y < map.Height; y += 5)
            GUI.Label(new Rect(o.x - 34f, o.y + y * cell, 30f, Mathf.Max(cell, 18f)), y.ToString(), side);
        GUI.Label(new Rect(o.x, o.y + h + 6f, w, 22f), $"{map.Width} × {map.Height}칸", Text(16, ColBorder, TextAnchor.UpperCenter, true));
    }

    void DrawPath(List<GridPoint> path, Color color, float size, Vector2 shift)
    {
        if (path == null) return;
        foreach (GridPoint p in path)
        {
            Rect r = CellRect(p.X, p.Y);
            r.position += shift;
            Fill(Shrink(r, 0.5f - size / 2f), color);
        }
    }

    void Marker(Rect r, Color color, string label)
    {
        Fill(Shrink(r, 0.08f), color);
        if (cell >= 22f)
            GUI.Label(r, label, Text(Mathf.Clamp(Mathf.RoundToInt(cell * 0.38f), 10, 28), Color.white, TextAnchor.MiddleCenter, true));
    }

    void DrawPanel()
    {
        var panel = new Rect(0, 0, PanelWidth, UIKit.Height);
        Fill(panel, new Color(0.09f, 0.08f, 0.16f));
        float x = 20, w = PanelWidth - 40, y = 16;

        GUI.Label(new Rect(x, y, w, 36), "맵 에디터 <size=16>(개발자용)</size>", Text(26, Color.white, TextAnchor.MiddleLeft, true));
        y += 44;

        GUI.Label(new Rect(x, y, 70, 30), "ID", Text(18, Color.white));
        string id = GUI.TextField(new Rect(x + 70, y, w - 70, 30), map.Id);
        if (id != map.Id) { map.Id = id; dirty = true; Recalculate(); }
        y += 36;
        GUI.Label(new Rect(x, y, 70, 30), "이름", Text(18, Color.white));
        string mapName = GUI.TextField(new Rect(x + 70, y, w - 70, 30), map.Name);
        if (mapName != map.Name) { map.Name = mapName; dirty = true; }
        y += 46;

        // 모눈 크기
        GUI.Label(new Rect(x, y, w, 28), $"모눈 크기  <b>현재 {map.Width} × {map.Height}</b>", Text(18, Color.white));
        y += 30;
        widthText = GUI.TextField(new Rect(x, y, 80, 32), widthText);
        GUI.Label(new Rect(x + 84, y, 24, 32), "×", Text(20, Color.white, TextAnchor.MiddleCenter));
        heightText = GUI.TextField(new Rect(x + 112, y, 80, 32), heightText);
        int tw = 0, th = 0;
        bool pending = int.TryParse(widthText, out tw) && int.TryParse(heightText, out th) && (tw != map.Width || th != map.Height);
        if (GUI.Button(new Rect(x + 200, y, w - 200, 32), pending ? "크기 적용 ←" : "크기 적용") && pending) ResizeTo(tw, th);
        y += 38;
        for (int i = 0; i < Presets.GetLength(0); i++)
        {
            int pw = Presets[i, 0], ph = Presets[i, 1];
            bool current = pw == map.Width && ph == map.Height;
            if (current) Fill(new Rect(x + i * (w / 4f) - 2, y - 2, w / 4f, 34), ColBorder);
            if (GUI.Button(new Rect(x + i * (w / 4f), y, w / 4f - 4, 30), $"{pw}×{ph}") && !current) ResizeTo(pw, ph);
        }
        y += 34;
        GUI.Label(new Rect(x, y, w, 22), pending ? "숫자를 바꿨으면 '크기 적용'을 눌러 주세요" : $"{GridMap.MinSize}~{GridMap.MaxSize}칸 · 버튼은 바로 적용", Text(14, pending ? new Color(1f, 0.8f, 0.4f) : new Color(0.7f, 0.68f, 0.8f)));
        y += 30;

        // 도구
        GUI.Label(new Rect(x, y, w, 28), "도구 (숫자키 1~6)", Text(18, Color.white));
        y += 30;
        Color[] swatches = { ColDark, ColWallMark, ColSmall, ColLarge, ColSpawn, ColGoal };
        for (int i = 0; i < ToolNames.Length; i++)
        {
            var r = new Rect(x, y, w, 34);
            bool active = (int)tool == i;
            if (active) Fill(r, new Color(0.35f, 0.28f, 0.6f));
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) tool = (Tool)i;
            Fill(new Rect(r.x + 8, r.y + 7, 20, 20), swatches[i]);
            GUI.Label(new Rect(r.x + 38, r.y, r.width - 40, r.height), $"{i + 1}. {ToolNames[i]}", Text(18, active ? Color.white : new Color(0.8f, 0.78f, 0.9f)));
            y += 36;
        }
        y += 8;

        // 결정 값
        GUI.Label(new Rect(x, y, w, 28), "결정 하나의 몽결정 값", Text(18, Color.white));
        y += 30;
        GUI.Label(new Rect(x, y, 50, 30), "작은", Text(16, Color.white));
        smallText = GUI.TextField(new Rect(x + 50, y, 70, 30), smallText);
        GUI.Label(new Rect(x + 140, y, 40, 30), "큰", Text(16, Color.white));
        largeText = GUI.TextField(new Rect(x + 180, y, 70, 30), largeText);
        int sv, lv;
        if (int.TryParse(smallText, out sv) && sv != map.SmallCrystalValue) { map.SmallCrystalValue = sv; dirty = true; Recalculate(); }
        if (int.TryParse(largeText, out lv) && lv != map.LargeCrystalValue) { map.LargeCrystalValue = lv; dirty = true; Recalculate(); }
        y += 42;

        showPath = GUI.Toggle(new Rect(x, y, w, 26), showPath, " 최단 경로 미리보기 (P, 전부 밝혔을 때)");
        y += 36;

        // 파일
        float bw = (w - 8) / 3f;
        if (GUI.Button(new Rect(x, y, bw, 40), "새 맵"))
        {
            int nw, nh;
            NewMap(int.TryParse(widthText, out nw) ? nw : 32, int.TryParse(heightText, out nh) ? nh : 18);
        }
        if (GUI.Button(new Rect(x + bw + 4, y, bw, 40), dirty ? "저장 *" : "저장")) Save();
        if (GUI.Button(new Rect(x + 2 * (bw + 4), y, bw, 40), "불러오기"))
        {
            mapIds = MapStorage.ListIds();
            loadOpen = !loadOpen;
        }
        y += 50;

        // 검사
        if (errors.Count == 0)
            GUI.Label(new Rect(x, y, w, 26), "검사 통과", Text(18, new Color(0.5f, 1f, 0.6f), TextAnchor.MiddleLeft, true));
        else
            for (int i = 0; i < errors.Count && i < 4; i++, y += 24)
                GUI.Label(new Rect(x, y, w, 24), "· " + errors[i], Text(15, new Color(1f, 0.6f, 0.6f)));
        y = Mathf.Max(y + 30, UIKit.Height - 70);
        GUI.enabled = errors.Count == 0;
        if (GUI.Button(new Rect(x, UIKit.Height - 108, w, 44), "▶ 테스트 플레이 (탐색 → 전투)")) TestPlay();
        GUI.enabled = true;
        if (GUI.Button(new Rect(x, UIKit.Height - 56, w, 40), "로비로")) SceneFlow.GoToLobby();
    }

    void DrawStatus()
    {
        var bar = new Rect(PanelWidth, UIKit.Height - StatusHeight, UIKit.Width - PanelWidth, StatusHeight);
        Fill(bar, new Color(0.07f, 0.06f, 0.12f));
        int small = map.Count(CellType.CrystalSmall), large = map.Count(CellType.CrystalLarge);
        int total = small * map.SmallCrystalValue + large * map.LargeCrystalValue;
        string hover = map.InBounds(hoverCell.x, hoverCell.y) ? $"칸 ({hoverCell.x}, {hoverCell.y})" : "칸 -";
        string all = pathAll != null ? $"{pathAll.Count - 1}칸" : "이을 수 없음";
        string text = $"{map.Width}×{map.Height}   ·   {hover}   ·   결정 작은 {small} / 큰 {large} (합계 {total})   ·   최단 경로(전부 밝혔을 때) {all}   ·   확대 {cell:0}px";
        if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil) text = message;
        GUI.Label(new Rect(bar.x + 16, bar.y, bar.width - 32, bar.height), text, Text(16, new Color(0.85f, 0.82f, 0.95f)));
    }

    void DrawLoadList()
    {
        var r = new Rect(PanelWidth + 20, 80, 420, 520);
        Fill(r, new Color(0.12f, 0.10f, 0.22f, 0.97f));
        GUI.Label(new Rect(r.x + 16, r.y + 8, r.width - 32, 34), "불러올 맵", Text(22, Color.white, TextAnchor.MiddleLeft, true));
        var view = new Rect(r.x + 12, r.y + 48, r.width - 24, r.height - 110);
        loadScroll = GUI.BeginScrollView(view, loadScroll, new Rect(0, 0, view.width - 20, Mathf.Max(view.height, mapIds.Count * 40)));
        for (int i = 0; i < mapIds.Count; i++)
            if (GUI.Button(new Rect(0, i * 40, view.width - 24, 36), mapIds[i])) Load(mapIds[i]);
        if (mapIds.Count == 0) GUI.Label(new Rect(0, 0, view.width, 30), "저장된 맵이 없어요", Text(16, Color.white));
        GUI.EndScrollView();
        if (GUI.Button(new Rect(r.x + 16, r.yMax - 52, r.width - 32, 40), "닫기")) loadOpen = false;
    }

    // ───────── 입력 ─────────

    void HandleCanvasInput(Event e, Vector2 mouse)
    {
        bool overCanvas = CanvasRect.Contains(mouse) && !(loadOpen && new Rect(PanelWidth + 20, 80, 420, 520).Contains(mouse));
        hoverCell = overCanvas ? CellAt(mouse) : new Vector2Int(-1, -1);

        switch (e.type)
        {
            case EventType.ScrollWheel:
                if (!overCanvas) return;
                ZoomAt(mouse, e.delta.y);
                e.Use();
                break;
            case EventType.MouseDown:
                if (!overCanvas) return;
                GUIUtility.keyboardControl = 0; // 입력칸에서 빠져나와 단축키가 먹게
                if (e.button == 0 && !e.alt)
                {
                    PushUndo();
                    painting = true;
                    Apply(hoverCell.x, hoverCell.y);
                }
                else
                {
                    panning = true;
                    lastMouse = mouse;
                }
                e.Use();
                break;
            case EventType.MouseDrag:
                if (painting && overCanvas) { Apply(hoverCell.x, hoverCell.y); e.Use(); }
                else if (panning) { origin += mouse - lastMouse; lastMouse = mouse; e.Use(); }
                break;
            case EventType.MouseUp:
                painting = false;
                panning = false;
                break;
        }
    }

    void HandleKeys(Event e)
    {
        if (e.type != EventType.KeyDown || GUIUtility.keyboardControl != 0) return;
        bool ctrl = e.control || e.command;
        if (ctrl && e.keyCode == KeyCode.Z) { Undo(); e.Use(); return; }
        if (ctrl && e.keyCode == KeyCode.S) { Save(); e.Use(); return; }
        if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha6) { tool = (Tool)(e.keyCode - KeyCode.Alpha1); e.Use(); }
        else if (e.keyCode == KeyCode.F) { FitView(); e.Use(); }
        else if (e.keyCode == KeyCode.P) { showPath = !showPath; e.Use(); }
        else if (e.keyCode == KeyCode.Escape) { loadOpen = false; GUIUtility.keyboardControl = 0; e.Use(); }
    }

    // ───────── 도움 ─────────

    static void Fill(Rect r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Outline(Rect r, Color color)
    {
        Fill(new Rect(r.x, r.y, r.width, 2), color);
        Fill(new Rect(r.x, r.yMax - 2, r.width, 2), color);
        Fill(new Rect(r.x, r.y, 2, r.height), color);
        Fill(new Rect(r.xMax - 2, r.y, 2, r.height), color);
    }

    static Rect Shrink(Rect r, float ratio) =>
        new Rect(r.x + r.width * ratio, r.y + r.height * ratio, r.width * (1f - 2f * ratio), r.height * (1f - 2f * ratio));

    readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    GUIStyle Text(int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
    {
        string key = $"{size}:{color}:{anchor}:{bold}";
        GUIStyle style;
        if (styles.TryGetValue(key, out style)) return style;
        style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, richText = true, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        // 글자는 클릭 대상이 아니므로 마우스를 올려도 색이 바뀌지 않게 모든 상태를 같은 색으로.
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
        styles[key] = style;
        return style;
    }
}
