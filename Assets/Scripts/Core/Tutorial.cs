using UnityEngine;

/// <summary>
/// 첫 수선 튜토리얼(새로하기 → 프롤로그 스토리 → 튜토리얼 한 판 → 마무리 스토리 → 수선소).
/// 기획: ArtSource/Story/프롤로그_스토리.docx 5장 "첫 수선 튜토리얼".
/// 달빛정원(첫 스테이지) 탐색 → 전투를 그대로 쓰되, 화면마다 안내(ExploreTutorial·BattleTutorial)를 띄우고
/// 전투 웨이브를 튜토리얼용으로 바꾼다(첫 웨이브는 쉽고, 마지막에 이길 수 없는 악몽 무리가 온다).
/// </summary>
public static class Tutorial
{
    const string DoneKey = "Tutorial.Done";
    public const string EndStoryPath = "Stories/tutorial_end";

    /// <summary>지금 튜토리얼 판을 하고 있는지.</summary>
    public static bool Active { get; private set; }

    public static bool Done => PlayerPrefs.GetInt(DoneKey, 0) == 1;

    public static void Begin() => Active = true;

    /// <summary>튜토리얼을 끝까지(또는 건너뛰기로) 마쳤다.</summary>
    public static void Complete()
    {
        Active = false;
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>중간에 수선소·로비로 나갔다(기록은 남기지 않음).</summary>
    public static void Stop() => Active = false;

    public static void ResetAll()
    {
        Active = false;
        PlayerPrefs.DeleteKey(DoneKey);
        PlayerPrefs.Save();
    }
}
