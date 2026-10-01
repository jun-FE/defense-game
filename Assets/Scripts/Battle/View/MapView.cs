using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 맵 데이터로 임시 전장을 그린다(바닥, 적 경로, 건설 칸, 꿈의 중심, 출현 지점).
/// 전장 타일 아트가 나오면 이 스크립트 대신 배치된 타일맵을 쓰면 된다.
/// </summary>
public class MapView : MonoBehaviour
{
    public BattleController controller;
    public Sprite square;
    public Sprite circle;

    public Color ground = new Color(0.12f, 0.12f, 0.22f);
    public Color lane = new Color(0.32f, 0.27f, 0.42f);
    public Color buildTile = new Color(0.25f, 0.42f, 0.45f, 0.35f);
    public Color core = new Color(1f, 0.78f, 0.4f);
    public Color spawn = new Color(0.65f, 0.4f, 0.95f);

    public Transform CoreTransform { get; private set; }

    void Start()
    {
        if (controller.Session == null) return;
        MapDef map = controller.Content.Stage.Map;
        FitCamera(map);

        var center = new Vector2(map.CameraX + map.CameraWidth / 2f, map.CameraY + map.CameraHeight / 2f);
        Create("Ground", square, ground, center, new Vector2(map.CameraWidth, map.CameraHeight), -20);

        foreach (BuildZone zone in map.BuildZones)
        {
            for (int x = Mathf.CeilToInt(zone.XMin); x <= Mathf.FloorToInt(zone.XMax); x++)
            for (int y = Mathf.CeilToInt(zone.YMin); y <= Mathf.FloorToInt(zone.YMax); y++)
                Create("BuildTile", square, buildTile, new Vector2(x, y), Vector2.one * 0.9f, -15);
        }

        foreach (SpawnPointDef spawnPoint in map.SpawnPoints)
        {
            for (int i = 1; i < spawnPoint.Path.Count; i++)
            {
                Vector2 a = BattleContentBuilder.ToUnity(spawnPoint.Path[i - 1]);
                Vector2 b = BattleContentBuilder.ToUnity(spawnPoint.Path[i]);
                Vector2 size = new Vector2(Mathf.Abs(b.x - a.x) + 1f, Mathf.Abs(b.y - a.y) + 1f);
                Create("Lane", square, lane, (a + b) / 2f, size, -18);
            }
            Create("Spawn " + spawnPoint.Id, circle, spawn, BattleContentBuilder.ToUnity(spawnPoint.Path[0]), Vector2.one * 1.1f, -16);
        }

        CoreTransform = Create("Core", circle, core, BattleContentBuilder.ToUnity(map.CorePos), Vector2.one * 1.6f, -10).transform;
    }

    void FitCamera(MapDef map)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.transform.position = new Vector3(map.CameraX + map.CameraWidth / 2f, map.CameraY + map.CameraHeight / 2f, -10f);
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        cam.orthographicSize = Mathf.Max(map.CameraHeight / 2f, map.CameraWidth / 2f / aspect);
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
