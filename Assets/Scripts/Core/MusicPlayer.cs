using UnityEngine;

/// <summary>
/// 배경음악 재생. 씬마다 Play("Music/파일이름")를 부르면 Resources에서 곡을 불러 반복 재생한다.
/// - 크기 = 설정의 배경음 크기(GameSettings.BgmVolume) × volume. 전체 크기는 AudioListener가 곱한다.
/// - 시작할 때 서서히 커지고(fadeIn초), 일시정지(Time.timeScale = 0)와 상관없이 계속 흐른다.
/// - 설정에서 배경음 크기를 바꾸면 곧바로 따라간다.
/// 곡 파일은 Assets/Resources/Music/ (.ogg 권장, .m4a는 Unity가 못 읽는 환경이 있어 변환해서 넣는다).
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    public float volume = 0.8f;
    public float fadeIn = 2f;

    AudioSource source;
    float fade;

    /// <summary>이 씬에 음악 재생기를 하나 만들고 곡을 튼다. 곡이 없으면 아무것도 안 한다.</summary>
    public static MusicPlayer Play(string resourcePath, float volume = 0.8f)
    {
        var clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogWarning("[음악] 곡을 찾을 수 없습니다: Resources/" + resourcePath);
            return null;
        }
        var go = new GameObject("Music");
        var player = go.AddComponent<MusicPlayer>();
        player.volume = volume;
        player.source = go.AddComponent<AudioSource>();
        player.source.clip = clip;
        player.source.loop = true;
        player.source.playOnAwake = false;
        player.source.spatialBlend = 0f;
        player.source.volume = 0f;
        player.source.Play();
        return player;
    }

    void Update()
    {
        if (source == null) return;
        fade = fadeIn <= 0f ? 1f : Mathf.Min(1f, fade + Time.unscaledDeltaTime / fadeIn);
        source.volume = GameSettings.BgmVolume * volume * fade;
    }
}
