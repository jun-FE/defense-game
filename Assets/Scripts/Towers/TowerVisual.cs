using UnityEngine;

/// <summary>
/// 타워 스프라이트 애니메이션: 설치(1회) → 대기(반복), 공격 시 공격 애니메이션(1회) 후 대기로 돌아간다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TowerVisual : MonoBehaviour
{
    public Sprite[] buildFrames;
    public float buildFps = 6f;
    public Sprite[] idleFrames;
    public float idleFps = 8f;
    public Sprite[] attackFrames;
    public float attackFps = 14f;

    SpriteRenderer spriteRenderer;
    Sprite[] current;
    float fps;
    bool loop;
    float time;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (buildFrames != null && buildFrames.Length > 0) Play(buildFrames, buildFps, false);
        else PlayIdle();
    }

    /// <summary>공격 애니메이션을 처음부터 재생한다. maxDuration 안에 끝나도록 속도를 맞춘다.</summary>
    public void PlayAttack(float maxDuration)
    {
        if (attackFrames == null || attackFrames.Length == 0) return;
        float speed = maxDuration > 0f ? Mathf.Max(attackFps, attackFrames.Length / maxDuration) : attackFps;
        Play(attackFrames, speed, false);
    }

    void PlayIdle()
    {
        Play(idleFrames, idleFps, true);
        // 여러 타워가 똑같이 흔들리지 않도록 시작 프레임을 흩뜨린다.
        if (idleFrames != null && idleFrames.Length > 0) time = Random.value * idleFrames.Length / idleFps;
    }

    void Play(Sprite[] frames, float framesPerSecond, bool looping)
    {
        current = frames;
        fps = framesPerSecond;
        loop = looping;
        time = 0f;
        Show(0);
    }

    void Update()
    {
        if (current == null || current.Length == 0) return;
        time += Time.deltaTime;
        int frame = Mathf.FloorToInt(time * fps);
        if (frame >= current.Length)
        {
            if (!loop)
            {
                PlayIdle();
                return;
            }
            frame %= current.Length;
        }
        Show(frame);
    }

    void Show(int frame)
    {
        if (current != null && frame < current.Length) spriteRenderer.sprite = current[frame];
    }
}
