using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 데모 씬(경로, 건설 자리, 타워/적 프리팹, 임시 스프라이트)을 코드로 만든다.
/// 메뉴: Defense > 데모 씬 다시 만들기
/// 프로젝트를 처음 열었을 때 Main 씬이 없으면 자동으로 한 번 실행된다.
/// </summary>
public static class DemoSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Main.unity";

    const string ArtDir = "Assets/Art";
    const string PrefabDir = "Assets/Prefabs";
    const string SceneDir = "Assets/Scenes";

    static readonly Vector2[] PathPoints =
    {
        new Vector2(-11f, 3f), new Vector2(-5f, 3f), new Vector2(-5f, -3f), new Vector2(1f, -3f),
        new Vector2(1f, 3f), new Vector2(6f, 3f), new Vector2(6f, -1f), new Vector2(11f, -1f),
    };

    static readonly Vector2[] SlotPoints =
    {
        new Vector2(-8f, 1.5f), new Vector2(-8f, 4.5f), new Vector2(-6.5f, -1f), new Vector2(-3.5f, 0f),
        new Vector2(-2f, -1.5f), new Vector2(-2f, -4.5f), new Vector2(-0.5f, 1.5f), new Vector2(2.5f, 1.5f),
        new Vector2(3.5f, 4.5f), new Vector2(4.5f, 0f), new Vector2(7.5f, 1.5f), new Vector2(9f, -2.5f),
    };

    [MenuItem("Defense/데모 씬 다시 만들기")]
    static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build();
    }

    public static void Build()
    {
        EnsureFolder(ArtDir);
        EnsureFolder(PrefabDir);
        EnsureFolder(SceneDir);

        Sprite square = CreateSpriteAsset($"{ArtDir}/Square.png", 32, false);
        Sprite circle = CreateSpriteAsset($"{ArtDir}/Circle.png", 64, true);

        Projectile bullet = CreateProjectilePrefab("Bullet", circle, new Color(1f, 0.95f, 0.4f), 0.18f);
        Projectile shell = CreateProjectilePrefab("CannonShell", circle, new Color(1f, 0.55f, 0.2f), 0.3f);
        Tower basic = CreateTowerPrefab("BasicTower", square, circle, bullet, new Color(0.35f, 0.55f, 0.9f),
            "기본 타워", 50, range: 2.6f, fireRate: 2f, damage: 1f, splash: 0f, speed: 10f);
        Tower cannon = CreateTowerPrefab("CannonTower", square, circle, shell, new Color(0.85f, 0.45f, 0.25f),
            "대포 타워", 90, range: 2.2f, fireRate: 0.6f, damage: 3f, splash: 1.2f, speed: 6f);
        Enemy enemy = CreateEnemyPrefab(square, circle);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.32f, 0.2f);
        cameraGo.AddComponent<AudioListener>();

        PathRoute path = CreatePath(square);

        var slots = new GameObject("BuildSlots").transform;
        foreach (Vector2 p in SlotPoints)
        {
            GameObject slot = CreateSprite("Slot", square, new Color(0.3f, 0.45f, 0.3f), slots, p, Vector2.one * 0.9f, -5);
            slot.AddComponent<BuildSlot>();
        }

        var game = new GameObject("Game");
        game.AddComponent<GameManager>();
        var spawner = game.AddComponent<WaveSpawner>();
        spawner.enemyPrefab = enemy;
        spawner.path = path;
        var build = game.AddComponent<BuildManager>();
        build.towerPrefabs = new[] { basic, cannon };
        var hud = game.AddComponent<HUD>();
        hud.worldCamera = cam;
        hud.buildManager = build;
        hud.spawner = spawner;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Defense] 데모 씬을 만들었습니다. Play 버튼을 눌러 보세요.");
    }

    static PathRoute CreatePath(Sprite square)
    {
        var root = new GameObject("Path");
        var route = root.AddComponent<PathRoute>();
        route.waypoints = new Transform[PathPoints.Length];
        for (int i = 0; i < PathPoints.Length; i++)
        {
            var point = new GameObject($"Waypoint{i}").transform;
            point.SetParent(root.transform);
            point.position = PathPoints[i];
            route.waypoints[i] = point;
        }

        var road = new GameObject("Road").transform;
        road.SetParent(root.transform);
        var roadColor = new Color(0.72f, 0.62f, 0.42f);
        for (int i = 0; i < PathPoints.Length - 1; i++)
        {
            Vector2 a = PathPoints[i];
            Vector2 b = PathPoints[i + 1];
            Vector2 size = new Vector2(Mathf.Abs(b.x - a.x) + 1f, Mathf.Abs(b.y - a.y) + 1f);
            CreateSprite("Segment", square, roadColor, road, (a + b) * 0.5f, size, -10);
        }
        return route;
    }

    static Projectile CreateProjectilePrefab(string name, Sprite circle, Color color, float size)
    {
        var go = CreateSprite(name, circle, color, null, Vector2.zero, Vector2.one * size, 5);
        go.AddComponent<Projectile>();
        return SavePrefab(go).GetComponent<Projectile>();
    }

    static Tower CreateTowerPrefab(string name, Sprite square, Sprite circle, Projectile projectile, Color color,
        string displayName, int cost, float range, float fireRate, float damage, float splash, float speed)
    {
        var root = new GameObject(name);
        CreateSprite("Base", square, color * 0.7f + new Color(0, 0, 0, 0.3f), root.transform, Vector2.zero, Vector2.one * 0.8f, 0);

        var head = new GameObject("Head").transform;
        head.SetParent(root.transform, false);
        CreateSprite("Turret", circle, color, head, Vector2.zero, Vector2.one * 0.55f, 1);
        CreateSprite("Barrel", square, color * 0.6f + new Color(0, 0, 0, 0.4f), head, new Vector2(0f, 0.35f), new Vector2(0.18f, 0.45f), 1);

        var tower = root.AddComponent<Tower>();
        tower.displayName = displayName;
        tower.cost = cost;
        tower.range = range;
        tower.fireRate = fireRate;
        tower.damage = damage;
        tower.splashRadius = splash;
        tower.projectileSpeed = speed;
        tower.head = head;
        tower.projectilePrefab = projectile;
        return SavePrefab(root).GetComponent<Tower>();
    }

    static Enemy CreateEnemyPrefab(Sprite square, Sprite circle)
    {
        var root = new GameObject("Enemy");
        CreateSprite("Body", circle, new Color(0.85f, 0.25f, 0.3f), root.transform, Vector2.zero, Vector2.one * 0.6f, 2);
        CreateSprite("HealthBack", square, new Color(0.1f, 0.1f, 0.1f), root.transform, new Vector2(0f, 0.5f), new Vector2(0.7f, 0.1f), 3);
        var fill = CreateSprite("HealthFill", square, new Color(0.3f, 0.9f, 0.3f), root.transform, new Vector2(0f, 0.5f), new Vector2(0.7f, 0.1f), 4);

        var enemy = root.AddComponent<Enemy>();
        enemy.healthFill = fill.transform;
        return SavePrefab(root).GetComponent<Enemy>();
    }

    static GameObject CreateSprite(string name, Sprite sprite, Color color, Transform parent, Vector2 position, Vector2 scale, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    static GameObject SavePrefab(GameObject go)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{go.name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>흰색 사각형/원 PNG를 만들어 스프라이트로 임포트한다. 나중에 실제 아트로 교체하면 된다.</summary>
    static Sprite CreateSpriteAsset(string path, int size, bool circle)
    {
        if (!File.Exists(path))
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 1f;
                    if (circle)
                    {
                        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                        alpha = Mathf.Clamp01(radius - distance);
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}

/// <summary>프로젝트를 처음 열었을 때 Main 씬이 없으면 데모 씬을 만들고 연다.</summary>
[InitializeOnLoad]
static class DemoSceneAutoSetup
{
    const string SessionKey = "DefenseGame.AutoSetupChecked";

    static DemoSceneAutoSetup()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(DemoSceneBuilder.ScenePath)) return;
            DemoSceneBuilder.Build();
        };
    }
}
