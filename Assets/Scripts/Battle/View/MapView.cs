using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 전장을 그린다. 맵 데이터(MapAsset)에 바닥 그림(background)이 있으면 그 그림을 카메라 영역에 깔고,
/// 없으면 임시 도형(바닥, 적 경로, 건설 칸, 출현 지점)으로 그린다.
/// 그림 맵에서는 건설 칸을 평소에 숨기고, 타워를 고를 때만 보여 준다(ShowBuildTiles).
/// 탐색형 판(DreamRun)이면 탐색에서 밝힌 칸 맵을 그린다: 밝힌 바닥, 몬스터 경로(최단 경로) 칸은 따뜻한 색,
/// 밝히지 않은 암흑(타워 자리가 되는 벽), 막힌 암흑, 남은 결정.
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

    void Start()
    {
        if (controller.Session == null) return;
        map = controller.Content.Stage.Map;
        MapAsset asset = controller.Content.StageAsset != null ? controller.Content.StageAsset.map : null;
        grid = DreamRun.Active;
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
            CreateGridCells(DreamRun.Map);
        }
        else
        {
            Create("Ground", square, ground, center, size, -20);
        }

        foreach (Vector2Int tile in BuildableTiles(map))
        {
            SpriteRenderer marker = Create("BuildTile", square, hasArt ? buildTileOnArt : buildTile, new Vector2(tile.x, tile.y), Vector2.one * (hasArt ? 0.8f : 0.9f), -15)
                .GetComponent<SpriteRenderer>();
            marker.enabled = !hasArt && !grid;
            buildMarkers.Add(marker);
        }

        if (grid)
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
