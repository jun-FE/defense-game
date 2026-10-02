using UnityEngine;

/// <summary>
/// 의뢰 수락 기록(프로토타입: PlayerPrefs). 실패·중도 귀환 후에도 "진행 중"으로 남는다.
/// 4주차에 JSON 저장(QuestService)으로 옮긴다.
/// </summary>
public static class QuestProgress
{
    const string Prefix = "Quest.Accepted.";
    const string KnownKey = "Quest.AcceptedList";

    public static bool IsAccepted(string questId) => PlayerPrefs.GetInt(Prefix + questId, 0) == 1;

    public static void MarkAccepted(string questId)
    {
        if (IsAccepted(questId)) return;
        PlayerPrefs.SetInt(Prefix + questId, 1);
        string known = PlayerPrefs.GetString(KnownKey, "");
        PlayerPrefs.SetString(KnownKey, known.Length == 0 ? questId : known + "," + questId);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        foreach (string id in PlayerPrefs.GetString(KnownKey, "").Split(','))
            if (id.Length > 0) PlayerPrefs.DeleteKey(Prefix + id);
        PlayerPrefs.DeleteKey(KnownKey);
        PlayerPrefs.Save();
    }
}
