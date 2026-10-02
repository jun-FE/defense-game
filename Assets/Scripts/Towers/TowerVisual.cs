using UnityEngine;

/// <summary>
/// 타워 스프라이트 애니메이션: 설치(1회) → 대기(반복), 공격 시 공격 애니메이션(1회) 후 대기로 돌아간다.
/// levels(단계별 4방향 프레임)가 있으면 그쪽을 쓴다: 대기는 첫 프레임, 공격은 바라보는 방향의 프레임 전체.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TowerVisual : MonoBehaviour
{
    [System.Serializable]
    public class DirectionalFrames
    {
        public Sprite[] up = new Sprite[0];
        public Sprite[] down = new Sprite[0];
        public Sprite[] left = new Sprite[0];
        public Sprite[] right = new Sprite[0];
    }

    [Tooltip("단계별 위·아래·좌·우 공격 프레임(병정인형처럼 4방향 아트가 있는 타워). 비어 있으면 아래 idle/attack을 쓴다.")]
    public DirectionalFrames[] levels = new DirectionalFrames[0];

    public Sprite[] buildFrames;
    public float buildFps = 6f;
    public Sprite[] idleFrames;
    public float idleFps = 8f;
    public Sprite[] attackFrames;
    public float attackFps = 14f;

    SpriteRenderer spriteRenderer;
    int level;
    Vector2 facing = Vector2.down;
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
        Sprite[] frames = Directional ? DirectionFrames() : attackFrames;
        if (frames == null || frames.Length == 0) return;
        float speed = maxDuration > 0f ? Mathf.Max(attackFps, frames.Length / maxDuration) : attackFps;
        Play(frames, speed, false);
    }

    bool Directional => levels != null && levels.Length > 0;

    /// <summary>강화 단계(0부터). 4방향 아트가 있으면 외형을 바꾼다.</summary>
    public void SetLevel(int index)
    {
        level = index;
        if (Directional) PlayIdle();
    }

    /// <summary>바라볼 방향(월드 좌표). 가로·세로 중 큰 쪽으로 4방향을 고른다.</summary>
    public void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < 1e-6f) return;
        facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
        if (Directional && loop) PlayIdle();
    }

    Sprite[] DirectionFrames()
    {
        DirectionalFrames set = levels[Mathf.Clamp(level, 0, levels.Length - 1)];
        if (facing.y > 0f) return set.up;
        if (facing.y < 0f) return set.down;
        return facing.x < 0f ? set.left : set.right;
    }

    void PlayIdle()
    {
        if (Directional)
        {
            Sprite[] frames = DirectionFrames();
            Play(frames != null && frames.Length > 0 ? new[] { frames[0] } : null, 1f, true);
            return;
        }
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
