using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 전장을 그린다. 맵 데이터(MapAsset)에 바닥 그림(background)이 있으면 그 그림을 카메라 영역에 깔고,
/// 없으면 임시 도형(바닥, 적 경로, 건설 칸, 출현 지점)으로 그린다.
/// 그림 맵에서는 건설 칸을 평소에 숨기고, 타워를 고를 때만 보여 준다(ShowBuildTiles).
/// 탐색형 판(DreamRun)이면 탐색에서 밝힌 칸 맵을 그린다: 밝힌 바닥, 몬스터 경로(최단 경로) 칸은 따뜻한 색,
/// 밝히지 않은 암흑(타워 자리가 되는 벽), 막힌 암흑, 남은 결정.
/// 테마 그림(MapTheme, 지금은 달빛 정원)이 있으면: 밝힌 칸은 돌길, 안 연 칸은 잔디(옅은 밤 안개), 막힌 암흑은 꽃밭,
/// 꽃밭 가장자리는 꽃덤불과 정원 소품(가로등은 은은하게 일렁이는 불빛), 돌길과 잔디 사이는 꽃 경계 띠,
/// 몬스터 경로는 은은한 꿈실(빛 알갱이가 출현 지점에서 꿈의 중심으로 흐름), 결정·출현 지점·꿈의 중심은 움직이는 오브젝트,
/// 설치 가능 칸은 바느질 점선. 맵 바깥은 어두운 꽃밭이 이어져 화면을 줄여도 검은 빈칸이 보이지 않는다.
/// </summary>
public class MapView : MonoBehaviour
{
    public BattleController controller;
    public Sprite square;
    public Sprite circle;

    public Color ground = new Color(0.12f, 0.12f, 0.22f);
    public Color lane = new Color(0.32f, 0.27f, 0.42f);
    public Color buildTile = new Color(0.25f, 0.42f, 0.45f, 0.35f);
    public Color buildTileOnArt = new Color(0.75f, 1f, 0.85f, 0.22f);
    public Color core = new Color(1f, 0.78f, 0.4f);
    public Color coreOnArt = new Color(0.75f, 0.55f, 1f, 0.55f);
    public Color spawn = new Color(0.65f, 0.4f, 0.95f);

    public Transform CoreTransform { get; private set; }

    readonly List<SpriteRenderer> buildMarkers = new List<SpriteRenderer>();
    bool hasArt;
    bool grid;
    SpriteRenderer coreGlow;
    MapDef map;
    int lastScreenWidth, lastScreenHeight;

    // 테마 연출
    MapTheme theme;
    SpriteRenderer fog;
    readonly List<Transform> bobbing = new List<Transform>();
    readonly List<Vector3> bobBase = new List<Vector3>();
    Transform rift;
    Vector3 riftScale;
    readonly List<Vector3> pathPoints = new List<Vector3>();
    readonly List<Transform> motes = new List<Transform>();
    readonly List<float> moteT = new List<float>();
    float moteTimer;
    readonly List<SpriteRenderer> lanternGlows = new List<SpriteRenderer>();
    bool Themed => grid && theme != null && theme.Valid;

