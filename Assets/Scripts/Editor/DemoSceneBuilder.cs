using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 데모 게임 전체를 코드로 만든다.
/// - 씬: Title(메인 메뉴), Story(스토리), Main(게임)
/// - 임시 스프라이트, 타워/적 프리팹
/// - 스테이지/스토리 데이터 (이미 있으면 덮어쓰지 않는다 → 직접 고친 대사는 안전)
/// 메뉴: Defense > 데모 게임 다시 만들기
/// 프로젝트를 열었을 때 Title 씬이 없으면 자동으로 한 번 실행된다.
/// </summary>
public static class DemoSceneBuilder
{
    public const string TitleScenePath = "Assets/Scenes/Title.unity";
    public const string StoryScenePath = "Assets/Scenes/Story.unity";
    public const string GameScenePath = "Assets/Scenes/Main.unity";

    const string ArtDir = "Assets/Art";
    const string PrefabDir = "Assets/Prefabs";
    const string SceneDir = "Assets/Scenes";
    const string DataDir = "Assets/Data";
    const string ResourcesDir = "Assets/Resources";

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

    [MenuItem("Defense/데모 게임 다시 만들기")]
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
        EnsureFolder(DataDir);
        EnsureFolder(ResourcesDir);

        StageDatabase database = CreateStageData();

        Sprite square = CreateSpriteAsset($"{ArtDir}/Square.png", 32, false);
        Sprite circle = CreateSpriteAsset($"{ArtDir}/Circle.png", 64, true);

        Projectile bullet = CreateProjectilePrefab("Bullet", circle, new Color(1f, 0.95f, 0.4f), 0.18f);
        Projectile shell = CreateProjectilePrefab("CannonShell", circle, new Color(1f, 0.55f, 0.2f), 0.3f);
        Tower basic = CreateTowerPrefab("BasicTower", square, circle, bullet, new Color(0.35f, 0.55f, 0.9f),
            "기본 타워", 50, range: 2.6f, fireRate: 2f, damage: 1f, splash: 0f, speed: 10f);
        Tower cannon = CreateTowerPrefab("CannonTower", square, circle, shell, new Color(0.85f, 0.45f, 0.25f),
            "대포 타워", 90, range: 2.2f, fireRate: 0.6f, damage: 3f, splash: 1.2f, speed: 6f);
        Enemy enemy = CreateEnemyPrefab(square, circle);

        BuildGameScene(square, basic, cannon, enemy);
        BuildTitleScene(square, circle);
        BuildStoryScene(database);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(StoryScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(TitleScenePath);
        Debug.Log("[Defense] 데모 게임을 만들었습니다. Title 씬에서 Play 버튼을 눌러 보세요.");
    }

