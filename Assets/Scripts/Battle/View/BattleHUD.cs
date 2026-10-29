using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 전투 UI(IMGUI, 1080p 기준 좌표). 배치는 전투 UI 시안(ArtSource/Battle_UI/전투UI_시안.png)을 따른다.
/// - 왼쪽 위: 스테이지 이름, 웨이브, (준비 중) 바로 시작 / 가운데 위: 악몽 침식도
/// - 오른쪽 위: 꿈의 불빛 HP, 몽결정(전투 재화), 일시정지, 배속
/// - 왼쪽 아래: 미니맵(클릭·드래그로 그 자리로 화면 이동) / 오른쪽 아래: 도하(스킬 자리)
/// - 건설: 지을 수 있는 칸을 클릭하면 그 칸 둘레에 타워 5종(병정인형·재봉틀·스탠드·오르골·스노우볼)이
///   동그란 버튼으로 펼쳐진다(건설 고리). 버튼을 누르거나 1~6으로 짓는다. 다른 곳 클릭·우클릭·Esc로 닫는다.
/// 아이콘은 시안에서 잘라 낸 임시 그림(Assets/Art/Battle/UI/mock_*)이다. 정식 UI 아트가 오면 교체한다.
/// 입력은 명령으로만 전달한다: 건설 / 강화 / 웨이브 시작 / 속도 / 일시정지 / 귀환.
/// 조작: 타워 클릭 → 정보·강화. Space 웨이브 바로 시작. Esc 일시정지.
///       개발용(에디터·개발 빌드): F1 재화 +100, F2 적 전부 처치.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    public BattleController controller;
    public BattleView view;
    public Camera worldCamera;
    public Sprite square;
    public Sprite circle;
    [Tooltip("메인 UI 조각(Assets/Art/Main/UI)과 전투 UI 임시 아이콘(Assets/Art/Battle/UI)")]
    public Sprite[] uiSprites = new Sprite[0];
    [Tooltip("Assets/Art/Main/UI/ui_borders.json")]
    public TextAsset uiBorders;

    /// <summary>건설 고리 6칸(수선소 물품 타워). 데이터에 없는 타워는 "준비 중"으로 잠가 보여 준다.</summary>
    static readonly Slot[] Slots =
    {
        new Slot("TW_LAMP", "스탠드 조명", "mock_tower_lamp", "따뜻한 빛줄기로 멀리 있는 한 적을 꾸준히 지집니다."),
        new Slot("TW_SCISSORS", "가위", "mock_tower_scissors", "가까이 온 적 여럿을 한꺼번에 싹둑 자릅니다. 3단계는 연달아 가위질."),
        new Slot("TW_SEWING", "재봉틀", "mock_tower_sewing", "빠르게 바늘을 쏘아 한 적을 꿰뚫습니다. 치명타가 잦아요."),
        new Slot("TW_TAPE", "줄자", "mock_tower_tape", "줄자를 휙 감아 적의 발을 묶어 느리게 합니다."),
        new Slot("TW_PIN", "바늘꽂이", "mock_tower_pin", "둘레 적들에게 시침핀을 꽂아 방어력을 깎습니다."),
        new Slot("TW_TAGBOW", "수선표 활", "mock_tower_tagbow", "멀리 있는 적에게 꼬리표를 꽂아, 모든 타워에게 더 아프게 맞게 합니다."),
    };

    /// <summary>여러 적 동시 공격·표식 설명 한 줄(없으면 빈 문자열).</summary>
    static string ExtraText(TowerLevelDef level)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (level.HitCount > 1) parts.Add($"한 번에 {level.HitCount}명");
        if (level.Marks && level.ArmorShred > 0f) parts.Add($"방어 -{level.ArmorShred:0} · {level.MarkSec:0.#}초");
        if (level.Marks && level.Vulnerability > 0f) parts.Add($"받는 피해 +{level.Vulnerability * 100f:0}% · {level.MarkSec:0.#}초");
        return string.Join(", ", parts);
    }

    class Slot
    {
        public readonly string Id, Name, Icon, Description;
        public Slot(string id, string name, string icon, string description) { Id = id; Name = name; Icon = icon; Description = description; }
    }

    /// <summary>
    /// 전투 HUD 크기 배율. 1080p 기준 좌표로 짠 HUD를 이 비율로 줄여 그려서 맵을 덜 가린다
    /// (모서리 기준 배치는 그대로 유지: W·H가 그만큼 넓은 가상 화면).
    /// </summary>
    const float HudScale = 0.72f;
    float W => UIKit.Width / HudScale;
    float H => UIKit.Height / HudScale;

    const float PanelWidth = 380f;
    /// <summary>건설 고리: 버튼 지름, 고리 반지름(칸 중심에서 버튼 중심까지), 펼쳐지는 시간(초).</summary>
    const float RingButton = 104f, RingRadius = 124f, RingOpenTime = 0.16f;
    /// <summary>미니맵이 들어갈 최대 크기(HUD 가상 좌표).</summary>
    const float MiniMaxW = 320f, MiniMaxH = 220f;

    BattleSession Session => controller.Session;

    UISkin skin;
    TowerDef[] slotTowers;
    TowerState selectedTower;
    bool ringOpen;
    Vector2Int ringTile;
    float ringOpenedAt;
    int ringHover = -1;
    Rect miniRect;
    bool miniDragging;
    Texture2D miniGrid;
    bool pauseMenu;
    string message;
    float messageUntil;
    Vector2 mouseScreen;       // IMGUI 좌표(왼쪽 위 원점, 픽셀)
    Vector2Int hoverTile;
    bool hoverOnMap;
    readonly List<Rect> uiRects = new List<Rect>();

    SpriteRenderer tilePreview;
    SpriteRenderer rangePreview;
    GUIStyle hudSmall, hudInk;

    void Start()
    {
        tilePreview = CreatePreview("TilePreview", square, 7);
        rangePreview = CreatePreview("RangePreview", circle, -5);
        skin = new UISkin(uiSprites, uiBorders);
    }

    void Update()
    {
        UpdatePreview();
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.Repaint || e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp)
            mouseScreen = e.mousePosition;

        UIKit.Begin();
        EnsureStyles();
        if (Session == null)
        {
            DrawDataErrors();
            UIKit.End();
            return;
        }
        if (slotTowers == null) MapSlots();

        uiRects.Clear();
        GUI.matrix = Matrix4x4.Scale(new Vector3(UIKit.Scale * HudScale, UIKit.Scale * HudScale, 1f));
        Vector2 virtualMouse = mouseScreen / (UIKit.Scale * HudScale);
        // 화면 테두리 레이어: 가장자리 비네트 + 위·아래 HUD 띠 뒤 밤색 그라데이션(맵 바깥이 화면 끝에서 자연스럽게 가라앉게)
        if (e.type == EventType.Repaint && DreamRun.Active) MapTheme.DrawScreenFrame(new Rect(0f, 0f, W, H), H * 0.14f, H * 0.21f);
        bool overlay = pauseMenu || Session.Phase == BattlePhase.Ended;
        GUI.enabled = !overlay;
        DrawStagePlate();
        DrawErosion();
        DrawStatus();
        if (ringOpen && (overlay || !RingStillValid())) ringOpen = false;
        DrawMinimap();
        DrawDoha();
        if (selectedTower != null) DrawTowerPanel();
        if (ringOpen) DrawBuildRing(virtualMouse);
        else ringHover = -1;
        DrawMessage();
        GUI.enabled = true;

        // 일시정지·결과 화면은 원래 크기로
        GUI.matrix = Matrix4x4.Scale(new Vector3(UIKit.Scale, UIKit.Scale, 1f));
        if (Session.Phase == BattlePhase.Ended) DrawResult();
        else if (pauseMenu) DrawPauseMenu();

        hoverOnMap = !overlay && !IsOverUI(virtualMouse);
        CameraZoom.PointerOverUI = !hoverOnMap;
        UIKit.End();

        HandleMinimapInput(e, virtualMouse, overlay);
        HandleInput(e, overlay, virtualMouse);
    }

    void MapSlots()
    {
        slotTowers = new TowerDef[Slots.Length];
        for (int i = 0; i < Slots.Length; i++)
        {
            Slot slot = Slots[i];
            slotTowers[i] = Session.Stage.Towers.Find(t => t != null && (t.Id == slot.Id || t.Name == slot.Name));
        }
    }

    /// <summary>UI 영역으로 기록한다(그 위에서는 맵 클릭이 먹히지 않는다).</summary>
    Rect Ui(Rect r)
    {
        uiRects.Add(r);
        return r;
    }

    // ───────── 위 ─────────

    void DrawStagePlate()
    {
        Rect plate = Ui(new Rect(24, 16, 470, 76));
        skin.Frame(plate, "ws_card_info");
        skin.Icon(new Rect(plate.x + 18, plate.y + 12, 52, 52), "ws_moon_emblem");
        int number = Mathf.Max(0, SceneFlow.CurrentStageIndex) + 1;
        string stageName = string.IsNullOrEmpty(Session.Stage.Name) ? Session.Stage.Id : Session.Stage.Name;
        GUI.Label(new Rect(plate.x + 78, plate.y, plate.width - 90, plate.height), $"{number:00} · {stageName}", skin.Text(30, UISkin.Ink, TextAnchor.MiddleLeft, true));

        Rect wave = Ui(new Rect(60, 96, 300, 44));
        skin.Frame(wave, "ws_bar");
        string remaining = Session.Phase == BattlePhase.Combat ? $"   ·   남은 적 {Session.AliveCount + Session.RemainingSpawns}" : "";
        GUI.Label(wave, $"웨이브 {Session.WaveIndex + 1} / {Session.Stage.Waves.Count}{remaining}", skin.Text(22, UISkin.Light, TextAnchor.MiddleCenter));

        if (Session.Phase == BattlePhase.Prepare)
        {
            Rect start = Ui(new Rect(60, 148, 300, 56));
            if (GUI.Button(start, $"바로 시작 [Space] · {Mathf.CeilToInt((float)Session.PrepareRemaining)}초", skin.Button(22))) Session.StartWaveNow();
        }
    }

    /// <summary>악몽 침식도(시스템 기획서). 침식 규칙은 2주차에 연결하고 지금은 0으로 둔다.</summary>
    void DrawErosion()
    {
        float w = W;
        Rect r = Ui(new Rect(w / 2f - 380f, 12, 760, 104));
        skin.Frame(r, "ws_card_info");
        float erosion = 0f;
        skin.Icon(new Rect(r.x + 26, r.y + 10, 40, 40), "ws_moon_emblem");
        GUI.Label(new Rect(r.x + 74, r.y + 8, 300, 44), "악몽 침식도", skin.Text(26, UISkin.Ink, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(r.xMax - 240, r.y + 8, 216, 44), $"<size=34><b>{erosion:0}</b></size> / 100", skin.Text(22, UISkin.Ink, TextAnchor.MiddleRight));
        var bar = new Rect(r.x + 28, r.y + 52, r.width - 56, 26);
        skin.Gauge(bar, erosion / 100f);
        int[] marks = { 30, 60, 90 };
        foreach (int mark in marks)
        {
            float x = bar.x + bar.width * mark / 100f;
            GUI.Label(new Rect(x - 30, bar.yMax, 60, 26), mark.ToString(), skin.Text(16, UISkin.InkMuted, TextAnchor.MiddleCenter));
        }
        int next = 30;
        foreach (int mark in marks) if (erosion < mark) { next = mark; break; }
        GUI.Label(new Rect(r.xMax - 200, bar.yMax, 176, 26), $"다음 임계: {next}", skin.Text(16, UISkin.Brown, TextAnchor.MiddleRight, true));
    }

    void DrawStatus()
    {
        float w = W;
        Rect hp = Ui(new Rect(w - 574, 16, 236, 80));
        skin.Frame(hp, "ws_card_info");
        skin.Icon(new Rect(hp.x + 12, hp.y + 12, 52, 56), "mock_icon_hp");
        GUI.Label(new Rect(hp.x + 70, hp.y + 8, hp.width - 80, 30), "꿈의 불빛 HP", skin.Text(18, UISkin.Brown, TextAnchor.MiddleCenter, true));
        GUI.Label(new Rect(hp.x + 70, hp.y + 36, hp.width - 80, 36), $"{Session.CoreHp} / {Session.Stage.CoreMaxHp}", skin.Text(26, UISkin.Ink, TextAnchor.MiddleCenter, true));

        Rect coin = Ui(new Rect(w - 328, 16, 180, 80));
        skin.Frame(coin, "ws_card_info");
        skin.Icon(new Rect(coin.x + 14, coin.y + 12, 42, 56), "ws_icon_crystal");
        GUI.Label(new Rect(coin.x + 62, coin.y + 8, coin.width - 70, 30), "몽결정", skin.Text(18, UISkin.Brown, TextAnchor.MiddleCenter, true));
        GUI.Label(new Rect(coin.x + 62, coin.y + 36, coin.width - 70, 36), Session.Coin.ToString(), skin.Text(28, UISkin.Ink, TextAnchor.MiddleCenter, true));

        Rect pause = Ui(new Rect(w - 136, 16, 80, 80));
        // 쇠고리 달린 꼬리표 모양 일시정지 버튼
        if (GUI.Button(pause, GUIContent.none, skin.FrameStyle("ws_tag"))) SetPauseMenu(true);
        GUI.Label(new Rect(pause.x, pause.y + 12, pause.width, pause.height - 12), "<b>II</b>", skin.Text(30, UISkin.Ink, TextAnchor.MiddleCenter));

        // 배속: 시안에는 없지만 테스트에 필요해서 작게 둔다.
        float x = w - 328;
        string[] labels = { "x1", "x2", "x4" };
        float[] speeds = { 1f, 2f, 4f };
        for (int i = 0; i < 3; i++)
        {
            Rect r = Ui(new Rect(x + i * 62, 104, 56, 40));
            bool active = Mathf.Approximately(controller.Speed, speeds[i]);
            if (GUI.Button(r, labels[i], skin.Tab(active)) && !active) controller.SetSpeed(speeds[i]);
        }
    }

    // ───────── 아래 ─────────

    /// <summary>도하 스킬 자리(3주차: 필드 지원). 지금은 모양만 보여 준다.</summary>
    void DrawDoha()
    {
        float w = W, h = H;
        Rect r = Ui(new Rect(w - 404, h - 208, 384, 190));
        skin.Frame(r, "ws_card_info");
        skin.Icon(new Rect(r.x + 22, r.y + 12, 32, 32), "ws_moon_emblem");
        GUI.Label(new Rect(r.x + 60, r.y + 8, 200, 38), "도하", skin.Text(24, UISkin.Ink, TextAnchor.MiddleLeft, true));
        skin.Icon(new Rect(r.x + 14, r.y + 50, 120, 128), "mock_doha");

        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.55f);
        string[] icons = { "mock_skill_q", "mock_skill_e" };
        string[] names = { "Q · 타워 강화", "E · 악몽 약화" };
        for (int i = 0; i < 2; i++)
        {
            var slot = new Rect(r.x + 150 + i * 116, r.y + 52, 96, 80);
            skin.Icon(slot, icons[i]);
            GUI.Label(new Rect(slot.x - 12, slot.yMax + 2, slot.width + 24, 26), names[i], skin.Text(16, UISkin.Ink, TextAnchor.MiddleCenter));
        }
        GUI.color = old;
        GUI.Label(new Rect(r.x + 140, r.yMax - 34, r.width - 150, 26), "스킬은 준비 중 · 우클릭 이동", skin.Text(15, UISkin.InkMuted, TextAnchor.MiddleCenter));
    }

    // ───────── 미니맵 ─────────

    /// <summary>
    /// 왼쪽 아래 미니맵: 맵 전체를 작게 그리고 타워(하늘색)·적(빨강)·출현 지점(보라)·꿈의 중심(금색)과
    /// 지금 화면에 보이는 영역(흰 테두리)을 표시한다. 누르거나 끌면 그 자리로 화면이 이동한다.
    /// </summary>
    void DrawMinimap()
    {
        MapDef map = Session.Stage.Map;
        float aspect = map.CameraWidth / Mathf.Max(0.01f, map.CameraHeight);
        float mw = MiniMaxW, mh = mw / aspect;
        if (mh > MiniMaxH) { mh = MiniMaxH; mw = mh * aspect; }
        const float pad = 18f;
        Rect back = Ui(new Rect(20, H - 20 - mh - pad * 2, mw + pad * 2, mh + pad * 2));
        skin.Frame(back, "ws_card_info"); // 황동 모서리 양피지 틀
        miniRect = new Rect(back.x + pad, back.y + pad, mw, mh);

        Color old = GUI.color;
        GUI.color = new Color(0.05f, 0.04f, 0.1f, 1f);
        GUI.DrawTexture(miniRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        MapAsset asset = controller.Content.StageAsset != null ? controller.Content.StageAsset.map : null;
        if (DreamRun.Active)
        {
            if (miniGrid == null) miniGrid = BuildGridTexture(DreamRun.Map);
            GUI.DrawTexture(miniRect, miniGrid);
        }
        else if (asset != null && asset.background != null)
        {
            DrawSprite(miniRect, asset.background, Color.white);
        }
        else
        {
            foreach (SpawnPointDef spawn in map.SpawnPoints)
                for (int i = 1; i < spawn.Path.Count; i++)
                {
                    Vector2 a = BattleContentBuilder.ToUnity(spawn.Path[i - 1]), b = BattleContentBuilder.ToUnity(spawn.Path[i]);
                    Vector2 min = Vector2.Min(a, b) - Vector2.one * 0.5f, max = Vector2.Max(a, b) + Vector2.one * 0.5f;
                    FillWorld(min, max, new Color(0.45f, 0.38f, 0.58f));
                }
        }

        foreach (SpawnPointDef spawn in map.SpawnPoints)
            Dot(BattleContentBuilder.ToUnity(spawn.Path[0]), 9f, new Color(0.75f, 0.45f, 1f));
        foreach (TowerState tower in Session.Towers)
            Dot(new Vector2(tower.TileX, tower.TileY), 7f, new Color(0.45f, 0.95f, 1f));
        foreach (EnemyState enemy in Session.Enemies)
            if (enemy.Alive) Dot(BattleContentBuilder.ToUnity(enemy.Position), enemy.Def.IsBoss ? 9f : 5f, new Color(1f, 0.32f, 0.32f));
        Dot(BattleContentBuilder.ToUnity(map.CorePos), 11f, new Color(1f, 0.8f, 0.4f));

        // 지금 보이는 영역
        if (worldCamera != null)
        {
            float halfH = worldCamera.orthographicSize, halfW = halfH * worldCamera.aspect;
            Vector3 c = worldCamera.transform.position;
            Rect view = WorldToMini(new Vector2(c.x - halfW, c.y - halfH), new Vector2(c.x + halfW, c.y + halfH));
            float x0 = Mathf.Max(view.xMin, miniRect.xMin), x1 = Mathf.Min(view.xMax, miniRect.xMax);
            float y0 = Mathf.Max(view.yMin, miniRect.yMin), y1 = Mathf.Min(view.yMax, miniRect.yMax);
            if (x1 > x0 && y1 > y0)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                const float t = 2f;
                GUI.DrawTexture(new Rect(x0, y0, x1 - x0, t), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x0, y1 - t, x1 - x0, t), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x0, y0, t, y1 - y0), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x1 - t, y0, t, y1 - y0), Texture2D.whiteTexture);
            }
        }
        GUI.color = old;
    }

    /// <summary>탐색 맵 한 칸 = 한 픽셀(달빛 정원 색: 돌길, 몬스터 경로는 더 밝게, 잔디, 꽃밭, 결정).</summary>
    static Texture2D BuildGridTexture(GridMap grid)
    {
        var texture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var onPath = new HashSet<Vector2Int>();
        List<GridPoint> path = grid.ShortestPath();
        if (path != null) foreach (GridPoint p in path) onPath.Add(new Vector2Int(p.X, p.Y));
        var pixels = new Color[grid.Width * grid.Height];
        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                CellType type = grid.Get(x, y);
                Color c;
                if (grid.IsOpen(x, y)) c = onPath.Contains(new Vector2Int(x, y)) ? new Color(0.93f, 0.86f, 1f) : new Color(0.72f, 0.7f, 0.84f);
                else if (type == CellType.Wall) c = new Color(0.14f, 0.16f, 0.3f);
                else if (type == CellType.CrystalSmall) c = new Color(0.62f, 0.45f, 1f);
                else if (type == CellType.CrystalLarge) c = new Color(0.45f, 0.85f, 1f);
                else c = new Color(0.12f, 0.3f, 0.28f);
                pixels[(grid.Height - 1 - y) * grid.Width + x] = c; // 텍스처 아래쪽 줄 = 월드 y 0
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    Rect WorldToMini(Vector2 min, Vector2 max)
    {
        MapDef map = Session.Stage.Map;
        float sx = miniRect.width / map.CameraWidth, sy = miniRect.height / map.CameraHeight;
        float x0 = miniRect.x + (min.x - map.CameraX) * sx, x1 = miniRect.x + (max.x - map.CameraX) * sx;
        float y0 = miniRect.yMax - (max.y - map.CameraY) * sy, y1 = miniRect.yMax - (min.y - map.CameraY) * sy;
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    Vector2 MiniToWorld(Vector2 point)
    {
        MapDef map = Session.Stage.Map;
        float u = Mathf.Clamp01((point.x - miniRect.x) / miniRect.width), v = Mathf.Clamp01((miniRect.yMax - point.y) / miniRect.height);
        return new Vector2(map.CameraX + u * map.CameraWidth, map.CameraY + v * map.CameraHeight);
    }

    void FillWorld(Vector2 min, Vector2 max, Color color)
    {
        Rect r = WorldToMini(min, max);
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void Dot(Vector2 world, float size, Color color)
    {
        Rect cell = WorldToMini(world, world);
        GUI.color = color;
        DrawSprite(new Rect(cell.x - size / 2f, cell.y - size / 2f, size, size), circle, color);
        GUI.color = Color.white;
    }

    void HandleMinimapInput(Event e, Vector2 virtualMouse, bool overlay)
    {
        if (overlay || worldCamera == null) { miniDragging = false; return; }
        if (e.type == EventType.MouseDown && e.button == 0 && miniRect.Contains(virtualMouse)) miniDragging = true;
        if (miniDragging && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            CameraZoom zoom = worldCamera.GetComponent<CameraZoom>();
            if (zoom != null) zoom.LookAt(MiniToWorld(virtualMouse));
            e.Use();
        }
        if (miniDragging && e.type == EventType.MouseUp && e.button == 0)
        {
            miniDragging = false;
            e.Use();
        }
    }

    // ───────── 건설 고리 ─────────

    /// <summary>이 칸에 슬롯 타워를 지을 수 있는지(재화 부족도 "자리는 맞음"으로 본다).</summary>
    bool SpotFits(TowerDef tower, int x, int y)
    {
        if (tower == null) return false;
        CommandError error = Session.CanBuild(tower, x, y);
        return error == CommandError.None || error == CommandError.NotEnoughCoin;
    }

    /// <summary>타워가 하나라도 들어갈 수 있는 빈 칸인지.</summary>
    bool IsBuildSpot(int x, int y)
    {
        if (slotTowers == null || Session.TowerAt(x, y) != null) return false;
        foreach (TowerDef tower in slotTowers)
            if (SpotFits(tower, x, y)) return true;
        return false;
    }

    bool RingStillValid() => Session.Phase != BattlePhase.Ended && IsBuildSpot(ringTile.x, ringTile.y);

    void OpenRing(Vector2Int tile)
    {
        ringOpen = true;
        ringTile = tile;
        ringOpenedAt = Time.unscaledTime;
        selectedTower = null;
    }

    Vector2 WorldToHud(Vector3 world)
    {
        Vector3 s = worldCamera.WorldToScreenPoint(world);
        return new Vector2(s.x, Screen.height - s.y) / (UIKit.Scale * HudScale);
    }

    /// <summary>고리 중심(칸 위치, 화면 밖으로 버튼이 나가지 않게 안쪽으로 당김)과 i번째 버튼 중심.</summary>
    Vector2 RingCenter()
    {
        Vector2 c = WorldToHud(new Vector3(ringTile.x, ringTile.y, 0f));
        float m = RingRadius + RingButton / 2f + 8f;
        return new Vector2(Mathf.Clamp(c.x, m, W - m), Mathf.Clamp(c.y, m, H - m));
    }

    Vector2 RingSlotCenter(Vector2 center, int i, float open)
    {
        float angle = (-90f + i * 360f / Slots.Length) * Mathf.Deg2Rad; // 12시부터 시계 방향
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (RingRadius * open);
    }

    float RingOpen(int i)
    {
        // 버튼마다 살짝 늦게, 끝에서 조금 튕기듯(동글동글 팝업)
        float t = Mathf.Clamp01((Time.unscaledTime - ringOpenedAt - i * 0.025f) / RingOpenTime);
        float back = 1.70158f;
        return 1f + (back + 1f) * Mathf.Pow(t - 1f, 3f) + back * Mathf.Pow(t - 1f, 2f); // easeOutBack
    }

    int RingButtonAt(Vector2 virtualMouse)
    {
        if (!ringOpen) return -1;
        Vector2 center = RingCenter();
        for (int i = 0; i < Slots.Length; i++)
            if ((virtualMouse - RingSlotCenter(center, i, 1f)).sqrMagnitude <= RingButton * RingButton / 4f) return i;
        return -1;
    }

    void DrawBuildRing(Vector2 virtualMouse)
    {
        Vector2 center = RingCenter();
        float full = RingRadius * 2f + RingButton;
        Ui(new Rect(center.x - full / 2f, center.y - full / 2f, full, full));
        ringHover = RingButtonAt(virtualMouse);

        Color old = GUI.color;
        // 고리 바탕(옅은 원)과 가운데 칸 표시
        float baseOpen = Mathf.Clamp01(RingOpen(0));
        float ringSize = (RingRadius * 2f + RingButton * 0.55f) * baseOpen;
        DrawSprite(new Rect(center.x - ringSize / 2f, center.y - ringSize / 2f, ringSize, ringSize), circle, new Color(0.12f, 0.08f, 0.05f, 0.45f));

        for (int i = 0; i < Slots.Length; i++)
        {
            Slot slot = Slots[i];
            TowerDef tower = slotTowers[i];
            bool fits = SpotFits(tower, ringTile.x, ringTile.y);
            bool affordable = fits && Session.Coin >= tower.BuildCost;
            bool hover = ringHover == i;

            float open = RingOpen(i);
            float d = RingButton * Mathf.Max(0f, open) * (hover && fits ? 1.08f : 1f);
            Vector2 c = RingSlotCenter(center, i, open);
            var r = new Rect(c.x - d / 2f, c.y - d / 2f, d, d);

            // 황동 테두리 나무 단추(ws_slot) 위에 타워 아이콘. 못 지으면 어둡게, 재화가 모자라면 붉게.
            if (hover && fits)
            {
                float glow = d * 1.35f;
                DrawSprite(new Rect(c.x - glow / 2f, c.y - glow / 2f, glow, glow), circle, new Color(1f, 0.75f, 0.4f, 0.3f));
            }
            GUI.color = !fits ? new Color(0.45f, 0.42f, 0.42f, 0.95f) : affordable ? Color.white : new Color(1f, 0.7f, 0.65f, 1f);
            if (skin.Has("ws_slot")) skin.Icon(r, "ws_slot");
            else DrawSprite(r, circle, new Color(0.11f, 0.09f, 0.22f, 0.97f));
            GUI.color = old;

            GUI.color = !fits ? new Color(0.6f, 0.6f, 0.7f, 0.45f) : affordable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            float icon = d * 0.6f;
            skin.Icon(new Rect(c.x - icon / 2f, c.y - icon / 2f - d * 0.08f, icon, icon), slot.Icon);
            GUI.color = old;

            if (open > 0.6f)
            {
                string cost = tower == null ? "준비 중" : fits ? tower.BuildCost.ToString() : "불가";
                Color costColor = !fits ? UISkin.Muted : affordable ? UISkin.Light : new Color(1f, 0.55f, 0.55f);
                UIKit.ShadowLabel(new Rect(c.x - d / 2f, c.y + d * 0.2f, d, 30), cost, skin.Text(tower != null && fits ? 21 : 16, costColor, TextAnchor.MiddleCenter, true));
                UIKit.ShadowLabel(new Rect(c.x - d * 0.42f - 13, c.y - d * 0.42f - 13, 26, 26), (i + 1).ToString(), skin.Text(16, UISkin.Gold, TextAnchor.MiddleCenter, true));
            }
        }
        GUI.color = old;

        if (ringHover >= 0) DrawRingTooltip(ringHover, center);
    }

    void DrawRingTooltip(int index, Vector2 center)
    {
        Slot slot = Slots[index];
        TowerDef tower = slotTowers[index];
        bool fits = SpotFits(tower, ringTile.x, ringTile.y);
        // 고리 위쪽에 띄우되, 화면 위로 넘치면 아래쪽에
        float top = center.y - RingRadius - RingButton / 2f - 118f;
        if (top < 90f) top = center.y + RingRadius + RingButton / 2f + 12f;
        var r = new Rect(Mathf.Clamp(center.x - 200f, 12f, W - 412f), top, 400, 106);
        skin.Frame(r, "ws_card");
        GUI.Label(new Rect(r.x + 24, r.y + 8, r.width - 48, 32), slot.Name, skin.Text(22, UISkin.Ink, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(r.x + 24, r.y + 38, r.width - 48, 30), slot.Description, skin.Text(16, UISkin.InkMuted, TextAnchor.MiddleLeft, false, true));
        string cost = tower == null ? "아직 만들어지지 않은 타워예요"
            : !fits ? "이 자리에는 지을 수 없어요"
            : $"설치 비용: 몽결정 {tower.BuildCost}";
        GUI.Label(new Rect(r.x + 24, r.y + 70, r.width - 48, 30), cost, skin.Text(17, UISkin.Brown, TextAnchor.MiddleLeft, true));
    }

    void BuildFromRing(int index)
    {
        Slot slot = Slots[index];
        TowerDef tower = slotTowers[index];
        if (tower == null) { ShowMessage($"{slot.Name}는 아직 준비 중이에요"); return; }
        TowerState built;
        CommandError error = Session.TryBuild(tower, ringTile.x, ringTile.y, out built);
        if (error != CommandError.None) { ShowMessage(BattleText.Error(error, tower.BuildCost)); return; }
        ringOpen = false;
    }

    /// <summary>스프라이트 한 장(아틀라스 영역 포함)을 IMGUI 사각형에 색을 곱해 그린다.</summary>
    static void DrawSprite(Rect r, Sprite sprite, Color color)
    {
        if (sprite == null) return;
        Texture2D t = sprite.texture;
        Rect tr = sprite.textureRect;
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTextureWithTexCoords(r, t, new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height));
        GUI.color = old;
    }

    void DrawTowerPanel()
    {
        float w = W;
        TowerLevelDef level = selectedTower.Level;
        bool blocks = selectedTower.Blocks;
        Rect rect = Ui(new Rect(w - PanelWidth - 24, 160, PanelWidth, blocks || level.Slows ? 440 : 370));
        skin.Frame(rect, "ws_card_info");
        float x = rect.x + 32, y = rect.y + 26, width = PanelWidth - 64;
        GUI.Label(new Rect(x, y, width, 40), $"{selectedTower.Def.Name}  {selectedTower.LevelIndex + 1}단계", skin.Text(28, UISkin.Ink, TextAnchor.MiddleLeft, true));
        y += 50;
        GUI.Label(new Rect(x, y, width, 30), $"공격력 {level.Damage:0.#}   공격속도 {BattleMath.AttackInterval(level.AttackSec, Session.Rules):0.##}초", hudInk);
        y += 34;
        GUI.Label(new Rect(x, y, width, 30), $"사거리 {level.Range:0.#}   치명 {level.CritChance * 100:0}% ×{level.CritMult:0.##}", hudInk);
        y += 34;
        if (blocks)
        {
            string push = level.Knockback > 0f ? " · 밀어내기" : "";
            GUI.Label(new Rect(x, y, width, 30), $"저지 {level.BlockCount}명 · {level.BlockSec:0.#}초{push}", hudInk);
            y += 34;
        }
        if (level.Slows)
        {
            GUI.Label(new Rect(x, y, width, 30), $"감속 {(1f - level.SlowMult) * 100f:0}% · {level.SlowSec:0.#}초", hudInk);
            y += 34;
        }
        string extra = ExtraText(level);
        if (extra.Length > 0)
        {
            GUI.Label(new Rect(x, y, width, 30), extra, hudInk);
            y += 34;
        }
        GUI.Label(new Rect(x, y, width, 30), $"투자 몽결정 {selectedTower.TotalSpent}", hudInk);
        y += 48;

        if (selectedTower.CanUpgrade)
        {
            TowerLevelDef next = selectedTower.Def.Levels[selectedTower.LevelIndex + 1];
            string nextText = blocks
                ? $"다음: 공격력 {next.Damage:0.#}\n저지 {next.BlockCount}명 · {next.BlockSec:0.#}초{(next.Knockback > level.Knockback ? " · 밀어내기" : "")}"
                : next.Slows ? $"다음: 공격력 {next.Damage:0.#}, 감속 {(1f - next.SlowMult) * 100f:0}% · {next.SlowSec:0.#}초"
                : next.Marks || next.HitCount > 1 ? $"다음: 공격력 {next.Damage:0.#}, {ExtraText(next)}"
                : $"다음: 공격력 {next.Damage:0.#}, 사거리 {next.Range:0.#}";
            float nextH = blocks ? 62f : 30f;
            GUI.Label(new Rect(x, y, width, nextH), nextText, hudInk);
            y += nextH + 12f;
            if (GUI.Button(new Rect(x, y, width, 60), $"강화 (몽결정 {level.UpgradeCost})", skin.Button(24)))
            {
                CommandError error = Session.TryUpgrade(selectedTower);
                if (error != CommandError.None) ShowMessage(BattleText.Error(error, level.UpgradeCost));
            }
        }
        else
        {
            GUI.Label(new Rect(x, y, width, 30), "최대 단계", skin.Text(22, UISkin.Brown, TextAnchor.MiddleLeft, true));
        }
    }

    void DrawMessage()
    {
        if (string.IsNullOrEmpty(message) || Time.unscaledTime > messageUntil) return;
        var rect = new Rect(0, H - 170, W, 50);
        UIKit.ShadowLabel(rect, message, skin.Text(28, UISkin.Gold, TextAnchor.MiddleCenter, true));
    }

    // ───────── 겹쳐 뜨는 화면 ─────────

    void DrawPauseMenu()
    {
        UIKit.DimScreen();
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0, 220, w, 100), "일시정지", UIKit.Heading);
        float x = (w - 420f) / 2f;
        if (GUI.Button(new Rect(x, 380, 420, 80), "계속하기", skin.Button(28))) SetPauseMenu(false);
        if (GUI.Button(new Rect(x, 480, 420, 80), "처음부터 다시", skin.Button(28))) SceneFlow.RestartStage();
        if (GUI.Button(new Rect(x, 580, 420, 80), "중도 귀환", skin.Button(28))) { SetPauseMenu(false); Session.Retreat(); }
        if (GUI.Button(new Rect(x, 680, 420, 80), "수선소로", skin.Button(28))) SceneFlow.GoToMain();
        if (DreamRun.TestMode && SceneFlow.DeveloperMode && GUI.Button(new Rect(x, 780, 420, 80), "맵 에디터로", skin.Button(28))) SceneFlow.GoToMapEditor();
    }

    void DrawResult()
    {
        UIKit.DimScreen(0.7f);
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0, 180, w, 130), BattleText.Result(Session.Result), UIKit.Title);
        GUI.Label(new Rect(0, 320, w, 40), BattleText.Reason(Session.EndReason), new GUIStyle(hudSmall) { alignment = TextAnchor.MiddleCenter });
        GUI.Label(new Rect(0, 370, w, 40),
            $"남은 꿈의 불빛 {Session.CoreHp}   ·   전투 시간 {Session.CombatTime:0}초   ·   웨이브 {Session.WaveIndex + 1}/{Session.Stage.Waves.Count}",
            new GUIStyle(hudSmall) { alignment = TextAnchor.MiddleCenter });

        float x = (w - 420f) / 2f, y = 460f;
        if (Session.Result == BattleResult.Success && SceneFlow.HasNextStage)
        {
            if (GUI.Button(new Rect(x, y, 420, 80), "다음 스테이지", skin.Button(28))) SceneFlow.StartNextStage();
            y += 100;
        }
        if (GUI.Button(new Rect(x, y, 420, 80), DreamRun.Active ? "다시 탐색부터" : "다시 도전", skin.Button(28))) SceneFlow.RestartStage();
        y += 100;
        if (GUI.Button(new Rect(x, y, 420, 80), "수선소로", skin.Button(28))) SceneFlow.GoToMain();
        y += 100;
        if (DreamRun.TestMode && SceneFlow.DeveloperMode && GUI.Button(new Rect(x, y, 420, 80), "맵 에디터로", skin.Button(28))) SceneFlow.GoToMapEditor();
    }

    void DrawDataErrors()
    {
        UIKit.DimScreen(0.85f);
        GUI.Label(new Rect(80, 80, UIKit.Width - 160, 60), "전투 데이터 오류로 시작할 수 없어요", UIKit.Heading);
        string list = controller.Content == null ? "" : string.Join("\n", controller.Content.Errors);
        GUI.Label(new Rect(80, 170, UIKit.Width - 160, UIKit.Height - 300), list, hudSmall);
        if (UIKit.DrawButton(new Rect(80, UIKit.Height - 120, 300, 70), "수선소로", UIKit.ButtonSmall)) SceneFlow.GoToMain();
    }

    // ───────── 입력 ─────────

    void HandleInput(Event e, bool overlay, Vector2 virtualMouse)
    {
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                if (Session.Phase == BattlePhase.Ended) return;
                if (ringOpen || selectedTower != null) ClearSelection();
                else SetPauseMenu(!pauseMenu);
                e.Use();
                return;
            }
            if (overlay) return;
            if (e.keyCode == KeyCode.Space) { Session.StartWaveNow(); e.Use(); }
            else if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            {
                // 숫자 키: 건설 고리가 열려 있으면 그 칸에 바로 짓는다.
                int index = e.keyCode - KeyCode.Alpha1;
                if (ringOpen && index < Slots.Length) BuildFromRing(index);
                else if (!ringOpen) ShowMessage("지을 칸을 먼저 클릭하세요");
                e.Use();
            }
            else if (Debug.isDebugBuild && e.keyCode == KeyCode.F1) { Session.DebugAddCoin(100); e.Use(); }
            else if (Debug.isDebugBuild && e.keyCode == KeyCode.F2) { Session.DebugKillAll(); e.Use(); }
            return;
        }

        // 클릭은 버튼을 뗄 때 처리한다. 누른 채 끌었으면 화면 이동(CameraZoom)이라 클릭으로 보지 않는다.
        if (overlay || e.type != EventType.MouseUp || CameraZoom.LastClickWasDrag) return;

        if (ringOpen)
        {
            int button = RingButtonAt(virtualMouse);
            if (e.button == 0 && button >= 0)
            {
                BuildFromRing(button);
                e.Use();
                return;
            }
            // 버튼 말고 다른 곳을 누르면 닫는다. 맵 위 왼쪽 클릭이면 그 자리 클릭(다른 칸·타워 선택)으로 이어서 처리.
            ringOpen = false;
            if (e.button == 1 || !hoverOnMap) { e.Use(); return; }
        }
        if (!hoverOnMap) return;

        if (e.button == 1)
        {
            ClearSelection();
            e.Use();
            return;
        }
        if (e.button != 0) return;

        TowerState tower = Session.TowerAt(hoverTile.x, hoverTile.y);
        if (tower != null) selectedTower = tower == selectedTower ? null : tower;
        else if (IsBuildSpot(hoverTile.x, hoverTile.y)) OpenRing(hoverTile);
        else selectedTower = null;
        e.Use();
    }

    void ClearSelection()
    {
        ringOpen = false;
        selectedTower = null;
    }

    void SetPauseMenu(bool open)
    {
        pauseMenu = open;
        controller.SetPaused(open);
    }

    bool IsOverUI(Vector2 virtualMouse)
    {
        foreach (Rect r in uiRects)
            if (r.Contains(virtualMouse)) return true;
        return false;
    }

    void ShowMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        message = text;
        messageUntil = Time.unscaledTime + 1.8f;
    }

    // ───────── 미리보기 ─────────

    void UpdatePreview()
    {
        if (Session == null || worldCamera == null) return;

        var screen = new Vector3(mouseScreen.x, Screen.height - mouseScreen.y, 0f);
        Vector3 world = worldCamera.ScreenToWorldPoint(screen);
        hoverTile = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));

        bool active = Session.Phase != BattlePhase.Ended;
        if (view != null && view.mapView != null) view.mapView.ShowBuildTiles(active && ringOpen);

        // 건설 고리가 열린 칸은 초록, 고리가 닫혀 있을 때 지을 수 있는 칸에 마우스를 올리면 옅게 표시
        bool hoverSpot = active && !ringOpen && hoverOnMap && IsBuildSpot(hoverTile.x, hoverTile.y);
        tilePreview.enabled = active && (ringOpen || hoverSpot);
        if (tilePreview.enabled)
        {
            Vector2Int tile = ringOpen ? ringTile : hoverTile;
            tilePreview.transform.position = new Vector3(tile.x, tile.y, 0f);
            tilePreview.transform.localScale = Vector3.one * 0.95f;
            tilePreview.color = ringOpen ? new Color(0.4f, 1f, 0.6f, 0.45f) : new Color(1f, 1f, 1f, 0.22f);
        }

        float range = 0f;
        Vector3 center = Vector3.zero;
        if (ringOpen && ringHover >= 0 && slotTowers[ringHover] != null)
        {
            range = slotTowers[ringHover].Levels[0].Range;
            center = new Vector3(ringTile.x, ringTile.y, 0f);
        }
        else if (selectedTower != null)
        {
            range = selectedTower.Level.Range;
            center = new Vector3(selectedTower.TileX, selectedTower.TileY, 0f);
        }
        rangePreview.enabled = range > 0f;
        if (range > 0f)
        {
            rangePreview.transform.position = center;
            rangePreview.transform.localScale = Vector3.one * range * 2f;
            rangePreview.color = new Color(0.5f, 0.95f, 0.9f, 0.14f);
        }
    }

    SpriteRenderer CreatePreview(string objectName, Sprite sprite, int order)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    void EnsureStyles()
    {
        if (hudSmall != null) return;
        hudSmall = new GUIStyle(UIKit.Body) { fontSize = 22, wordWrap = true };
        hudSmall.normal.textColor = UISkin.Light;
        hudInk = new GUIStyle(hudSmall);
        hudInk.normal.textColor = UISkin.Ink;
    }
}
