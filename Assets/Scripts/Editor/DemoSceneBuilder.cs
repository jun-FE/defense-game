using System.Collections.Generic;
using System.IO;
using Akmong.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 데모 게임 전체를 코드로 만든다.
/// - 씬: Title(메인 메뉴), Story(스토리), Battle(전투)
/// - 임시 스프라이트, 타워 프리팹
/// - 전투 데이터(Assets/Data/Battle): 시스템 기획서 샘플(SampleContent)에서 처음 한 번만 만든다
/// - 스테이지/스토리 데이터
/// 데이터 에셋은 이미 있으면 덮어쓰지 않는다 → 기획자가 고친 수치와 대사는 안전하다.
/// 메뉴: Defense > 데모 게임 다시 만들기
/// </summary>
public static class DemoSceneBuilder
{
    public const string TitleScenePath = "Assets/Scenes/Title.unity";
    public const string StoryScenePath = "Assets/Scenes/Story.unity";
    public const string BattleScenePath = "Assets/Scenes/Battle.unity";

    const string ArtDir = "Assets/Art";
    const string PrefabDir = "Assets/Prefabs";
    const string SceneDir = "Assets/Scenes";
    const string DataDir = "Assets/Data";
    const string BattleDataDir = "Assets/Data/Battle";
    public const string DreamcatcherDir = "Assets/Art/Towers/Dreamcatcher";
    const string ResourcesDir = "Assets/Resources";

    // 이전 버전(단일 경로 디펜스)이 만든 파일. 업데이트할 때 지운다.
    static readonly string[] ObsoleteAssets =
    {
        "Assets/Scenes/Main.unity",
        "Assets/Prefabs/BasicTower.prefab",
        "Assets/Prefabs/CannonTower.prefab",
        "Assets/Prefabs/Enemy.prefab",
        "Assets/Prefabs/Bullet.prefab",
        "Assets/Prefabs/CannonShell.prefab",
        "Assets/Prefabs/Feather.prefab",
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
        EnsureFolder(BattleDataDir);
        EnsureFolder(ResourcesDir);

        foreach (string path in ObsoleteAssets)
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        Sprite square = CreateSpriteAsset($"{ArtDir}/Square.png", 32, false);
        Sprite circle = CreateSpriteAsset($"{ArtDir}/Circle.png", 64, true);

        GameRulesAsset rules;
        StageAsset battleStage = CreateBattleData(circle, out rules);
        StageDatabase database = CreateStageData(battleStage);

        BuildBattleScene(battleStage, rules, square, circle);
        BuildTitleScene(square, circle);
        BuildStoryScene(database);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(StoryScenePath, true),
            new EditorBuildSettingsScene(BattleScenePath, true),
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(TitleScenePath);
        Debug.Log("[Defense] 데모 게임을 만들었습니다. Title 씬에서 Play 버튼을 눌러 보세요.");
    }

    static void BuildBattleScene(StageAsset stage, GameRulesAsset rules, Sprite square, Sprite circle)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = CreateCamera(new Color(0.07f, 0.07f, 0.13f));

        var battle = new GameObject("Battle");
        var controller = battle.AddComponent<BattleController>();
        controller.defaultStage = stage;
        controller.rules = rules;

        var map = new GameObject("Map").AddComponent<MapView>();
        map.controller = controller;
        map.square = square;
        map.circle = circle;

        var view = new GameObject("BattleView").AddComponent<BattleView>();
        view.controller = controller;
        view.mapView = map;
        view.square = square;
        view.circle = circle;

        var hud = new GameObject("BattleHUD").AddComponent<BattleHUD>();
        hud.controller = controller;
        hud.view = view;
        hud.worldCamera = cam;
        hud.square = square;
        hud.circle = circle;