    static void BuildGameScene(Sprite square, Tower basic, Tower cannon, Enemy enemy)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = CreateCamera(new Color(0.18f, 0.32f, 0.2f));

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

        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    static void BuildTitleScene(Sprite square, Sprite circle)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.07f, 0.09f, 0.15f));

        // 배경 장식: 길과 타워 실루엣 (나중에 타이틀 일러스트로 교체)
        var decor = new GameObject("Decor").transform;
        var road = new Color(0.16f, 0.2f, 0.3f);
        CreateSprite("Road", square, road, decor, new Vector2(0f, -4.2f), new Vector2(24f, 1.2f), -10);
        CreateSprite("Ground", square, new Color(0.1f, 0.13f, 0.2f), decor, new Vector2(0f, -5.6f), new Vector2(24f, 1.8f), -11);
        float[] towerX = { -9f, -6.5f, 6.5f, 9f };
        foreach (float x in towerX)
        {
            CreateSprite("Tower", square, new Color(0.2f, 0.26f, 0.4f), decor, new Vector2(x, -2.9f), new Vector2(0.9f, 1.4f), -9);
            CreateSprite("Head", circle, new Color(0.3f, 0.4f, 0.6f), decor, new Vector2(x, -2f), Vector2.one * 0.7f, -8);
        }
        CreateSprite("Moon", circle, new Color(0.95f, 0.9f, 0.7f, 0.9f), decor, new Vector2(8.5f, 4.2f), Vector2.one * 1.6f, -12);

        new GameObject("TitleMenu").AddComponent<TitleMenu>();
        EditorSceneManager.SaveScene(scene, TitleScenePath);
    }

    static void BuildStoryScene(StageDatabase database)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.08f, 0.09f, 0.14f));
        var player = new GameObject("StoryPlayer").AddComponent<StoryPlayer>();
        player.fallbackStory = database.Get(0) != null ? database.Get(0).introStory : null;
        EditorSceneManager.SaveScene(scene, StoryScenePath);
    }

    static Camera CreateCamera(Color background)
    {
        var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cameraGo.AddComponent<AudioListener>();
        return cam;
    }

    /// <summary>임시 스테이지 3개와 스토리 2개. 이미 있으면 그대로 둔다.</summary>
    static StageDatabase CreateStageData()
    {
        string databasePath = $"{ResourcesDir}/{StageDatabase.ResourcePath}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<StageDatabase>(databasePath);
        if (existing != null) return existing;

        StoryData intro = CreateStory("Story_1-1_Intro", new Color(0.08f, 0.09f, 0.16f), new[]
        {
            new StoryLine("", "평화롭던 왕국 아르덴. 어느 날, 북쪽 숲 너머에서 붉은 그림자들이 몰려오기 시작했다."),
            new StoryLine("", "왕국의 마지막 방어선, 초원의 입구. 이곳이 무너지면 수도까지 막을 것이 없다."),
            new StoryLine("기사단장 레온", "지휘관님, 드디어 오셨군요! 정찰병 말로는 적 선봉대가 곧 이 길로 들이닥친답니다."),
            new StoryLine("견습 마법사 미나", "길 옆에 탑을 세울 자리를 마련해 뒀어요. 골드만 있으면 바로 지을 수 있어요!"),
            new StoryLine("기사단장 레온", "기본 타워는 싸고 빠르게 쏘고, 대포 타워는 비싸지만 여러 놈을 한꺼번에 날려버리죠."),
            new StoryLine("견습 마법사 미나", "적을 쓰러뜨리면 골드가 들어오니까, 처음엔 아껴서 잘 배치해 주세요."),
            new StoryLine("기사단장 레온", "놈들이 옵니다! 지휘관님, 명령을 내려 주십시오!"),
        });
        StoryData gate = CreateStory("Story_1-3_Gate", new Color(0.14f, 0.06f, 0.08f), new[]
        {
            new StoryLine("", "숲을 지나온 적의 본대가 마침내 성문 앞에 모습을 드러냈다."),
            new StoryLine("기사단장 레온", "저 거대한 놈... 지금까지와는 차원이 다릅니다."),
            new StoryLine("견습 마법사 미나", "여기서 막아내면 왕국을 지킬 수 있어요. 끝까지 버텨 봐요!"),
        });

        var database = ScriptableObject.CreateInstance<StageDatabase>();
        database.stages = new[]
        {
            CreateStage("Stage_1-1", "1-1", "초원의 입구", intro, gold: 150, lives: 20, waves: 5, health: 0.8f),
            CreateStage("Stage_1-2", "1-2", "어두운 숲길", null, gold: 130, lives: 20, waves: 8, health: 1f),
            CreateStage("Stage_1-3", "1-3", "성문 앞 결전", gate, gold: 120, lives: 15, waves: 10, health: 1.2f),
        };
        AssetDatabase.CreateAsset(database, databasePath);
        return database;
    }

    static StoryData CreateStory(string fileName, Color background, StoryLine[] lines)
    {
        var story = ScriptableObject.CreateInstance<StoryData>();
        story.background = background;
        story.lines = lines;
        AssetDatabase.CreateAsset(story, $"{DataDir}/{fileName}.asset");
        return story;
    }

    static StageData CreateStage(string fileName, string id, string title, StoryData story, int gold, int lives, int waves, float health)
    {
        var stage = ScriptableObject.CreateInstance<StageData>();
        stage.stageId = id;
        stage.title = title;
        stage.introStory = story;
        stage.startGold = gold;
        stage.startLives = lives;
        stage.totalWaves = waves;
        stage.enemyHealthMultiplier = health;
        AssetDatabase.CreateAsset(stage, $"{DataDir}/{fileName}.asset");
        return stage;
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

/// <summary>프로젝트를 열었을 때 Title 씬이 없으면 데모 게임을 만들고 Title 씬을 연다.</summary>
[InitializeOnLoad]
static class DemoSceneAutoSetup
{
    const string SessionKey = "DefenseGame.AutoSetupChecked.v2";

    static DemoSceneAutoSetup()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(DemoSceneBuilder.TitleScenePath)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            DemoSceneBuilder.Build();
        };
    }
}
