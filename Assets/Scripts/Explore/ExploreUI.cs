using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 꿈 탐색 화면(회색 프로토타입, IMGUI 1080p 기준). 게임플레이 기획 요약의 "탐색과 개방" 단계.
/// - 이안은 목표(꿈의 중심)에서 출발해 상하좌우로 한 칸씩 움직이며 암흑을 밝힌다. 결정 칸을 밝히면 몽결정을 얻는다.
/// - 막힌 암흑과 맵 밖으로는 못 간다. 결정·출현 지점은 암흑 속에서도 보인다.
/// - 출현 지점까지 길이 이어지면 "전투 준비"로 넘어간다(탐색 제한 없음). 몬스터는 밝힌 칸의 최단 경로로 온다.
///   몬스터 경로는 미리 보여 주지 않는다(기획: 연결 상태를 보고 판단).
/// 연출: 이안(랜턴을 든 전신 캐릭터, 4방향·걷기 2장)이 칸 사이를 통통 미끄러지듯 걷고, 랜턴 빛이 둘레 어둠을 밝힌다.
///       암흑을 밝히면 어둠이 먼지처럼 흩어져 사라지고, 은은한 빛과 꿈실 고리가 번지며 빛 알갱이가 떠오른다(화면 흔들림 없음).
///       결정이면 보라 폭발과 +값 글자. 막힌 암흑에 부딪히면 이안이 살짝 튕기고 가시 먼지가 튄다.
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

    // ───────── 연출 상태 ─────────

    class Fx
    {
        public Vector2 pos, vel;      // 칸 단위(칸 (x, y)의 가운데 = x + 0.5, y + 0.5)
        public float age, life, size0, size1, spin, angle;
        public Color color;
        public int kind;              // 0 빛 점, 1 불티, 2 고리, 3 글자, 4 어둠 먼지, 5 떠오르는 빛 알갱이(좌우로 흔들림)
        public string text;
    }

    readonly List<Fx> effects = new List<Fx>();
    Vector2 bumpDir;
    float bumpT;
    Texture2D oldFog;
    float oldFogFade;
    Vector2Int prevPlayer, facing = new Vector2Int(0, 1);
    float moveT = 1f;
    Vector2Int flashCell;
    float flashT;
    Sprite[] ian;   // 아래·위·왼쪽·오른쪽 × (서 있기, 걷기)

    void LoadIan()
    {
        ian = new Sprite[8];
        string[] dirs = { "down", "up", "left", "right" };
        for (int i = 0; i < 8; i++) ian[i] = Resources.Load<Sprite>($"Characters/Ian/ian_{dirs[i % 4]}_{i / 4}");
        if (ian[0] == null) ian = null;
    }

    void Start()
    {
        skin = new UISkin(uiSprites, uiBorders);
        LoadIan();
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
        prevPlayer = player;
        connected = map.ShortestPath() != null;
        FitView();
        ShowToast("목표(파란 칸)에서 출발해요. 출현 지점(보라 칸)까지 암흑을 밝혀 길을 이어 주세요.", 5f);
    }

    // ───────── 이동·개방 ─────────

    void Update()
    {
        UpdateEffects(Time.unscaledDeltaTime);
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
                Bump(x, y);
                return;
            }
            opened++;
            Reveal(x, y, before, value);
            if (value > 0)
            {
                DreamRun.Collected += value;
                ShowToast((before == CellType.CrystalLarge ? "큰 몽결정" : "몽결정") + $" +{value}", 1.4f);
            }
            bool wasConnected = connected;
            connected = map.ShortestPath() != null;
            if (connected && !wasConnected) ShowToast("출현 지점까지 길이 이어졌어요! 더 탐색하거나 전투를 준비하세요.", 3f);
        }
        facing = new Vector2Int(x - player.x, y - player.y);
        prevPlayer = player;
        player = new Vector2Int(x, y);
        moveT = 0f;
        KeepPlayerInView();
    }

    void OnDestroy()
    {
        if (fogTexture != null) Destroy(fogTexture);
        if (oldFog != null) Destroy(oldFog);
    }

    // ───────── 타격감 연출 ─────────

    void AddFx(int kind, Vector2 pos, Vector2 vel, float life, float size0, float size1, Color color, float spin = 0f, string text = null)
    {
        effects.Add(new Fx { kind = kind, pos = pos, vel = vel, life = life, size0 = size0, size1 = size1, color = color, spin = spin, text = text, angle = Random.value * 360f });
    }

    /// <summary>
    /// 암흑을 밝힌 순간: 칸을 덮던 어둠이 먼지처럼 사방으로 흩어지며 옅어지고, 그 자리에 은은한 빛과 꿈실 고리가 번지며
    /// 작은 불티와 빛 알갱이가 천천히 떠오른다. 결정이면 보라 폭발과 +값 글자를 더한다. 화면은 흔들지 않는다.
    /// </summary>
    void Reveal(int x, int y, CellType before, int value)
    {
        var c = new Vector2(x + 0.5f, y + 0.5f);
        var warm = new Color(1f, 0.86f, 0.55f);
        var violet = new Color(0.75f, 0.58f, 1f);
        flashCell = new Vector2Int(x, y);
        flashT = 1f;

        // 어둠 먼지: 칸 곳곳에서 바깥으로 흩어지며 커지고 옅어진다
        for (int i = 0; i < 16; i++)
        {
            Vector2 start = c + new Vector2(Random.Range(-0.45f, 0.45f), Random.Range(-0.45f, 0.45f));
            Vector2 away = (start - c).normalized + Random.insideUnitCircle * 0.5f;
            float shade = Random.Range(0.02f, 0.09f);
            AddFx(4, start, away * Random.Range(0.9f, 1.8f) + new Vector2(0f, -0.25f), Random.Range(0.6f, 1.0f),
                Random.Range(0.45f, 0.7f), Random.Range(0.9f, 1.4f), new Color(shade, shade * 0.8f, shade * 2.2f + 0.04f, 0.85f));
        }
        for (int i = 0; i < 10; i++)
            AddFx(4, c + Random.insideUnitCircle * 0.4f, Random.insideUnitCircle.normalized * Random.Range(1.6f, 2.6f), Random.Range(0.5f, 0.8f),
                0.14f, 0.05f, new Color(0.12f, 0.08f, 0.24f, 0.9f));

        // 은은한 빛과 꿈실 고리
        AddFx(0, c, Vector2.zero, 0.55f, 0.6f, 2.0f, new Color(1f, 0.9f, 0.7f, 0.45f));
        AddFx(2, c, Vector2.zero, 0.6f, 0.5f, 1.9f, new Color(warm.r, warm.g, warm.b, 0.75f), Random.Range(-90f, 90f));
        for (int i = 0; i < 6; i++)
        {
            float a = (i + Random.value * 0.6f) / 6f * Mathf.PI * 2f;
            AddFx(1, c, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.2f, 2f), Random.Range(0.35f, 0.5f), 0.2f, 0.04f,
                new Color(1f, 0.9f, 0.65f, 0.85f), Random.Range(-300f, 300f));
        }
        for (int i = 0; i < 6; i++)
            AddFx(5, c + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.1f, 0.4f)), new Vector2(0f, -Random.Range(0.35f, 0.7f)),
                Random.Range(1.2f, 1.9f), 0.13f, 0.05f, i % 2 == 0 ? new Color(1f, 0.88f, 0.6f, 0.95f) : new Color(0.85f, 0.8f, 1f, 0.95f));

        if (value <= 0) return;
        bool big = before == CellType.CrystalLarge;
        AddFx(2, c, Vector2.zero, 0.65f, 0.4f, big ? 3.2f : 2.5f, violet, Random.Range(-140f, 140f));
        AddFx(0, c, Vector2.zero, 0.45f, 0.8f, big ? 3f : 2.2f, new Color(0.8f, 0.65f, 1f, 0.7f));
        int n = big ? 18 : 12;
        for (int i = 0; i < n; i++)
        {
            float a = (i + Random.value) / n * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Color col = i % 3 == 0 ? Color.white : i % 3 == 1 ? violet : new Color(0.55f, 0.9f, 1f);
            AddFx(1, c, dir * Random.Range(2f, big ? 4.5f : 3.4f), Random.Range(0.45f, 0.75f), big ? 0.38f : 0.3f, 0.06f, col, Random.Range(-500f, 500f));
        }
        if (big) AddFx(2, c, Vector2.zero, 0.8f, 0.2f, 4f, new Color(1f, 0.9f, 0.6f, 0.8f), Random.Range(-140f, 140f));
        AddFx(3, c + new Vector2(0f, -0.4f), new Vector2(0f, -1.1f), 1.1f, 1f, 1f, big ? new Color(1f, 0.9f, 0.55f) : new Color(0.85f, 0.75f, 1f), 0f, $"+{value}");
    }

    /// <summary>막힌 암흑에 부딪힘: 이안이 벽 쪽으로 살짝 튕겼다 돌아오고 가시 먼지가 튄다.</summary>
    void Bump(int x, int y)
    {
        bumpDir = new Vector2(x - player.x, y - player.y);
        bumpT = 1f;
        facing = new Vector2Int(x - player.x, y - player.y);
        var edge = new Vector2((player.x + x) / 2f + 0.5f, (player.y + y) / 2f + 0.5f);
        for (int i = 0; i < 6; i++)
            AddFx(1, edge, Random.insideUnitCircle * 1.4f - bumpDir * 0.6f, Random.Range(0.25f, 0.4f), 0.18f, 0.05f, new Color(0.7f, 0.45f, 0.65f), Random.Range(-300f, 300f));
        AddFx(4, edge, -bumpDir * 0.4f, 0.5f, 0.3f, 0.7f, new Color(0.08f, 0.06f, 0.16f, 0.6f));
    }

    void UpdateEffects(float dt)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            Fx f = effects[i];
            f.age += dt;
            f.pos += f.vel * dt;
            if (f.kind == 4) f.vel *= Mathf.Exp(-2.2f * dt);
            else if (f.kind != 3 && f.kind != 5) f.vel *= Mathf.Exp(-3.5f * dt);
            f.angle += f.spin * dt;
            if (f.age >= f.life) effects.RemoveAt(i);
        }
        if (bumpT > 0f) bumpT -= dt / 0.22f;
        if (oldFogFade > 0f)
        {
            oldFogFade -= dt / 0.35f;
            if (oldFogFade <= 0f && oldFog != null) { Destroy(oldFog); oldFog = null; }
        }
        if (flashT > 0f) flashT -= dt / 0.3f;
        if (moveT < 1f) moveT = Mathf.Min(1f, moveT + dt / 0.14f);
    }

    Vector2 CellToScreen(Vector2 p, Vector2 shift) => origin + shift + p * cell;

    void DrawEffects(Vector2 shift)
    {
        if (theme == null) return;
        foreach (Fx f in effects)
        {
            float t = f.age / f.life;
            float size = Mathf.Lerp(f.size0, f.size1, 1f - (1f - t) * (1f - t)) * cell;
            Color c = f.color;
            c.a *= 1f - t * t;
            Vector2 p = CellToScreen(f.pos, shift);
            if (f.kind == 5) p.x += Mathf.Sin(f.age * 5f + f.angle) * cell * 0.08f;
            if (f.kind == 4) c.a = f.color.a * (1f - t) * Mathf.Min(1f, t * 8f + 0.6f);
            if (f.kind == 3)
            {
                GUIStyle style = skin.Text(Mathf.Clamp(Mathf.RoundToInt(cell * 0.55f), 14, 40), c, TextAnchor.MiddleCenter, true);
                UIKit.ShadowLabel(new Rect(p.x - cell * 1.5f, p.y - cell * 0.4f, cell * 3f, cell * 0.8f), f.text, style);
                continue;
            }
            Sprite sprite = f.kind == 1 ? theme.FxSpark : f.kind == 2 ? theme.FxRing : theme.FxGlow;
            var r = new Rect(p.x - size / 2f, p.y - size / 2f, size, size);
            if (f.kind == 1 || f.kind == 2)
            {
                Matrix4x4 m = GUI.matrix;
                GUIUtility.RotateAroundPivot(f.angle, p);
                DrawSprite(r, sprite, c);
                GUI.matrix = m;
            }
            else DrawSprite(r, sprite, c);
        }
    }

    /// <summary>이안: 칸 사이를 통통 뛰듯 이동(0.14초), 바라보는 방향 그림, 걷는 중엔 걷기 그림, 서 있으면 숨쉬기. 랜턴 빛이 둘레를 밝힌다.</summary>
    void DrawIan(Vector2 shift, float px)
    {
        float e = 1f - (1f - moveT) * (1f - moveT);
        Vector2 pos = Vector2.Lerp(new Vector2(prevPlayer.x + 0.5f, prevPlayer.y + 0.5f), new Vector2(player.x + 0.5f, player.y + 0.5f), e);
        float hop = Mathf.Sin(moveT * Mathf.PI) * 0.16f;
        if (bumpT > 0f) pos += bumpDir * 0.14f * Mathf.Sin((1f - bumpT) * Mathf.PI);
        Vector2 p = CellToScreen(pos, shift);

        // 랜턴 빛(어둠 위에 따뜻하게 번짐, 살짝 일렁임)
        if (theme != null && theme.FxGlow != null)
        {
            float flicker = 0.9f + 0.1f * Mathf.PerlinNoise(Time.time * 3f, 0.5f);
            float big = cell * 3.6f * flicker, small = cell * 1.6f;
            DrawSprite(new Rect(p.x - big / 2f, p.y - big / 2f, big, big), theme.FxGlow, new Color(1f, 0.78f, 0.42f, 0.32f));
            DrawSprite(new Rect(p.x - small / 2f, p.y - small / 2f, small, small), theme.FxGlow, new Color(1f, 0.85f, 0.55f, 0.35f));
        }

        if (ian == null)
        {
            Rect fallback = CellRect(player.x, player.y);
            fallback.position += shift;
            if (skin.Has("mock_ian")) skin.Icon(Shrink(fallback, -0.15f), "mock_ian");
            else Fill(Shrink(fallback, 0.2f), ColPlayer);
            Outline(Shrink(fallback, 0.04f), ColPlayer, 2f * px);
            return;
        }
        int dir = facing.y > 0 ? 0 : facing.y < 0 ? 1 : facing.x < 0 ? 2 : 3;
        bool walking = moveT < 1f && moveT > 0.15f && moveT < 0.85f;
        Sprite sprite = ian[dir + (walking ? 4 : 0)] ?? ian[dir];
        float breathe = moveT >= 1f ? 1f + 0.02f * Mathf.Sin(Time.time * 2.4f) : 1f;
        float h = cell * 1.45f * breathe, w = h * sprite.rect.width / sprite.rect.height;
        float feet = p.y + cell * 0.42f - hop * cell;
        // 발밑 그림자
        if (theme != null && theme.FxGlow != null)
            DrawSprite(new Rect(p.x - cell * 0.35f, p.y + cell * 0.3f, cell * 0.7f, cell * 0.22f), theme.FxGlow, new Color(0f, 0f, 0f, 0.45f));
        DrawSprite(new Rect(p.x - w / 2f, feet - h, w, h), sprite, Color.white);
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

    // ───────── 테마(달빛 정원) ─────────

    MapTheme theme;
    Texture2D fogTexture;
    int fogOpenCount = -1;

    int CountOpen()
    {
        int n = 0;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.IsOpen(x, y)) n++;
        return n;
    }

    /// <summary>스프라이트를 IMGUI 사각형에(아틀라스 영역 포함).</summary>
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

    /// <summary>회전(도)·좌우 뒤집기를 적용해 그린다(가운데 기준).</summary>
    static void DrawRotated(Rect r, Sprite sprite, Color color, float angle, bool flip)
    {
        if (sprite == null) return;
        Texture2D t = sprite.texture;
        Rect tr = sprite.textureRect;
        var uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
        if (flip) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
        Matrix4x4 m = GUI.matrix;
        if (angle != 0f) GUIUtility.RotateAroundPivot(angle, r.center);
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTextureWithTexCoords(r, t, uv);
        GUI.color = old;
        GUI.matrix = m;
    }

    /// <summary>바닥에 서 있는 오브젝트: 폭 width칸, 아래쪽이 칸 바닥 근처에 닿게.</summary>
    void DrawProp(Rect cellRect, Sprite sprite, float width, float bob = 0f) => DrawProp(cellRect, sprite, width, Color.white, bob);

    void DrawProp(Rect cellRect, Sprite sprite, float width, Color tint, float bob = 0f)
    {
        if (sprite == null || tint.a <= 0f) return;
        float w = cell * width, h = w * sprite.rect.height / sprite.rect.width;
        var r = new Rect(cellRect.center.x - w / 2f, cellRect.yMax - cell * 0.08f - h + bob, w, h);
        DrawSprite(r, sprite, tint);
    }

    /// <summary>
    /// 테마 그림으로 그리기(사용자 샘플 그림 기준): 맵 바깥 어두운 꽃밭 → 바닥(밝힌 칸 돌길, 안 연 칸 잔디, 막힌 암흑 꽃밭)
    /// → 꽃밭 가장자리 덤불·돌길 경계 꽃 띠 → 어둠 안개(밝힌 칸만 걷힘, 경계는 부드럽게) → 정원 소품·결정·출현 지점·꿈의 중심.
    /// 몬스터 경로는 탐색 중에는 보여 주지 않는다(기획).
    /// </summary>
    void DrawThemedCells(Vector2 shift, Rect area)
    {
        Color old = GUI.color;
        // 맵 바깥(화면 전체)에 어두운 꽃밭. 칸 좌표와 이어지게 텍스처 좌표를 맞춘다(꽃밭 한 장 = 8칸).
        {
            Vector2 o0 = origin + shift;
            float cx0 = -o0.x / cell, cx1 = (area.width - o0.x) / cell;
            float cy0 = -o0.y / cell, cy1 = (area.height - o0.y) / cell;
            float n = MapTheme.Tiles(MapTheme.Ground.Flower);
            GUI.color = MapTheme.Shade(MapTheme.Ground.Flower) * new Color(0.3f, 0.3f, 0.38f);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, area.width, area.height), theme.Flower,
                new Rect(cx0 / n, (map.Height - cy1) / n, (cx1 - cx0) / n, (cy1 - cy0) / n));
        }
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                Rect r = CellRect(x, y);
                r.position += shift;
                if (r.xMax < 0 || r.yMax < 0 || r.x > area.width || r.y > area.height) continue;
                MapTheme.Ground kind = MapTheme.GroundAt(map, x, y);
                GUI.color = MapTheme.Shade(kind);
                GUI.DrawTextureWithTexCoords(new Rect(r.x - 0.5f, r.y - 0.5f, r.width + 1f, r.height + 1f), theme.Texture(kind), MapTheme.GroundUV(kind, x, map.Height - 1 - y));
            }
        GUI.color = old;

        // 꽃밭 가장자리 덤불, 돌길과 잔디 사이 꽃 경계 띠
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                Rect r = CellRect(x, y);
                r.position += shift;
                if (r.xMax < -cell || r.yMax < -cell || r.x > area.width + cell || r.y > area.height + cell) continue;
                Sprite tuft;
                float size, angle;
                Vector2 offset;
                bool flip;
                if (theme.Tuft(map, x, y, out tuft, out size, out offset, out angle, out flip))
                {
                    Vector2 c = r.center + offset * cell;
                    float s = size * cell;
                    DrawRotated(new Rect(c.x - s / 2f, c.y - s / 2f, s, s), tuft, MapTheme.Shade(MapTheme.Ground.Flower), angle, flip);
                }
                foreach (Vector2Int side in MapTheme.Sides)
                {
                    Sprite border = theme.BorderFor(map, x, y, side);
                    if (border == null) continue;
                    float w = cell * 1.06f, h = w * border.rect.height / border.rect.width;
                    Vector2 c = r.center + new Vector2(side.x, side.y) * (cell * 0.5f);
                    DrawRotated(new Rect(c.x - w / 2f, c.y - h / 2f, w, h), border, Color.white, side.x != 0 ? 90f : 0f, false);
                }
            }

        int open = CountOpen();
        if (open != fogOpenCount || fogTexture == null)
        {
            // 예전 안개는 0.35초 동안 서서히 사라지게 남겨 둔다(안개가 걷히는 느낌)
            if (fogTexture != null && fogOpenCount >= 0)
            {
                if (oldFog != null) Destroy(oldFog);
                oldFog = fogTexture;
                oldFogFade = 1f;
            }
            fogTexture = MapTheme.BuildFog(map, MapTheme.ExploreFog);
            fogOpenCount = open;
        }
        Vector2 o = origin + shift;
        var fogRect = new Rect(o.x, o.y, cell * map.Width, cell * map.Height);
        Color before = GUI.color;
        float breath = 0.94f + 0.06f * Mathf.Sin(Time.time * 0.8f);
        GUI.color = new Color(1f, 1f, 1f, breath);
        GUI.DrawTexture(fogRect, fogTexture);
        if (oldFog != null && oldFogFade > 0f)
        {
            GUI.color = new Color(1f, 1f, 1f, breath * oldFogFade * oldFogFade);
            GUI.DrawTexture(fogRect, oldFog);
        }
        GUI.color = before;

        // 방금 밝힌 칸 번쩍임
        if (flashT > 0f)
        {
            Rect fr = CellRect(flashCell.x, flashCell.y);
            fr.position += shift;
            Fill(fr, new Color(1f, 0.93f, 0.78f, 0.28f * flashT * flashT));
        }

        float t = Time.time;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                Rect r = CellRect(x, y);
                r.position += shift;
                if (r.xMax < -cell || r.yMax < -cell || r.x > area.width + cell || r.y > area.height + cell) continue;
                CellType type = map.Get(x, y);
                Sprite garden;
                float width;
                if (theme.GardenProp(map, x, y, out garden, out width))
                {
                    Color tint = MapTheme.WallTint(map, x, y);
                    if (tint.a <= 0f) tint = new Color(0.55f, 0.55f, 0.7f);
                    // 밝힌 칸 곁의 가로등은 불을 켠다
                    if (tint.r > 0.9f && garden.name.Contains("lantern") && theme.FxGlow != null)
                    {
                        float g = cell * (1.5f + 0.12f * Mathf.PerlinNoise(t * 2.5f, x * 0.7f + y));
                        DrawSprite(new Rect(r.center.x - g / 2f, r.y - g * 0.35f, g, g), theme.FxGlow, new Color(1f, 0.75f, 0.4f, 0.38f));
                    }
                    DrawProp(r, garden, width, tint);
                }
                else if (type == CellType.CrystalSmall) DrawProp(r, theme.CrystalSmall, 0.6f, cell * 0.04f * Mathf.Sin(t * 2f + x + y));
                else if (type == CellType.CrystalLarge) DrawProp(r, theme.CrystalLarge, 0.85f, cell * 0.04f * Mathf.Sin(t * 2f + x + y));
                if (map.IsSpawn(x, y)) DrawProp(r, theme.SpawnRift, 1.1f);
                if (map.IsGoal(x, y)) DrawProp(r, theme.CoreFlower, 0.85f);
            }
    }

    void DrawMap()
    {
        Rect area = MapArea;
        GUI.BeginClip(area);
        Vector2 shift = -area.position;
        float px = 1f / Mathf.Max(0.01f, UIKit.Scale);
        if (theme == null) theme = MapTheme.Load();
        bool themed = theme.Valid;
        if (themed) DrawThemedCells(shift, area);

        for (int y = 0; y < map.Height && !themed; y++)
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

        if (cell >= 8f && !themed)
        {
            Vector2 o = origin + shift;
            for (int x = 1; x < map.Width; x++) Fill(new Rect(o.x + x * cell, o.y, px, cell * map.Height), ColLine);
            for (int y = 1; y < map.Height; y++) Fill(new Rect(o.x, o.y + y * cell, cell * map.Width, px), ColLine);
        }

        DrawIan(shift, px);
        DrawEffects(shift);
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
