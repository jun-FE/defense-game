using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 칸 맵 테마(그림 묶음). Assets/Resources/Themes/{이름}/ 의 그림을 불러 전투 맵(MapView)과 탐색 화면(ExploreUI)이 같이 쓴다.
/// 기준 그림: ArtSource/Maps/MoonGarden/reference/sample_moon_garden.png (밝은 돌길 + 잔디 + 꽃밭 테두리).
/// - 바닥 3종(이음새 없이 반복): 밝힌 칸 = 돌길(ground_stone, 4×4칸), 안 연 칸 = 잔디(ground_grass, 4×4칸),
///   막힌 암흑 = 꽃밭(ground_flower, 8×8칸). 맵 바깥도 어두운 꽃밭이 이어진다.
/// - 꽃밭 가장자리: 들쭉날쭉한 꽃덤불 조각(flower_tuft_*)으로 칸 경계를 덮고, 세 칸에 하나쯤 정원 소품(garden_*: 가로등·실패·화분…).
/// - 돌길과 잔디 사이: 꽃·황동 기둥·금사슬 경계 띠(border_*), 한 조각 = 칸 한 변.
/// - 작은·큰 결정, 출현 지점, 꿈의 중심: 살짝 앞에서 본 오브젝트(아래쪽이 칸 바닥).
/// - path_thread: 몬스터 경로를 잇는 빛나는 꿈실 띠, build_mark: 타워를 지을 수 있는 칸의 바느질 점선.
/// - 어둠(안 연 칸)은 그림이 아니라 BuildFog로 만든 부드러운 밤 안개(전투는 옅게, 탐색은 짙게).
/// 새 테마는 같은 이름의 그림을 다른 폴더에 넣고 Load("폴더 이름")으로 바꾸면 된다(Tools/Art/build_moon_garden.py 참고).
/// </summary>
public class MapTheme
{
    public const int FogPixelsPerCell = 8;
    public const float BattleFog = 0.34f;    // 전투: 안 연 칸도 잔디가 보이는 옅은 밤 안개
    public const float ExploreFog = 0.94f;   // 탐색: 짙은 어둠(정원이 아주 희미하게 비침)
    /// <summary>맵 바깥·화면 가장자리 어둠 색(밤 정원). 맵 가장자리 그라데이션·화면 테두리가 이 색으로 녹아든다.</summary>
    public static readonly Color Night = new Color(0.035f, 0.04f, 0.09f);
    /// <summary>어둠 안개 색(BuildFog). 탐색 화면은 맵 바깥에도 이 색을 같은 진하기로 덮어 어둠이 맵 경계 없이 이어진다.</summary>
    public static readonly Color FogColor = new Color(10f / 255f, 12f / 255f, 34f / 255f);

    public enum Ground { Stone, Grass, Flower }

    public Texture2D Stone, Grass, Flower;
    public Sprite[] Tufts { get; private set; }
    public Sprite[] Borders { get; private set; }
    public Sprite[] BordersV { get; private set; }   // 세로 변용(미리 90° 돌린 그림)
    public Sprite[] FxRings { get; private set; }
    public Sprite[] Garden { get; private set; }
    public float[] GardenWidth { get; private set; }
    public Sprite CrystalSmall, CrystalLarge, SpawnRift, CoreFlower, PathThread, BuildMark, FxGlow, FxSpark;

    readonly Dictionary<int, Sprite> groundSprites = new Dictionary<int, Sprite>();
    static readonly Dictionary<string, MapTheme> cache = new Dictionary<string, MapTheme>();
    static readonly string[] GardenNames = { "garden_lantern_post", "garden_hydrangea", "garden_spools", "garden_urn", "garden_lilies", "garden_lantern", "garden_pillar", "garden_button", "garden_chest" };
    static readonly float[] GardenWidths = { 0.55f, 0.95f, 0.85f, 0.75f, 0.85f, 0.55f, 0.6f, 0.8f, 0.85f };

    public bool Valid => Stone != null && Grass != null && Flower != null;

