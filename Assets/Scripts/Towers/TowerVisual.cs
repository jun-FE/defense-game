using UnityEngine;

/// <summary>
/// 타워 스프라이트 애니메이션: 설치(1회) → 대기(반복), 공격 시 공격 애니메이션(1회) 후 대기로 돌아간다.
/// levels(단계별 4방향 프레임)가 있으면 그쪽을 쓴다: 대기는 첫 프레임, 공격은 바라보는 방향의 프레임 전체.
/// stages(단계별 대기·공격 프레임, 한 방향 그림)가 있으면 그쪽을 쓴다: 대기 프레임을 반복하고, 좌우는 뒤집어서 바라본다(재봉틀).
/// lively를 켜면 대기 중 숨쉬듯 움직이고, 공격 때 반동(눌렸다 튀어 오름), 강화 때 통통 튀는 연출을 더한다.
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

    [System.Serializable]
    public class StageFrames
    {
        public Sprite[] idle = new Sprite[0];
        public Sprite[] attack = new Sprite[0];
    }

    [Tooltip("단계별 대기(반복)·공격 프레임(한 방향 그림). 그림이 왼쪽을 보고 있으면 오른쪽 대상은 좌우를 뒤집는다.")]
    public StageFrames[] stages = new StageFrames[0];
    [Tooltip("stages 그림이 기본으로 보는 방향이 왼쪽이면 켠다(오른쪽을 볼 때 뒤집음).")]
    public bool artFacesLeft = true;
    [Tooltip("대기 숨쉬기·공격 반동·강화 통통 연출")]
    public bool lively;

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
    Vector3 baseScale = Vector3.one, basePosition;
    float kick = -1f, pop = -1f, breathPhase;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
        breathPhase = Random.value * 6.28f;
        if (buildFrames != null && buildFrames.Length > 0) Play(buildFrames, buildFps, false);
        else PlayIdle();
    }

    /// <summary>공격 애니메이션을 처음부터 재생한다. maxDuration 안에 끝나도록 속도를 맞춘다.</summary>
    public void PlayAttack(float maxDuration)
    {
        Sprite[] frames = Staged ? CurrentStage.attack : Directional ? DirectionFrames() : attackFrames;
        kick = 0f;
        if (frames == null || frames.Length == 0) return;
        float speed = maxDuration > 0f ? Mathf.Max(attackFps, frames.Length / maxDuration) : attackFps;
        Play(frames, speed, false);
    }

    bool Directional => levels != null && levels.Length > 0;
    bool Staged => stages != null && stages.Length > 0;
    /// <summary>그림을 좌우로 뒤집어 그리는 중인지(발사 지점도 같이 뒤집는다).</summary>
    public bool Flipped => spriteRenderer != null && spriteRenderer.flipX;
    StageFrames CurrentStage => stages[Mathf.Clamp(level, 0, stages.Length - 1)];

    /// <summary>이 단계의 전용 그림이 있는지(없으면 TowerView가 크기를 키워 단계를 표시).</summary>
    public bool HasLevelArt(int index) => (Staged && stages.Length > index) || (Directional && levels.Length > index);

    /// <summary>강화 단계(0부터). 4방향 아트가 있으면 외형을 바꾼다.</summary>
    public void SetLevel(int index)
    {
        level = index;
        pop = 0f;
        if (Directional || Staged) PlayIdle();
    }

    /// <summary>바라볼 방향(월드 좌표). 가로·세로 중 큰 쪽으로 4방향을 고른다.</summary>
    public void Face(Vector2 direction)
    {
        if (direction.sqrMagnitude < 1e-6f) return;
        if (Staged && Mathf.Abs(direction.x) > 0.05f)
            spriteRenderer.flipX = artFacesLeft ? direction.x > 0f : direction.x < 0f;
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
        if (Staged)
        {
            Play(CurrentStage.idle, idleFps, true);
            return;
        }
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
        if (lively) Animate();
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

    /// <summary>숨쉬기(대기) + 반동(공격 직후 0.3초) + 통통(강화 직후 0.5초). 바닥(아래쪽)을 고정한 채 늘이고 줄인다.</summary>
    void Animate()
    {
        float t = Time.time;
        float sx = 1f + 0.012f * Mathf.Sin(t * 2.2f + breathPhase + 1.3f);
        float sy = 1f + 0.02f * Mathf.Sin(t * 2.2f + breathPhase);
        float shake = 0f;
        if (kick >= 0f)
        {
            kick += Time.deltaTime;
            float k = Mathf.Clamp01(kick / 0.3f);
            float punch = Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k);   // 눌렸다가 튀어 오르며 잦아듦
            sx *= 1f + 0.12f * punch;
            sy *= 1f - 0.14f * punch;
            shake = (1f - k) * 0.03f * Mathf.Sin(kick * 90f);
            if (kick > 0.3f) kick = -1f;
        }
        if (pop >= 0f)
        {
            pop += Time.deltaTime;
            float k = Mathf.Clamp01(pop / 0.5f);
            float bounce = 1f + 0.3f * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
            sx *= bounce;
            sy *= bounce;
            if (pop > 0.5f) pop = -1f;
        }
        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        // 그림 아래쪽(받침)이 제자리에 있도록 위로 늘어난 만큼 올린다.
        float height = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.size.y * baseScale.y : 0f;
        transform.localPosition = basePosition + new Vector3(shake, height * (sy - 1f) * 0.5f, 0f);
    }

    void Show(int frame)
    {
        if (current != null && frame < current.Length) spriteRenderer.sprite = current[frame];
    }
}
