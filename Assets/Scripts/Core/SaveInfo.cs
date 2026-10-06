using System;
using UnityEngine;

/// <summary>
/// 로비 "이어하기" 창(저장 기록)에 보여 줄 정보: 진행 중 의뢰, 마지막 저장 위치, 플레이 시간, 마지막 저장 시각.
/// 프로토타입은 PlayerPrefs. 화면을 옮길 때(SceneFlow) 위치와 시각을 적고, 플레이 시간은 백그라운드에서 센다.
/// </summary>
public static class SaveInfo
{
    const string LocationKey = "Save.LastLocation";
    const string TimeKey = "Save.LastTime";
    const string PlaySecondsKey = "Save.PlaySeconds";

    public static string LastLocation => PlayerPrefs.GetString(LocationKey, "수선소");

    /// <summary>마지막 저장 시각. 없으면 null.</summary>
    public static DateTime? LastSaved
    {
        get
        {
            long ticks;
            return long.TryParse(PlayerPrefs.GetString(TimeKey, ""), out ticks) ? new DateTime(ticks) : (DateTime?)null;
        }
    }

    public static double PlaySeconds => PlayerPrefs.GetFloat(PlaySecondsKey, 0f) + (tracker != null ? tracker.Pending : 0f);

    /// <summary>진행 중인 의뢰 제목(수락했고 아직 못 깬 첫 의뢰). 없으면 "없음".</summary>
    public static string CurrentQuestTitle
    {
        get
        {
            foreach (QuestData quest in QuestDatabase.Instance.quests)
                if (quest != null && quest.IsInProgress) return quest.title;
            return "없음";
        }
    }

    /// <summary>화면을 옮길 때 부른다. 진행 기록이 있을 때만 위치·시각을 남긴다.</summary>
    public static void Touch(string location)
    {
        PlayerPrefs.SetString(LocationKey, location);
        PlayerPrefs.SetString(TimeKey, DateTime.Now.Ticks.ToString());
        if (tracker != null) tracker.Flush();
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(LocationKey);
        PlayerPrefs.DeleteKey(TimeKey);
        PlayerPrefs.DeleteKey(PlaySecondsKey);
        if (tracker != null) tracker.Pending = 0f;
        PlayerPrefs.Save();
    }

    public static string FormatPlayTime(double seconds)
    {
        int minutes = (int)(seconds / 60.0);
        return $"{minutes / 60:00}시간 {minutes % 60:00}분";
    }

    // ───────── 플레이 시간 세기 ─────────

    static Tracker tracker;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartTracking()
    {
        if (tracker != null) return;
        var go = new GameObject("SaveInfo.PlayTime") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.Object.DontDestroyOnLoad(go);
        tracker = go.AddComponent<Tracker>();
    }

    /// <summary>로비가 아닌 화면에 있는 동안 실제 시간을 더한다. 30초마다, 그리고 끌 때 저장.</summary>
    class Tracker : MonoBehaviour
    {
        public float Pending;
        float sinceFlush;

        void Update()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneFlow.LobbyScene) return;
            Pending += Time.unscaledDeltaTime;
            sinceFlush += Time.unscaledDeltaTime;
            if (sinceFlush >= 30f) Flush();
        }

        public void Flush()
        {
            sinceFlush = 0f;
            if (Pending <= 0f) return;
            PlayerPrefs.SetFloat(PlaySecondsKey, PlayerPrefs.GetFloat(PlaySecondsKey, 0f) + Pending);
            Pending = 0f;
        }

        void OnApplicationQuit()
        {
            Flush();
            PlayerPrefs.Save();
        }
    }
}
