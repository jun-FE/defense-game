using UnityEngine;

/// <summary>설정 값. PlayerPrefs에 저장되어 다음 실행에도 유지된다.</summary>
public static class GameSettings
{
    const string VolumeKey = "Settings.MasterVolume";
    const string TextSpeedKey = "Settings.TextSpeed";

    public static readonly string[] TextSpeedNames = { "느림", "보통", "빠름", "즉시" };
    static readonly float[] TextSpeedCharsPerSecond = { 20f, 40f, 80f, 0f };

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
        set
        {
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, volume);
            AudioListener.volume = volume;
        }
    }

    public static int TextSpeed
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(TextSpeedKey, 1), 0, TextSpeedNames.Length - 1);
        set => PlayerPrefs.SetInt(TextSpeedKey, Mathf.Clamp(value, 0, TextSpeedNames.Length - 1));
    }

    /// <summary>스토리 글자 출력 속도(초당 글자 수). 0이면 한 번에 표시.</summary>
    public static float TextCharsPerSecond => TextSpeedCharsPerSecond[TextSpeed];

    /// <summary>전체 화면 여부는 Unity가 알아서 저장한다.</summary>
    public static bool Fullscreen
    {
        get => Screen.fullScreen;
        set => Screen.fullScreen = value;
    }

    public static void Save() => PlayerPrefs.Save();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnStartup()
    {
        AudioListener.volume = MasterVolume;
    }
}
