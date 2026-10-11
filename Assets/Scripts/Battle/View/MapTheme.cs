using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 칸 맵 테마(그림 묶음). Assets/Resources/Themes/{이름}/ 의 그림을 불러 전투 맵(MapView)과 탐색 화면(ExploreUI)이 같이 쓴다.
/// - ground: 이음새 없이 반복되는 바닥(512px = 4×4칸). 칸 (x, y)는 그 안의 한 조각.
/// - 벽(막힌 암흑) 4종, 작은·큰 결정, 출현 지점, 꿈의 중심, 새싹: 살짝 앞에서 본 오브젝트(아래쪽이 칸 바닥).
/// - path_thread: 몬스터 경로를 잇는 빛나는 꿈실 띠, build_mark: 타워를 지을 수 있는 칸의 바느질 점선.
/// - 어둠(안 연 칸)은 그림이 아니라 BuildFog로 만든 부드러운 안개.
/// 새 테마는 같은 이름의 그림을 다른 폴더에 넣고 Load("폴더 이름")으로 바꾸면 된다(Tools/Art/build_moon_garden.py 참고).
/// </summary>
public class MapTheme
{
    public const int GroundTiles = 4;      // 바닥 그림 한 장이 덮는 칸 수(가로·세로)
    public const int FogPixelsPerCell = 8;

    public Texture2D Ground { get; private set; }
    public Sprite[] Walls { get; private set; }
    public Sprite CrystalSmall, CrystalLarge, SpawnRift, CoreFlower, Sprout, PathThread, BuildMark;

    readonly Dictionary<int, Sprite> groundSprites = new Dictionary<int, Sprite>();
    static readonly Dictionary<string, MapTheme> cache = new Dictionary<string, MapTheme>();

    public bool Valid => Ground != null && Walls != null && Walls.Length > 0;

    public static MapTheme Load(string name = "MoonGarden")
    {
        MapTheme theme;
        if (cache.TryGetValue(name, out theme)) return theme;
        string dir = "Themes/" + name + "/";
        theme = new MapTheme
        {
            Ground = Resources.Load<Texture2D>(dir + "ground"),
            CrystalSmall = Resources.Load<Sprite>(dir + "crystal_small"),
            CrystalLarge = Resources.Load<Sprite>(dir + "crystal_large"),
            SpawnRift = Resources.Load<Sprite>(dir + "spawn_rift"),
            CoreFlower = Resources.Load<Sprite>(dir + "core_flower"),
            Sprout = Resources.Load<Sprite>(dir + "sprout"),
            PathThread = Resources.Load<Sprite>(dir + "path_thread"),
            BuildMark = Resources.Load<Sprite>(dir + "build_mark"),
        };
        var walls = new List<Sprite>();
        foreach (string wall in new[] { "wall_bramble", "wall_knot", "wall_rock", "wall_fence" })
        {
            Sprite s = Resources.Load<Sprite>(dir + wall);
            if (s != null) walls.Add(s);
        }
        theme.Walls = walls.ToArray();
        cache[name] = theme;
        return theme;
    }

    /// <summary>바닥 조각(1칸 = 1유닛). x는 오른쪽, y는 위쪽으로 늘어나는 칸 번호(월드 좌표). 4×4칸마다 같은 그림이 반복된다.</summary>
    public Sprite GroundSprite(int x, int y)
    {
        int gx = Mod(x, GroundTiles), gy = Mod(y, GroundTiles);
        int key = gy * GroundTiles + gx;
        Sprite sprite;
        if (groundSprites.TryGetValue(key, out sprite)) return sprite;
        float cell = Ground.width / (float)GroundTiles;
        sprite = Sprite.Create(Ground, new Rect(gx * cell, gy * cell, cell, cell), new Vector2(0.5f, 0.5f), cell);
        groundSprites[key] = sprite;
        return sprite;
    }

    /// <summary>IMGUI용 바닥 텍스처 좌표(0~1). y는 GroundSprite와 같이 위쪽으로 늘어나는 번호(칸 맵 행 → Height - 1 - 행).</summary>
    public Rect GroundUV(int x, int y)
    {
        float k = 1f / GroundTiles;
        return new Rect(Mod(x, GroundTiles) * k, Mod(y, GroundTiles) * k, k, k);
    }

    /// <summary>
    /// 벽(막힌 암흑) 오브젝트를 그릴지와 밝기. 벽으로 둘러싸인 안쪽 벽은 그리지 않고(그냥 어둠) 가장자리 벽만 그린다.
    /// 밝힌 칸 바로 옆 벽은 환하게, 어둠 속 벽은 어둡게. 반환값 알파가 0이면 그리지 않는다.
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
        return lit ? Color.white : new Color(0.5f, 0.5f, 0.62f, 1f);
    }

    /// <summary>칸마다 고정된(맵이 같으면 늘 같은) 벽 그림.</summary>
    public Sprite WallFor(int x, int y) => Walls[Mod(x * 73856093 ^ y * 19349663, Walls.Length)];

    /// <summary>
    /// 어둠 안개 텍스처: 아직 밝히지 않은 칸은 짙은 남색, 밝힌 칸은 투명. 경계는 흐리게 번져 랜턴 빛이 스민 것처럼.
    /// 텍스처 아래쪽 줄 = 월드 y 0(맨 아래 칸). 칸 (x, y)의 월드 y는 Height - 1 - y.
    /// </summary>
    public static Texture2D BuildFog(GridMap grid, Texture2D reuse = null)
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
            pixels[i] = new Color32(7, 6, 20, (byte)(a * 232f));
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
