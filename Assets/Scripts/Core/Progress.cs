using UnityEngine;

/// <summary>스테이지 클리어 기록. 클리어한 다음 스테이지까지 열린다.</summary>
public static class Progress
{
    const string HighestClearedKey = "Progress.HighestCleared";

    public static int HighestCleared => PlayerPrefs.GetInt(HighestClearedKey, -1);

    public static bool IsCleared(int index) => index <= HighestCleared;

    public static bool IsUnlocked(int index) => index <= HighestCleared + 1;

    public static void MarkCleared(int index)
    {
        if (index <= HighestCleared) return;
        PlayerPrefs.SetInt(HighestClearedKey, index);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(HighestClearedKey);
        PlayerPrefs.Save();
    }
}
