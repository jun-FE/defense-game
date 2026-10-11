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
    public const float ExploreFog = 0.8f;    // 탐색: 짙은 어둠(정원이 희미하게 비침)

    public enum Ground { Stone, Grass, Flower }

    public Texture2D Stone, Grass, Flower;
    public Sprite[] Tufts { get; private set; }
    public Sprite[] Borders { get; private set; }
    public Sprite[] Garden { get; private set; }
    public float[] GardenWidth { get; private set; }
    public Sprite CrystalSmall, CrystalLarge, SpawnRift, CoreFlower, PathThread, BuildMark, FxGlow, FxSpark, FxRing;

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
            FxRing = Resources.Load<Sprite>(dir + "fx_ring"),
        };
        theme.Tufts = LoadAll(dir, "flower_tuft_", 4);
        theme.Borders = LoadAll(dir, "border_", 4);
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
        size = 1.3f + ((k >> 4) % 5) * 0.05f;
        offset = new Vector2(((k >> 10) % 5 - 2) * 0.05f, ((k >> 13) % 5 - 2) * 0.05f);
        angle = ((k >> 16) % 4) * 90f;
        flip = ((k >> 8) % 2) == 1;
        return true;
    }

    /// <summary>정원 소품: 잔디·돌길과 변으로 맞닿은 꽃밭 칸 세 개 중 하나쯤. width는 칸 단위 폭.</summary>
    public bool GardenProp(GridMap g, int x, int y, out Sprite sprite, out float width)
    {
        sprite = null; width = 0f;
        if (Garden.Length == 0 || !IsWall(g, x, y) || Hash(x, y) % 3 != 0) return false;
        if (IsWall(g, x + 1, y) && IsWall(g, x - 1, y) && IsWall(g, x, y + 1) && IsWall(g, x, y - 1)) return false;
        int k = Hash(y + 7, x + 3) % Garden.Length;
        sprite = Garden[k];
        width = GardenWidth[k];
        return true;
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
        return Borders[Hash(x * 2 + dir.x, y * 2 + dir.y) % Borders.Length];
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