        EditorSceneManager.SaveScene(scene, BattleScenePath);
    }

    // ───────── 전투 데이터 (기획서 샘플 → 에셋, 처음 한 번) ─────────

    static StageAsset CreateBattleData(Sprite circle, out GameRulesAsset rules)
    {
        rules = LoadOrCreate<GameRulesAsset>("RULE_BASE", r =>
        {
            var source = new GameRules();
            r.armorConstant = source.ArmorConstant;
            r.minAttackSec = source.MinAttackSec;
            r.fixedDt = source.FixedDt;
            r.minMoveRatio = source.MinMoveRatio;
            r.maxMoveRatio = source.MaxMoveRatio;
        });

        StageDef sample = SampleContent.StageQ01();
        var enemyColors = new Dictionary<string, Color>
        {
            { "EN_TOY", new Color(0.85f, 0.3f, 0.35f) },
            { "EN_RUSH", new Color(0.62f, 0.42f, 0.95f) },
            { "EN_HEAVY", new Color(0.62f, 0.45f, 0.3f) },
        };
        var enemyScales = new Dictionary<string, float> { { "EN_TOY", 0.6f }, { "EN_RUSH", 0.45f }, { "EN_HEAVY", 0.9f } };

        var enemies = new Dictionary<EnemyDef, EnemyAsset>();
        foreach (WaveDef wave in sample.Waves)
        foreach (SpawnGroupDef group in wave.Groups)
        {
            EnemyDef def = group.Enemy;
            if (enemies.ContainsKey(def)) continue;
            enemies[def] = LoadOrCreate<EnemyAsset>(def.Id, e =>
            {
                e.displayName = def.Name;
                e.maxHp = def.MaxHp;
                e.armor = def.Armor;
                e.moveSpeed = def.MoveSpeed;
                e.coreDamage = def.CoreDamage;
                e.killCoin = def.KillCoin;
                e.sprite = circle;
                e.tint = enemyColors.ContainsKey(def.Id) ? enemyColors[def.Id] : Color.white;
                e.scale = enemyScales.ContainsKey(def.Id) ? enemyScales[def.Id] : 0.6f;
            });
        }

        var towers = new List<TowerAsset>();
        foreach (TowerDef def in sample.Towers)
        {
            towers.Add(LoadOrCreate<TowerAsset>(def.Id, t =>
            {
                t.displayName = def.Name;
                t.buildCost = def.BuildCost;
                t.levels = def.Levels.ConvertAll(l => new TowerLevelData
                {
                    id = l.Id, damage = l.Damage, range = l.Range, attackSec = l.AttackSec,
                    critChance = l.CritChance, critMult = l.CritMult, upgradeCost = l.UpgradeCost,
                }).ToArray();
            }));
        }
        // 스탠드(TW_LAMP)는 드림캐처 아트를 쓴다. 프리팹·아이콘이 비어 있으면 채운다.
        foreach (TowerAsset tower in towers)
        {
            if (tower.id != "TW_LAMP") continue;
            if (tower.prefab == null) tower.prefab = CreateDreamcatcherPrefab(tower.id);
            if (tower.icon == null) tower.icon = LoadTowerSprite("icon_tower.png");
            if (tower.iconDisabled == null) tower.iconDisabled = LoadTowerSprite("icon_tower_disabled.png");
            if (tower.projectile == null) tower.projectile = LoadTowerSprite("feather_projectile.png");
            EditorUtility.SetDirty(tower);
        }

        MapDef mapDef = sample.Map;
        MapAsset map = LoadOrCreate<MapAsset>(mapDef.Id, m =>
        {
            m.corePos = BattleContentBuilder.ToUnity(mapDef.CorePos);
            m.spawnPoints = mapDef.SpawnPoints.ConvertAll(sp => new SpawnPointData
            {
                id = sp.Id,
                path = sp.Path.ConvertAll(BattleContentBuilder.ToUnity).ToArray(),
            }).ToArray();
            m.buildZones = mapDef.BuildZones.ConvertAll(z => Rect.MinMaxRect(z.XMin, z.YMin, z.XMax, z.YMax)).ToArray();
            m.cameraBounds = new Rect(mapDef.CameraX, mapDef.CameraY, mapDef.CameraWidth, mapDef.CameraHeight);
        });

        return LoadOrCreate<StageAsset>(sample.Id, st =>
        {
            st.displayName = sample.Name;
            st.map = map;
            st.startCoin = sample.StartCoin;
            st.coreMaxHp = sample.CoreMaxHp;
            st.towers = towers.ToArray();
            st.waves = sample.Waves.ConvertAll(w => new WaveData
            {
                id = w.Id,
                prepareSec = w.PrepareSec,
                clearCoin = w.ClearCoin,
                groups = w.Groups.ConvertAll(g => new SpawnGroupData
                {
                    id = g.Id, spawnId = g.SpawnId, enemy = enemies[g.Enemy],
                    count = g.Count, startSec = g.StartSec, intervalSec = g.IntervalSec,
                }).ToArray(),
            }).ToArray();
        });
    }

    /// <summary>Assets/Data/Battle/{id}.asset 이 있으면 그대로 쓰고, 없으면 만들어서 fill로 채운다.</summary>
    static T LoadOrCreate<T>(string id, System.Action<T> fill) where T : DefinitionAsset
    {
        string path = $"{BattleDataDir}/{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<T>();
        asset.id = id;
        fill(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static GameObject CreateDreamcatcherPrefab(string id)
    {
        Sprite[] idle = LoadTowerFrames("dreamcatcher_idle", 7);
        Sprite[] attack = LoadTowerFrames("dreamcatcher_attack", 7);
        Sprite[] build = LoadTowerFrames("dreamcatcher_build", 2);
        if (idle == null || attack == null) return null;

        EnsureFolder($"{PrefabDir}/Towers");
        var root = new GameObject(id);
        // 고리 중심이 타일보다 살짝 위에 오도록 올린다(깃털이 타일 위로 늘어진다).
        var ringCenter = new Vector2(0f, 0.45f);
        GameObject visualGo = CreateSprite("Visual", idle[0], Color.white, root.transform, ringCenter, Vector2.one, 1);
        var visual = visualGo.AddComponent<TowerVisual>();
        visual.buildFrames = build ?? new Sprite[0];
        visual.idleFrames = idle;
        visual.attackFrames = attack;

        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(root.transform, false);
        firePoint.localPosition = ringCenter;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/Towers/{id}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
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

    // ───────── 스테이지·스토리 데이터 ─────────

    /// <summary>임시 스테이지 3개와 스토리 2개. 이미 있으면 그대로 두고, 전투 데이터가 비어 있는 스테이지만 채운다.</summary>
    static StageDatabase CreateStageData(StageAsset battle)
    {
        string databasePath = $"{ResourcesDir}/{StageDatabase.ResourcePath}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<StageDatabase>(databasePath);
        if (existing != null)
        {
            foreach (StageData stage in existing.stages)
            {
                if (stage == null || stage.battleStage != null) continue;
                stage.battleStage = battle;
                EditorUtility.SetDirty(stage);
            }
            return existing;
        }

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
            CreateStage("Stage_1-1", "1-1", "초원의 입구", intro, battle),
            CreateStage("Stage_1-2", "1-2", "어두운 숲길", null, battle),
            CreateStage("Stage_1-3", "1-3", "성문 앞 결전", gate, battle),
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

    static StageData CreateStage(string fileName, string id, string title, StoryData story, StageAsset battle)
    {
        var stage = ScriptableObject.CreateInstance<StageData>();
        stage.stageId = id;
        stage.title = title;
        stage.introStory = story;
        stage.battleStage = battle;
        AssetDatabase.CreateAsset(stage, $"{DataDir}/{fileName}.asset");
        return stage;
    }

    // ───────── 공통 ─────────

    static Sprite[] LoadTowerFrames(string prefix, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = LoadTowerSprite($"{prefix}_{i}.png");
            if (frames[i] == null) return null;
        }
        return frames;
    }

    static Sprite LoadTowerSprite(string fileName)
    {
        string path = $"{DreamcatcherDir}/{fileName}";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (TowerArtImporter.Apply(importer, path)) importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

/// <summary>
/// 프로젝트를 열었을 때 Title 또는 Battle 씬이 없으면(처음 열었거나 이전 버전에서 업데이트한 경우)
/// 데모 게임을 만들고 Title 씬을 연다.
/// </summary>
[InitializeOnLoad]
static class DemoSceneAutoSetup
{
    const string SessionKey = "DefenseGame.AutoSetupChecked.v4";

    static DemoSceneAutoSetup()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NeedsBuild()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            DemoSceneBuilder.Build();
        };
    }

    static bool NeedsBuild()
    {
        return !File.Exists(DemoSceneBuilder.TitleScenePath) || !File.Exists(DemoSceneBuilder.BattleScenePath);
    }
}