    public static MapTheme Load(string name = "MoonGarden")
    {
        MapTheme theme;
        if (cache.TryGetValue(name, out theme)) return theme;
        string dir = "Themes/" + name + "/";
        theme = new MapTheme
        {
            Stone = Resources.Load<Texture2D>(dir + "ground_stone"),
            Grass = Resources.Load<Texture2D>(dir + "ground_grass"),
            Flower = Resources.Load<Texture2D>(dir + "ground_flower"),
            CrystalSmall = Resources.Load<Sprite>(dir + "crystal_small"),
            CrystalLarge = Resources.Load<Sprite>(dir + "crystal_large"),
            SpawnRift = Resources.Load<Sprite>(dir + "spawn_rift"),
            CoreFlower = Resources.Load<Sprite>(dir + "core_flower"),
            PathThread = Resources.Load<Sprite>(dir + "path_thread"),
            BuildMark = Resources.Load<Sprite>(dir + "build_mark"),
            FxGlow = Resources.Load<Sprite>(dir + "fx_glow"),
            FxSpark = Resources.Load<Sprite>(dir + "fx_spark"),
        };
        theme.Tufts = LoadAll(dir, "flower_tuft_", 4);
        theme.Borders = LoadAll(dir, "border_", 4);
        theme.BordersV = LoadAll(dir, "border_v_", 4);
        theme.FxRings = LoadAll(dir, "fx_ring_", 3);
        // 바닥은 맵 바깥에 반복해 깔기 때문에 Repeat여야 한다(가져오기 설정이 늦게 적용돼도 줄무늬가 생기지 않게 여기서도 맞춘다).
        foreach (Texture2D t in new[] { theme.Stone, theme.Grass, theme.Flower })
            if (t != null) t.wrapMode = TextureWrapMode.Repeat;
        var garden = new List<Sprite>();
        var widths = new List<float>();
        for (int i = 0; i < GardenNames.Length; i++)
        {
            Sprite s = Resources.Load<Sprite>(dir + GardenNames[i]);
            if (s == null) continue;
            garden.Add(s);
            widths.Add(GardenWidths[i]);
        }
        theme.Garden = garden.ToArray();
        theme.GardenWidth = widths.ToArray();
        cache[name] = theme;
        return theme;
    }

    static Sprite[] LoadAll(string dir, string prefix, int max)
    {
        var list = new List<Sprite>();
        for (int i = 0; i < max; i++)
        {
            Sprite s = Resources.Load<Sprite>(dir + prefix + i);
            if (s != null) list.Add(s);
        }
        return list.ToArray();
    }

    // ───────── 바닥 ─────────

    /// <summary>칸의 바닥 종류: 막힌 암흑 = 꽃밭, 밝힌 칸 = 돌길, 그 밖(안 연 암흑·결정 칸) = 잔디.</summary>
    public static Ground GroundAt(GridMap g, int x, int y)
    {
        if (!g.InBounds(x, y) || g.Get(x, y) == CellType.Wall) return Ground.Flower;
        return g.IsOpen(x, y) ? Ground.Stone : Ground.Grass;
    }

    public Texture2D Texture(Ground kind) => kind == Ground.Stone ? Stone : kind == Ground.Grass ? Grass : Flower;
    public static int Tiles(Ground kind) => kind == Ground.Flower ? 8 : 4;

    /// <summary>바닥 밝기: 돌길은 살짝 푸르게, 꽃밭은 어둡게(길·잔디가 눈에 들어오게).</summary>
    public static Color Shade(Ground kind) => kind == Ground.Stone ? new Color(0.95f, 0.95f, 1f)
        : kind == Ground.Grass ? new Color(1f, 1.05f, 1f) : new Color(0.78f, 0.78f, 0.86f);

    /// <summary>바닥 조각(1칸 = 1유닛). x는 오른쪽, y는 위쪽으로 늘어나는 칸 번호(월드 좌표). Tiles칸마다 같은 그림이 반복된다.</summary>
    public Sprite GroundSprite(Ground kind, int x, int y)
    {
        int n = Tiles(kind);
        int gx = Mod(x, n), gy = Mod(y, n);
        int key = ((int)kind * 64 + gy) * 64 + gx;
        Sprite sprite;
        if (groundSprites.TryGetValue(key, out sprite)) return sprite;
        Texture2D texture = Texture(kind);
        float cell = texture.width / (float)n;
        sprite = Sprite.Create(texture, new Rect(gx * cell, gy * cell, cell, cell), new Vector2(0.5f, 0.5f), cell);
        groundSprites[key] = sprite;
        return sprite;
    }

