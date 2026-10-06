using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 화면 흐름: 로비(첫 화면) → 메인(수선소) → 의뢰 → (스토리) → 전투 → 메인.
/// 어떤 스테이지를 플레이 중인지도 여기서 기억한다.
/// </summary>
public static class SceneFlow
{
    public const string LobbyScene = "Lobby";
    public const string MainScene = "Main";
    public const string StoryScene = "Story";
    public const string GameScene = "Battle";
    public const string MapEditorScene = "MapEditor";

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
            Load(GameScene);
        }
    }

    public static void FinishStory()
    {
        PendingStory = null;
        Load(GameScene);
    }

    public static void RestartStage() => StartStage(CurrentStageIndex, showStory: false);

    public static void StartNextStage() => StartStage(CurrentStageIndex + 1);

    /// <summary>로비(첫 화면: 이어하기·새로하기·설정·종료)로 간다.</summary>
    public static void GoToLobby() => Load(LobbyScene);

    /// <summary>개발자용 맵 에디터. 출시 빌드에서는 열리지 않는다.</summary>
    public static void GoToMapEditor()
    {
        if (DeveloperMode) Load(MapEditorScene);
    }

    /// <summary>메인(수선소)로 돌아간다.</summary>
    public static void GoToMain() => Load(MainScene);

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
        SceneManager.LoadScene(scene);
    }
}
