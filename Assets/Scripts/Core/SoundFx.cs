using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 재생. SoundFx.Play("이름")이면 Assets/Resources/Sfx/이름.wav(또는 .ogg 등)를 튼다.
/// - 크기 = 설정의 효과음 크기(GameSettings.SfxVolume) × volume. 전체 크기는 AudioListener가 곱한다.
/// - 같은 소리는 minInterval초 안에 다시 울리지 않는다(여러 적이 한꺼번에 맞을 때 소리가 뭉개지지 않게).
/// - 소리마다 음높이를 살짝 흔들어(pitchJitter) 같은 소리가 반복돼도 덜 지루하게.
/// 지금 파일은 Tools/Audio/make_temp_sfx.py로 합성한 임시 소리. 같은 이름으로 바꿔 넣으면 그대로 쓰인다.
/// </summary>
public static class SoundFx
{
    const int Voices = 12;
    static AudioSource[] voices;
    static int nextVoice;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

    public static void Play(string name, float volume = 1f, float pitchJitter = 0.05f, float minInterval = 0.03f)
    {
        if (string.IsNullOrEmpty(name)) return;
        float now = Time.unscaledTime;
        float last;
        if (lastPlayed.TryGetValue(name, out last) && now - last < minInterval) return;

        AudioClip clip;
        if (!clips.TryGetValue(name, out clip))
        {
            clip = Resources.Load<AudioClip>("Sfx/" + name);
            clips[name] = clip;
            if (clip == null) Debug.LogWarning("[효과음] 없음: Resources/Sfx/" + name);
        }
        if (clip == null) return;
        lastPlayed[name] = now;

        EnsureVoices();
        AudioSource source = voices[nextVoice];
        nextVoice = (nextVoice + 1) % Voices;
        source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        source.PlayOneShot(clip, GameSettings.SfxVolume * volume);
    }

    static void EnsureVoices()
    {
        if (voices != null && voices[0] != null) return;
        var host = new GameObject("SoundFx");
        Object.DontDestroyOnLoad(host);
        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            voices[i] = host.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
        }
    }
}