    /// <summary>IMGUI용 바닥 텍스처 좌표(0~1). y는 GroundSprite와 같이 위쪽으로 늘어나는 번호(칸 맵 행 → Height - 1 - 행).</summary>
    public static Rect GroundUV(Ground kind, int x, int y)
    {
        int n = Tiles(kind);
        float k = 1f / n;
        return new Rect(Mod(x, n) * k, Mod(y, n) * k, k, k);
    }

    // ───────── 꽃밭 가장자리·경계 띠·소품 ─────────

    public static int Hash(int x, int y) => (x * 73856093 ^ y * 19349663) & 0x7fffffff;

    static bool IsWall(GridMap g, int x, int y) => !g.InBounds(x, y) || g.Get(x, y) == CellType.Wall;

    /// <summary>꽃밭 칸이 잔디·돌길과 맞닿아 있는가(대각선 포함).</summary>
    public static bool IsEdgeWall(GridMap g, int x, int y)
    {
        if (!IsWall(g, x, y)) return false;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dy != 0) && !IsWall(g, x + dx, y + dy)) return true;
        return false;
    }

    /// <summary>
    /// 꽃밭 가장자리 덤불 조각: 칸보다 조금 크게(1.3~1.5칸) 그려 칸 경계를 덮는다. 칸마다 고정된 그림·크기·위치 흔들림·회전(90° 단위)·좌우 뒤집기.
    /// offset은 칸 단위(아래쪽이 +y = 칸 맵 행 방향).
    /// </summary>
    public bool Tuft(GridMap g, int x, int y, out Sprite sprite, out float size, out Vector2 offset, out float angle, out bool flip)
    {
        sprite = null; size = 0f; offset = Vector2.zero; angle = 0f; flip = false;
        if (Tufts.Length == 0 || !IsEdgeWall(g, x, y)) return false;
        int k = Hash(x, y);
        sprite = Tufts[k % Tufts.Length];
        if (TouchesOpen(g, x, y))
        {
            // 돌길(밝힌 칸) 옆: 길 위로 넘치지 않게 칸 크기 그대로(길은 깔끔하게)
            size = 1.02f;
            offset = Vector2.zero;
        }
        else
        {
            size = 1.3f + ((k >> 4) % 5) * 0.05f;
            offset = new Vector2(((k >> 10) % 5 - 2) * 0.05f, ((k >> 13) % 5 - 2) * 0.05f);
        }
        angle = ((k >> 16) % 4) * 90f;
        flip = ((k >> 8) % 2) == 1;
        return true;
    }

    static bool TouchesOpen(GridMap g, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (g.InBounds(x + dx, y + dy) && g.IsOpen(x + dx, y + dy)) return true;
        return false;
    }

    /// <summary>
    /// 정원 소품: 꽃밭 안쪽(둘레 8칸이 모두 꽃밭)에만, 네 칸 중 하나쯤. 길·타워 자리 옆에는 두지 않는다
    /// (몬스터 길이나 타워와 헷갈리지 않게). width는 칸 단위 폭.
    /// </summary>
    public bool GardenProp(GridMap g, int x, int y, out Sprite sprite, out float width)
    {
        sprite = null; width = 0f;
        if (Garden.Length == 0 || !IsWall(g, x, y) || IsEdgeWall(g, x, y) || Hash(x, y) % 4 != 0) return false;
        // 위쪽 칸(그림이 위로 솟음)도 꽃밭 안쪽이어야 한다
        if (!IsWall(g, x, y - 1) || IsEdgeWall(g, x, y - 1)) return false;
        int k = Hash(y + 7, x + 3) % Garden.Length;
        sprite = Garden[k];
        width = GardenWidth[k];
        return true;
    }

    /// <summary>
    /// 꽃밭 덤불 조각을 IMGUI로 그릴 때 쓰는 뒤집기(회전 대신). 탐색 화면은 확대 배율이 걸린 IMGUI라 회전 그리기가 어긋나므로
    /// angle(0/90/180/270)을 좌우·상하 뒤집기 조합으로 바꿔 모양이 칸마다 다르게 보이게 한다.
    /// </summary>
    public static void FlipsFor(float angle, bool flip, out bool flipX, out bool flipY)
    {
        int q = Mathf.RoundToInt(angle / 90f) & 3;
        flipX = flip ^ (q == 1 || q == 2);
        flipY = q >= 2;
    }

    /// <summary>
    /// 돌길(밝힌 칸)과 잔디 사이 경계 띠. 밝힌 칸 (x, y)의 네 변 중 잔디와 맞닿은 변마다 한 조각.
    /// dir: (0,-1)=위, (1,0)=오른쪽, (0,1)=아래, (-1,0)=왼쪽(칸 맵 행 방향). 꽃밭과 맞닿은 변은 덤불이 덮으므로 없다.
    /// </summary>
    public Sprite BorderFor(GridMap g, int x, int y, Vector2Int dir)
    {
        if (Borders.Length == 0 || !g.IsOpen(x, y)) return null;
        int nx = x + dir.x, ny = y + dir.y;
        if (IsWall(g, nx, ny) || g.IsOpen(nx, ny)) return null;
        int k = Hash(x * 2 + dir.x, y * 2 + dir.y);
        if (dir.x != 0 && BordersV.Length > 0) return BordersV[k % BordersV.Length];
        return Borders[k % Borders.Length];
    }

    // ───────── 맵 가장자리·화면 테두리 ─────────

    static Texture2D fadeH, fadeV, vignette;

    /// <summary>가로 그라데이션(왼쪽 불투명 → 오른쪽 투명).</summary>
    public static Texture2D FadeH => fadeH != null ? fadeH : (fadeH = MakeFade(true));
    /// <summary>세로 그라데이션(위쪽 불투명 → 아래쪽 투명).</summary>
    public static Texture2D FadeV => fadeV != null ? fadeV : (fadeV = MakeFade(false));

    static Texture2D MakeFade(bool horizontal)
    {
        const int n = 64;
        var t = new Texture2D(horizontal ? n : 1, horizontal ? 1 : n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[n];
        for (int i = 0; i < n; i++)
        {
            float a = 1f - i / (n - 1f);
            a = a * a * (3f - 2f * a);
            // 텍스처 v는 아래→위. 세로는 위쪽(v=1)이 불투명이 되게 뒤집는다.
            px[horizontal ? i : n - 1 - i] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    /// <summary>화면 가장자리를 둥글게 어둡게 하는 비네트(가운데 투명).</summary>
    public static Texture2D Vignette
    {
        get
        {
            if (vignette != null) return vignette;
            const int n = 128;
            vignette = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    float a = Mathf.Clamp01((d - 0.62f) / 0.6f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 235f));
                }
            vignette.SetPixels32(px);
            vignette.Apply();
            return vignette;
        }
    }

    /// <summary>
    /// 화면 테두리 레이어(IMGUI): 가장자리 비네트 + 위·아래 HUD 띠 뒤를 밤색으로 덮는 그라데이션.
    /// 맵·배경이 HUD 아래로 이어지되, 글자 뒤는 어둡게 가라앉아 깔끔해 보이게 한다. top·bottom은 그라데이션 높이(가상 좌표).
    /// </summary>
    public static void DrawScreenFrame(Rect screen, float top, float bottom)
    {
        Color old = GUI.color;
        GUI.color = Night;
        GUI.DrawTexture(screen, Vignette);
        if (top > 0f)
        {
            GUI.color = new Color(Night.r, Night.g, Night.b, 0.92f);
            GUI.DrawTexture(new Rect(screen.x, screen.y, screen.width, top), FadeV);
        }
        if (bottom > 0f)
        {
            GUI.color = new Color(Night.r, Night.g, Night.b, 0.92f);
            GUI.DrawTextureWithTexCoords(new Rect(screen.x, screen.yMax - bottom, screen.width, bottom), FadeV, new Rect(0f, 1f, 1f, -1f));
        }
        GUI.color = old;
    }

    /// <summary>
    /// 맵 가장자리를 바깥 어둠 쪽으로 부드럽게 녹이는 테두리(IMGUI). mapRect = 맵 전체 화면 사각형, width = 그라데이션 폭(안쪽으로).
    /// </summary>
    public static void DrawMapEdge(Rect mapRect, float width, float alpha)
    {
        Color old = GUI.color;
        GUI.color = new Color(Night.r, Night.g, Night.b, alpha);
        GUI.DrawTexture(new Rect(mapRect.x, mapRect.y, mapRect.width, width), FadeV);
        GUI.DrawTextureWithTexCoords(new Rect(mapRect.x, mapRect.yMax - width, mapRect.width, width), FadeV, new Rect(0f, 1f, 1f, -1f));
        GUI.DrawTexture(new Rect(mapRect.x, mapRect.y, width, mapRect.height), FadeH);
        GUI.DrawTextureWithTexCoords(new Rect(mapRect.xMax - width, mapRect.y, width, mapRect.height), FadeH, new Rect(1f, 0f, -1f, 1f));
        GUI.color = old;
    }

    public static readonly Vector2Int[] Sides = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

    /// <summary>
    /// 꽃밭(막힌 암흑) 위 정원 소품의 밝기(탐색 화면). 밝힌 칸 바로 옆은 환하게, 어둠 속은 어둡게.
    /// 꽃밭으로 둘러싸인 안쪽 칸이면 알파 0.
    /// </summary>
    public static Color WallTint(GridMap g, int x, int y)
    {
        bool edge = false, lit = false;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (!g.InBounds(nx, ny)) continue;
                if (g.Get(nx, ny) != CellType.Wall) edge = true;
                if (g.IsOpen(nx, ny)) lit = true;
            }
        if (!edge) return new Color(1f, 1f, 1f, 0f);
        return lit ? Color.white : new Color(0.55f, 0.55f, 0.7f, 1f);
    }

    /// <summary>
    /// 어둠 안개 텍스처: 아직 밝히지 않은 칸은 밤빛 남색(진하기 strength, 0~1), 밝힌 칸은 투명. 경계는 흐리게 번져 랜턴 빛이 스민 것처럼.
    /// 텍스처 아래쪽 줄 = 월드 y 0(맨 아래 칸). 칸 (x, y)의 월드 y는 Height - 1 - y.
    /// </summary>
    public static Texture2D BuildFog(GridMap grid, float strength, Texture2D reuse = null)
    {
        int s = FogPixelsPerCell, w = grid.Width * s, h = grid.Height * s;
        var dark = new float[w * h];
        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                float v = grid.IsOpen(x, y) ? 0f : 1f;
                int wy = grid.Height - 1 - y;
                for (int py = 0; py < s; py++)
                    for (int px = 0; px < s; px++)
                        dark[(wy * s + py) * w + x * s + px] = v;
            }
        // 가로·세로 상자 흐림 두 번 → 경계가 반 칸쯤 부드럽게 번진다
        dark = Blur(dark, w, h, s / 2);
        dark = Blur(dark, w, h, s / 2);

        Texture2D texture = reuse != null && reuse.width == w && reuse.height == h
            ? reuse
            : new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++)
        {
            float a = Mathf.Clamp01(dark[i]);
            a = a * a * (3f - 2f * a);
            pixels[i] = new Color32(10, 12, 34, (byte)(a * strength * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static float[] Blur(float[] src, int w, int h, int r)
    {
        var tmp = new float[src.Length];
        var dst = new float[src.Length];
        float inv = 1f / (2 * r + 1);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int k = -r; k <= r; k++) sum += src[y * w + Mathf.Clamp(x + k, 0, w - 1)];
                tmp[y * w + x] = sum * inv;
            }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int k = -r; k <= r; k++) sum += tmp[Mathf.Clamp(y + k, 0, h - 1) * w + x];
                dst[y * w + x] = sum * inv;
            }
        return dst;
    }

    static int Mod(int a, int m) => ((a % m) + m) % m;
}