    void Start()
    {
        if (controller.Session == null) return;
        map = controller.Content.Stage.Map;
        MapAsset asset = controller.Content.StageAsset != null ? controller.Content.StageAsset.map : null;
        grid = DreamRun.Active;
        if (grid) theme = MapTheme.Load();
        hasArt = !grid && asset != null && asset.background != null;
        FitCamera();

        var center = new Vector2(map.CameraX + map.CameraWidth / 2f, map.CameraY + map.CameraHeight / 2f);
        var size = new Vector2(map.CameraWidth, map.CameraHeight);
        if (hasArt)
        {
            CreateArt("Background", asset.background, center, size, -20);
            if (asset.foreground != null) CreateArt("Foreground", asset.foreground, center, size, 15);
        }
        else if (grid)
        {
            if (theme != null && theme.Valid) CreateThemedGrid(DreamRun.Map);
            else CreateGridCells(DreamRun.Map);
        }
        else
        {
            Create("Ground", square, ground, center, size, -20);
        }

        foreach (Vector2Int tile in BuildableTiles(map))
        {
            SpriteRenderer marker = Themed
                ? Create("BuildTile", theme.BuildMark, new Color(1f, 0.84f, 0.5f, 0.8f), new Vector2(tile.x, tile.y), Vector2.one * 0.94f, -11).GetComponent<SpriteRenderer>()
                : Create("BuildTile", square, hasArt ? buildTileOnArt : buildTile, new Vector2(tile.x, tile.y), Vector2.one * (hasArt ? 0.8f : 0.9f), -15)
                    .GetComponent<SpriteRenderer>();
            marker.enabled = !hasArt && !grid;
            buildMarkers.Add(marker);
        }

        if (Themed)
        {
            // 출현 지점·꿈의 중심은 CreateThemedGrid에서 오브젝트로 그렸다.
        }
        else if (grid)
        {
            foreach (SpawnPointDef spawnPoint in map.SpawnPoints)
                Create("Spawn " + spawnPoint.Id, circle, new Color(spawn.r, spawn.g, spawn.b, 0.85f), BattleContentBuilder.ToUnity(spawnPoint.Path[0]), Vector2.one * 0.9f, -16);
        }
        else if (!hasArt)
        {
            foreach (SpawnPointDef spawnPoint in map.SpawnPoints)
            {
                for (int i = 1; i < spawnPoint.Path.Count; i++)
                {
                    Vector2 a = BattleContentBuilder.ToUnity(spawnPoint.Path[i - 1]);
                    Vector2 b = BattleContentBuilder.ToUnity(spawnPoint.Path[i]);
                    Vector2 laneSize = new Vector2(Mathf.Abs(b.x - a.x) + 1f, Mathf.Abs(b.y - a.y) + 1f);
                    Create("Lane", square, lane, (a + b) / 2f, laneSize, -18);
                }
                Create("Spawn " + spawnPoint.Id, circle, spawn, BattleContentBuilder.ToUnity(spawnPoint.Path[0]), Vector2.one * 1.1f, -16);
            }
        }

        // 그림 맵에서는 꿈의 중심을 은은하게 빛나는 원으로만 표시한다.
        bool glowCore = hasArt || grid;
        GameObject coreGo = Create("Core", circle, glowCore ? coreOnArt : core, BattleContentBuilder.ToUnity(map.CorePos), Vector2.one * (glowCore ? 1.3f : 1.6f), -10);
        CoreTransform = coreGo.transform;
        if (glowCore) coreGlow = coreGo.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (map == null) return;
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight) FitCamera();
        if (coreGlow != null)
        {
            // 알파만 바꿔서 누수 때 깜빡이는 빨간색(BattleView.Flash)은 그대로 보이게 한다.
            Color c = coreGlow.color;
            c.a = coreOnArt.a * (0.75f + 0.25f * Mathf.Sin(Time.time * 2f));
            coreGlow.color = c;
        }
        if (Themed) AnimateTheme();
    }

    // ───────── 테마(달빛 정원) ─────────

    Vector3 CellWorld(Akmong.Battle.GridMap g, int x, int y) => new Vector3(x, g.Height - 1 - y, 0f);

    void CreateThemedGrid(Akmong.Battle.GridMap g)
    {
        var onPath = new HashSet<Vector2Int>();
        List<Akmong.Battle.GridPoint> path = g.ShortestPath();
        if (path != null) foreach (Akmong.Battle.GridPoint p in path) onPath.Add(new Vector2Int(p.X, p.Y));

        // 0) 맵 바깥: 어두운 꽃밭을 넉넉히 반복해 깔고, 그 너머는 밤하늘색
        if (Camera.main != null) Camera.main.backgroundColor = new Color(0.04f, 0.045f, 0.1f);
        int margin = Mathf.Max(g.Width, g.Height);
        var flowerSprite = Sprite.Create(theme.Flower, new Rect(0, 0, theme.Flower.width, theme.Flower.height), new Vector2(0.5f, 0.5f),
            theme.Flower.width / (float)MapTheme.Tiles(MapTheme.Ground.Flower), 0, SpriteMeshType.FullRect);
        GameObject backdrop = Create("Backdrop", flowerSprite, MapTheme.Shade(MapTheme.Ground.Flower) * new Color(0.55f, 0.55f, 0.62f), 
            new Vector2((g.Width - 1) / 2f, (g.Height - 1) / 2f), Vector2.one, -23);
        var backdropRenderer = backdrop.GetComponent<SpriteRenderer>();
        backdropRenderer.drawMode = SpriteDrawMode.Tiled;
        backdropRenderer.size = new Vector2(g.Width + margin * 2, g.Height + margin * 2);

        // 1) 바닥: 돌길(밝힌 칸)·잔디(안 연 칸)·꽃밭(막힌 암흑)
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
            {
                MapTheme.Ground kind = MapTheme.GroundAt(g, x, y);
                Create("Ground", theme.GroundSprite(kind, x, g.Height - 1 - y), MapTheme.Shade(kind), CellWorld(g, x, y), Vector2.one * 1.002f, -21);
            }

        // 2) 꽃밭 가장자리 덤불(칸 경계를 덮음)과 돌길·잔디 사이 꽃 경계 띠
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
            {
                Sprite tuft;
                float size, angle;
                Vector2 offset;
                bool flip;
                if (theme.Tuft(g, x, y, out tuft, out size, out offset, out angle, out flip))
                {
                    float scale = size / tuft.bounds.size.x;
                    GameObject t = Create("Tuft", tuft, MapTheme.Shade(MapTheme.Ground.Flower), (Vector2)CellWorld(g, x, y) + new Vector2(offset.x, -offset.y),
                        new Vector2(flip ? -scale : scale, scale), -20);
                    t.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                foreach (Vector2Int side in MapTheme.Sides)
                {
                    Sprite border = theme.BorderFor(g, x, y, side);
                    if (border == null) continue;
                    float scale = 1.06f / border.bounds.size.x;
                    Vector2 at = (Vector2)CellWorld(g, x, y) + new Vector2(side.x * 0.5f, -side.y * 0.5f);
                    GameObject b = Create("Border", border, Color.white, at, Vector2.one * scale, -18);
                    if (side.x != 0) b.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

        // 3) 밤 안개(안 연 칸, 옅게)
        Texture2D fogTexture = MapTheme.BuildFog(g, MapTheme.BattleFog);
        var fogSprite = Sprite.Create(fogTexture, new Rect(0, 0, fogTexture.width, fogTexture.height), Vector2.zero, MapTheme.FogPixelsPerCell);
        GameObject fogGo = Create("Fog", fogSprite, Color.white, new Vector2(-0.5f, -0.5f), Vector2.one, -14);
        fog = fogGo.GetComponent<SpriteRenderer>();

        // 4) 몬스터 경로: 칸 중심을 잇는 은은한 꿈실
        if (path != null)
        {
            for (int i = 0; i < path.Count; i++) pathPoints.Add(CellWorld(g, path[i].X, path[i].Y));
            for (int i = 1; i < pathPoints.Count; i++)
            {
                Vector3 a = pathPoints[i - 1], b = pathPoints[i];
                GameObject seg = Create("PathThread", theme.PathThread, new Color(0.95f, 0.85f, 1f, 0.5f), (a + b) / 2f,
                    new Vector2(Vector3.Distance(a, b) + 0.12f, 0.4f), -13);
                seg.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
        }

        // 5) 오브젝트: 정원 소품(가로등은 불빛), 결정, 출현 지점, 꿈의 중심
        for (int y = 0; y < g.Height; y++)
            for (int x = 0; x < g.Width; x++)
            {
                Akmong.Battle.CellType type = g.Get(x, y);
                Vector3 cell = CellWorld(g, x, y);
                Sprite garden;
                float width;
                if (theme.GardenProp(g, x, y, out garden, out width))
                {
                    Transform prop = Prop("Garden", garden, cell, width, -9, Color.white);
                    if (garden.name.Contains("lantern") && theme.FxGlow != null)
                    {
                        float top = prop.position.y + garden.bounds.size.y * prop.localScale.y * 0.25f;
                        GameObject glow = Create("LanternGlow", theme.FxGlow, new Color(1f, 0.75f, 0.4f, 0.4f), new Vector2(cell.x, top), Vector2.one * 1.6f / theme.FxGlow.bounds.size.x, -8);
                        lanternGlows.Add(glow.GetComponent<SpriteRenderer>());
                    }
                }
                else if (type == Akmong.Battle.CellType.CrystalSmall || type == Akmong.Battle.CellType.CrystalLarge)
                {
                    bool big = type == Akmong.Battle.CellType.CrystalLarge;
                    Transform gem = Prop("Crystal", big ? theme.CrystalLarge : theme.CrystalSmall, cell, big ? 0.85f : 0.6f, -8, Color.white);
                    bobbing.Add(gem);
                    bobBase.Add(gem.localPosition);
                }
            }
        if (g.HasSpawn)
        {
            rift = Prop("SpawnRift", theme.SpawnRift, CellWorld(g, g.SpawnX, g.SpawnY), 1.1f, -8, Color.white);
            riftScale = rift.localScale;
        }
        if (g.HasGoal) Prop("CoreFlower", theme.CoreFlower, CellWorld(g, g.GoalX, g.GoalY), 0.85f, -8, Color.white);
    }

    /// <summary>바닥에 서 있는 오브젝트: 아래쪽이 칸 바닥(중심보다 0.42칸 아래)에 닿게, 위쪽 칸일수록 뒤에 그린다.</summary>
    Transform Prop(string objectName, Sprite sprite, Vector3 cell, float width, int order, Color color)
    {
        if (sprite == null) return transform;
        float scale = width / sprite.bounds.size.x;
        float height = sprite.bounds.size.y * scale;
        var position = new Vector3(cell.x, cell.y - 0.42f + height / 2f, cell.y * 0.001f);
        GameObject go = Create(objectName, sprite, color, position, Vector2.one * scale, order);
        go.transform.position = position;
        return go.transform;
    }

    void AnimateTheme()
    {
        float t = Time.time;
        if (fog != null) fog.color = new Color(1f, 1f, 1f, 0.9f + 0.1f * Mathf.Sin(t * 0.8f));
        for (int i = 0; i < lanternGlows.Count; i++)
            lanternGlows[i].color = new Color(1f, 0.75f, 0.4f, 0.32f + 0.1f * Mathf.PerlinNoise(t * 2.5f, i * 3.1f));
        for (int i = 0; i < bobbing.Count; i++)
            bobbing[i].localPosition = bobBase[i] + new Vector3(0f, 0.04f * Mathf.Sin(t * 2f + i * 1.3f), 0f);
        if (rift != null)
        {
            float k = 1f + 0.04f * Mathf.Sin(t * 3f);
            rift.localScale = new Vector3(riftScale.x * k, riftScale.y * k, 1f);
        }

        // 빛길: 빛 알갱이가 출현 지점에서 꿈의 중심으로 흐른다(약 2.5칸/초)
        if (pathPoints.Count < 2) return;
        moteTimer -= Time.deltaTime;
        if (moteTimer <= 0f)
        {
            moteTimer = 0.45f;
            GameObject mote = Create("PathMote", circle, new Color(1f, 0.9f, 1f, 0.9f), pathPoints[0], Vector2.one * 0.16f, -12);
            motes.Add(mote.transform);
            moteT.Add(0f);
        }
        for (int i = motes.Count - 1; i >= 0; i--)
        {
            moteT[i] += Time.deltaTime * 2.5f;
            int seg = Mathf.FloorToInt(moteT[i]);
            if (seg >= pathPoints.Count - 1)
            {
                Destroy(motes[i].gameObject);
                motes.RemoveAt(i);
                moteT.RemoveAt(i);
                continue;
            }
            motes[i].position = Vector3.Lerp(pathPoints[seg], pathPoints[seg + 1], moteT[i] - seg);
        }
    }

    /// <summary>타워를 고르는 동안 지을 수 있는 칸을 보여 준다(그림 맵 전용, 임시 도형 맵은 항상 보임).</summary>
    public void ShowBuildTiles(bool show)
    {
        if (!hasArt && !grid) return;
        foreach (SpriteRenderer marker in buildMarkers) marker.enabled = show;
    }

    /// <summary>칸 맵 그리기. 월드 좌표: 칸 (x, y) → (x, Height - 1 - y).</summary>
    void CreateGridCells(Akmong.Battle.GridMap gridMap)
    {
        var floor = new Color(0.62f, 0.53f, 0.41f);
        var pathColor = new Color(0.86f, 0.66f, 0.38f);
        var dark = new Color(0.07f, 0.06f, 0.13f);
        var wall = new Color(0.015f, 0.01f, 0.03f);
        var wallMark = new Color(0.42f, 0.10f, 0.24f);
        var small = new Color(0.62f, 0.45f, 1f);
        var large = new Color(0.40f, 0.85f, 1f);

        var onPath = new HashSet<Vector2Int>();
        List<Akmong.Battle.GridPoint> path = gridMap.ShortestPath();
        if (path != null) foreach (Akmong.Battle.GridPoint p in path) onPath.Add(new Vector2Int(p.X, p.Y));

        for (int y = 0; y < gridMap.Height; y++)
            for (int x = 0; x < gridMap.Width; x++)
            {
                var pos = new Vector2(x, gridMap.Height - 1 - y);
                Akmong.Battle.CellType type = gridMap.Get(x, y);
                if (gridMap.IsOpen(x, y))
                {
                    // 몬스터 경로는 타일 색으로 구분(기획: 전투가 시작되면 경로를 보여 줌)
                    Create("Floor", square, onPath.Contains(new Vector2Int(x, y)) ? pathColor : floor, pos, Vector2.one * 0.98f, -20);
                }
                else if (type == Akmong.Battle.CellType.Wall)
                {
                    Create("Wall", square, wall, pos, Vector2.one, -20);
                    Create("WallMark", square, wallMark, pos, Vector2.one * 0.32f, -19);
                }
                else
                {
                    Create("Dark", square, dark, pos, Vector2.one, -20);
                    if (type == Akmong.Battle.CellType.CrystalSmall || type == Akmong.Battle.CellType.CrystalLarge)
                    {
                        bool big = type == Akmong.Battle.CellType.CrystalLarge;
                        GameObject gem = Create("Crystal", square, big ? large : small, pos, Vector2.one * (big ? 0.5f : 0.32f), -19);
                        gem.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                    }
                }
            }
    }

    /// <summary>건설 구역 안의 정수 칸 중 실제로 지을 수 있는 칸(길 여유 거리 포함).</summary>
    static List<Vector2Int> BuildableTiles(MapDef map)
    {
        var tiles = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        foreach (BuildZone zone in map.BuildZones)
            for (int x = Mathf.CeilToInt(zone.XMin); x <= Mathf.FloorToInt(zone.XMax); x++)
                for (int y = Mathf.CeilToInt(zone.YMin); y <= Mathf.FloorToInt(zone.YMax); y++)
                {
                    var tile = new Vector2Int(x, y);
                    if (seen.Add(tile) && map.IsBuildable(new System.Numerics.Vector2(x, y))) tiles.Add(tile);
                }
        return tiles;
    }

    /// <summary>HUD가 덮는 화면 위·아래 비율(전투 HUD를 줄인 크기 기준). 맵은 그 사이 띠에 맞춰 보여 준다.</summary>
    const float HudTop = 0.085f, HudBottom = 0.155f;

    /// <summary>
    /// 맵 전체(카메라 영역)가 HUD를 뺀 화면 띠 안에 다 들어오게 맞춘다. 화면비가 다르면 남는 쪽은 배경색.
    /// 휠로 더 줄이거나 당겨 볼 수 있다(CameraZoom).
    /// </summary>
    void FitCamera()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float band = 1f - HudTop - HudBottom;
        float size = Mathf.Max(map.CameraHeight / 2f / band, map.CameraWidth / 2f / aspect);
        // 맵 가운데가 띠의 가운데에 오도록 카메라를 아래로 내린다.
        float bandCenter = HudBottom + band / 2f; // 화면 아래에서부터의 비율
        var mapCenter = new Vector2(map.CameraX + map.CameraWidth / 2f, map.CameraY + map.CameraHeight / 2f);
        var center = new Vector2(mapCenter.x, mapCenter.y - (bandCenter - 0.5f) * 2f * size);
        cam.orthographicSize = size;
        cam.transform.position = new Vector3(center.x, center.y, -10f);
        CameraZoom zoom = cam.GetComponent<CameraZoom>();
        if (zoom != null) zoom.SetFit(new Rect(map.CameraX, map.CameraY, map.CameraWidth, map.CameraHeight), size, center);
    }

    void CreateArt(string objectName, Sprite sprite, Vector2 center, Vector2 size, int order)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.position = center;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        Vector2 spriteSize = sprite.bounds.size;
        go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
    }

    GameObject Create(string objectName, Sprite sprite, Color color, Vector2 position, Vector2 size, int order)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }
}
