using UnityEngine;

/// <summary>
/// 프롤로그 튜토리얼(새로하기). 기획: ArtSource/Prologue/프롤로그_화면구성.xlsx.
/// 꿈속 방(MapId) 탐색 → 전투(스탠드 조명만, 이길 수 있는 쉬운 두 웨이브, "전투 시작"을 눌러야 시작) →
/// 같은 방으로 돌아와 몽쇄 걸린 문 → 깨어나는 장면(EndStoryPath) → 수선소.
/// 화면마다 안내는 ExploreTutorial·BattleTutorial, 대사창·툴팁 모양은 DreamUI.
/// </summary>
public static class Tutorial
{
    const string DoneKey = "Tutorial.Done";
    public const string EndStoryPath = "Stories/prologue_wake";
    public const string MapId = "MAP_DREAM_ROOM";

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
