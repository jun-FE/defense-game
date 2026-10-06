using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 화면 흐름: 로비(첫 화면) → 메인(수선소) → 의뢰 → (스토리) → [탐색] → 전투 → 메인.
/// 스테이지에 탐색 맵(dreamMapId)이 있으면 전투 전에 탐색 화면(Explore)에서 암흑을 밝혀 길을 만든다.
/// 어떤 스테이지를 플레이 중인지도 여기서 기억한다.
/// </summary>
public static class SceneFlow
{
    public const string LobbyScene = "Lobby";
    public const string MainScene = "Main";
    public const string StoryScene = "Story";
    public const string GameScene = "Battle";
    public const string MapEditorScene = "MapEditor";
    public const string ExploreScene = "Explore";

    /// <summary>개발자 기능(맵 에디터 등)을 쓸 수 있는지: Unity 에디터나 개발 빌드에서만.</summary>
    public static bool DeveloperMode => Application.isEditor || Debug.isDebugBuild;

    public static int CurrentStageIndex { get; private set; }
    public static StageData CurrentStage => StageDatabase.Instance.Get(CurrentStageIndex);
    public static bool HasNextStage => CurrentStageIndex + 1 < StageDatabase.Instance.Count;

    /// <summary>재생할 스토리. 스토리 씬이 읽고 나면 비운다.</summary>
    public static StoryData PendingStory { get; private set; }

    public static void StartStage(int index, bool showStory = true)
    {
        CurrentStageIndex = index;
        StoryData story = CurrentStage != null ? CurrentStage.introStory : null;
        if (showStory && story != null && story.lines.Length > 0)
        {
            PendingStory = story;
            Load(StoryScene);
        }
        else
        {
            EnterStage();
        }
    }

    public static void FinishStory()
    {
        PendingStory = null;
        EnterStage();
    }

    /// <summary>스테이지에 탐색 맵이 있으면 탐색부터, 없으면 바로 전투.</summary>
    static void EnterStage()
    {
        DreamRun.Clear();
        string mapId = CurrentStage != null ? CurrentStage.dreamMapId : null;
        if (!string.IsNullOrEmpty(mapId))
        {
            Akmong.Battle.GridMap map = MapStorage.Load(mapId);
            if (map != null)
            {
                DreamRun.Begin(map, testMode: false);
                Load(ExploreScene);
                return;
            }
            Debug.LogError($"[탐색] 맵 '{mapId}'을 찾지 못해 전투 맵으로 바로 시작합니다. (Assets/Resources/Maps/{mapId}.json)");
        }
        Load(GameScene);
    }

    /// <summary>맵 에디터의 "테스트 플레이": 첫 스테이지의 전투 데이터(웨이브·타워)로 이 맵을 탐색 → 전투해 본다.</summary>
    public static void StartDreamTest(Akmong.Battle.GridMap map)
    {
        CurrentStageIndex = 0;
        DreamRun.Begin(map, testMode: true);
        Load(ExploreScene);
    }

    /// <summary>탐색을 마치고(출현 지점까지 길이 이어짐) 전투로.</summary>
    public static void FinishExploration() => Load(GameScene);

    /// <summary>다시 도전: 칸 맵 판이면 같은 맵을 처음부터 다시 탐색한다.</summary>
    public static void RestartStage()
    {
        if (DreamRun.Active)
        {
            DreamRun.Restart();
            Load(ExploreScene);
            return;
        }
        StartStage(CurrentStageIndex, showStory: false);
    }

    public static void StartNextStage() => StartStage(CurrentStageIndex + 1);

    /// <summary>로비(첫 화면: 이어하기·새로하기·설정·종료)로 간다.</summary>
    public static void GoToLobby()
    {
        DreamRun.Clear();
        Load(LobbyScene);
    }

    /// <summary>개발자용 맵 에디터. 출시 빌드에서는 열리지 않는다.</summary>
    public static void GoToMapEditor()
    {
        if (!DeveloperMode) return;
        DreamRun.Clear();
        Load(MapEditorScene);
    }

    /// <summary>메인(수선소)로 돌아간다.</summary>
    public static void GoToMain()
    {
        DreamRun.Clear();
        Load(MainScene);
    }

    /// <summary>의뢰를 수락하고 연결된 스테이지를 시작한다(스토리가 있으면 스토리부터).</summary>
    public static void StartQuest(QuestData quest)
    {
        QuestProgress.MarkAccepted(quest.questId);
        StartStage(quest.stageIndex);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void Load(string scene)
    {
        Time.timeScale = 1f;
        // 로비 "이어하기" 창의 마지막 저장 위치·시각
        if (scene == MainScene) SaveInfo.Touch("수선소");
        else if (scene == GameScene) SaveInfo.Touch(CurrentStage != null && !string.IsNullOrEmpty(CurrentStage.title) ? "꿈 속 · " + CurrentStage.title : "꿈 속");
        SceneManager.LoadScene(scene);
    }
}
