using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 화면 흐름: 타이틀 → 메인(수선소) → 의뢰 → (스토리) → 전투 → 수선소.
/// 어떤 스테이지를 플레이 중인지도 여기서 기억한다.
/// </summary>
public static class SceneFlow
{
    public const string TitleScene = "Title";
    public const string MainScene = "Main";
    public const string StoryScene = "Story";
    public const string GameScene = "Battle";

    public static int CurrentStageIndex { get; private set; }
    public static StageData CurrentStage => StageDatabase.Instance.Get(CurrentStageIndex);
    public static bool HasNextStage => CurrentStageIndex + 1 < StageDatabase.Instance.Count;

    /// <summary>재생할 스토리. 스토리 씬이 읽고 나면 비운다.</summary>
    public static StoryData PendingStory { get; private set; }

    /// <summary>타이틀로 돌아갈 때 스테이지 선택 화면을 바로 열지.</summary>
    public static bool OpenStageSelectOnTitle { get; set; }

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

    public static void GoToTitle() => Load(TitleScene);

    /// <summary>메인(수선소)로 돌아간다.</summary>
    public static void GoToMain() => Load(MainScene);

    /// <summary>의뢰를 수락하고 연결된 스테이지를 시작한다(스토리가 있으면 스토리부터).</summary>
    public static void StartQuest(QuestData quest)
    {
        QuestProgress.MarkAccepted(quest.questId);
        StartStage(quest.stageIndex);
    }

    public static void GoToStageSelect()
    {
        OpenStageSelectOnTitle = true;
        Load(TitleScene);
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
