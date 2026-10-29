using UnityEngine;

/// <summary>
/// 화려한 공격 연출(재봉틀 등). 피해는 전투 코어에서 이미 계산되었고 여기서는 보이기만 한다.
/// - 발사 불꽃(MuzzleFlash): 발사 지점에서 빛이 터지고 작은 불티가 튄다.
/// - 바늘 투사체(NeedleShot): 빛나는 바늘이 날아가며 꿈실 꼬리(빛 점)를 남긴다.
/// - 명중 폭발(HitBurst): 빛 고리가 퍼지고 별 불티가 사방으로 튄다. 치명타·높은 단계일수록 크고 많다.
/// - 강화 반짝임(UpgradeSparkle): 타워 둘레로 별이 솟는다.
/// 수선소 물품 타워: 빛줄기(Beam, 스탠드 조명), 가위질(Snip, 가위), 줄자 휘감기(Whip, 줄자),
/// 시침핀·꼬리표 바늘은 바늘 투사체를 작게/색을 바꿔 쓴다(Style.scale·color).
/// 표식이 걸린 적 위의 작은 핀·꼬리표 표시는 EnemyView가 맡는다.
/// 스프라이트는 TowerAsset의 fx 항목, 가산(빛) 머티리얼은 BattleView.glowMaterial.
/// </summary>
public static class BattleFx
{
    public static Material Glow;

    public struct Style
    {
        public Sprite needle, spark, ring, glow, slash;
        public Color color;
        public int level;      // 0부터
        public float scale;    // 바늘 크기 배율(0이면 1)
    }

    /// <summary>스탠드 조명: 발사 지점에서 적까지 따뜻한 빛줄기가 번쩍였다 사라지고, 적 자리에 빛이 터진다.</summary>
    public static void Beam(Transform parent, Vector3 from, Vector3 to, Style s, bool crit)
    {
        float k = 1f + 0.25f * s.level + (crit ? 0.4f : 0f);
        Vector3 d = to - from;
        float len = d.magnitude;
        if (len < 0.01f) return;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Vector3 mid = (from + to) / 2f;
        // 굵은 겉빛 + 가는 속빛(가로로 길게 늘인 빛 점)
        Stretched(parent, s.glow, mid, angle, len * 1.15f, 0.5f * k, 0.22f, s.color, 8);
        Stretched(parent, s.glow, mid, angle, len * 1.05f, 0.18f * k, 0.16f, Color.white, 9);
        int dots = Mathf.Clamp(Mathf.RoundToInt(len * 3f), 4, 18);
        for (int i = 0; i < dots; i++)
        {
            Vector3 p = Vector3.Lerp(from, to, (i + Random.value) / dots);
            Particle(parent, s.spark, p, Random.insideUnitCircle * 0.6f, 0.2f * k, 0.04f, Random.Range(0.2f, 0.35f), Color.Lerp(Color.white, s.color, Random.value), 9, Random.Range(-300f, 300f));
        }
        Particle(parent, s.glow, from, Vector2.zero, 0.5f * k, 1.1f * k, 0.18f, Color.white, 9, 0f);
        // 맞은 자리: 고리 없이 따뜻한 빛과 불티만(빛줄기와 겹쳐 이중으로 보이지 않게)
        Particle(parent, s.glow, to, Vector2.zero, 0.35f * k, 1.1f * k, 0.2f, new Color(1f, 0.95f, 0.8f), 9, 0f);
        for (int i = 0; i < 4 + s.level * 2; i++)
            Particle(parent, s.spark, to, Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.4f), 0.18f, 0.04f, Random.Range(0.18f, 0.3f), Color.Lerp(Color.white, s.color, Random.value), 9, Random.Range(-400f, 400f));
    }

    /// <summary>
    /// 가위: 맞은 적마다 칼날 빛 두 줄기가 X자로 교차하며 싹둑. 첫 적이면 타워 둘레로 휘두른 빛 고리도.
    /// 단계가 오를수록 크고, 3단계는 칼날 빛이 하나 더(연달아 가위질).
    /// </summary>
    public static void Snip(Transform parent, Vector3 tower, Vector3 at, Style s, bool crit, bool first)
    {
        float k = 1f + 0.2f * s.level + (crit ? 0.35f : 0f);
        Sprite blade = s.slash != null ? s.slash : s.glow;
        float baseAngle = Random.Range(0f, 180f);
        int cuts = s.level >= 2 ? 3 : 2;
        for (int i = 0; i < cuts; i++)
        {
            float a = baseAngle + (i == 0 ? 35f : i == 1 ? -35f : 90f);
            Stretched(parent, blade, at, a, 1.3f * k, 0.22f * k, 0.18f + i * 0.03f, i % 2 == 0 ? Color.white : s.color, 9, i * 0.05f);
        }
        for (int i = 0; i < 4 + s.level * 2; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            Particle(parent, s.spark, at, dir * Random.Range(1.5f, 3.2f), 0.2f, 0.04f, Random.Range(0.2f, 0.32f), Color.Lerp(Color.white, s.color, Random.value), 9, Random.Range(-500f, 500f));
        }
        if (first)
        {
            Particle(parent, s.ring, tower, Vector2.zero, 0.3f, 2.4f * k, 0.28f, new Color(s.color.r, s.color.g, s.color.b, 0.8f), 7, Random.Range(-600f, 600f));
            if (s.level >= 2) Particle(parent, s.ring, tower, Vector2.zero, 0.2f, 3.0f * k, 0.36f, new Color(1f, 0.9f, 0.6f, 0.6f), 7, Random.Range(-600f, 600f));
        }
    }

    /// <summary>줄자: 발사 지점에서 적까지 줄자가 휘어지며 휙 뻗었다(앞에서부터 차례로 나타남) 사라지고, 적을 감는 고리.</summary>
    public static void Whip(Transform parent, Vector3 from, Vector3 to, Style s, bool crit)
    {
        Vector3 d = to - from;
        Vector3 normal = new Vector3(-d.y, d.x, 0f).normalized * Mathf.Min(1.2f, d.magnitude * 0.35f) * (Random.value < 0.5f ? 1f : -1f);
        Vector3 control = (from + to) / 2f + normal;
        int dots = 16;
        var tape = new Color(1f, 0.93f, 0.75f);
        for (int i = 0; i <= dots; i++)
        {
            float t = i / (float)dots;
            Vector3 p = (1 - t) * (1 - t) * from + 2 * (1 - t) * t * control + t * t * to;
            Particle(parent, s.glow, p, Vector2.zero, 0.32f, 0.12f, 0.28f, i % 4 == 0 ? s.color : tape, 8, 0f, t * 0.12f);
        }
        Particle(parent, s.ring, to, Vector2.zero, 1.4f, 0.5f, 0.35f, tape, 9, 720f, 0.12f);
        Particle(parent, s.ring, to, Vector2.zero, 1.0f, 0.3f, 0.4f, s.color, 9, -540f, 0.14f);
        for (int i = 0; i < 5; i++)
            Particle(parent, s.spark, to, Random.insideUnitCircle * 1.6f, 0.18f, 0.04f, 0.3f, tape, 9, Random.Range(-300f, 300f), 0.12f);
    }

    /// <summary>가로로 길게 늘인 빛(빛줄기·칼날). length·thickness는 월드 크기.</summary>
    public static void Stretched(Transform parent, Sprite sprite, Vector3 at, float angle, float length, float thickness, float life, Color color, int order, float delay = 0f)
    {
        if (sprite == null) return;
        Vector2 size = sprite.bounds.size;
        Particle(parent, sprite, at, Vector2.zero, 1f, 1f, life, color, order, 0f, delay, new Vector2(length / Mathf.Max(0.01f, size.x), thickness / Mathf.Max(0.01f, size.y)), angle);
    }

    public static void MuzzleFlash(Transform parent, Vector3 at, Style s, bool crit)
    {
        float k = 1f + 0.25f * s.level + (crit ? 0.4f : 0f);
        Particle(parent, s.glow, at, Vector2.zero, 0.55f * k, 1.2f * k, 0.16f, Color.white, 9, 0f);
        Particle(parent, s.glow, at, Vector2.zero, 0.8f * k, 0.2f, 0.22f, s.color, 8, 0f);
        int sparks = 3 + s.level * 2;
        for (int i = 0; i < sparks; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            Particle(parent, s.spark, at, dir * Random.Range(1.5f, 3f), 0.22f, 0.05f, Random.Range(0.18f, 0.3f), Color.Lerp(Color.white, s.color, Random.value), 9, Random.Range(-360f, 360f));
        }
    }

    public static void NeedleShot(Transform parent, Vector3 from, Vector3 to, Style s, bool crit)
    {
        var go = new GameObject("NeedleShot");
        go.transform.SetParent(parent, false);
        go.transform.position = from;
        go.transform.localScale = Vector3.one * (0.55f + 0.08f * s.level) * (crit ? 1.3f : 1f) * (s.scale > 0f ? s.scale : 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = s.needle;
        renderer.sortingOrder = 8;
        var shot = go.AddComponent<NeedleShotView>();
        shot.target = to;
        shot.style = s;
        shot.crit = crit;

        // 바늘 둘레의 빛
        var halo = new GameObject("Halo");
        halo.transform.SetParent(go.transform, false);
        halo.transform.localScale = Vector3.one * 1.6f;
        var haloRenderer = halo.AddComponent<SpriteRenderer>();
        haloRenderer.sprite = s.glow;
        haloRenderer.color = new Color(s.color.r, s.color.g, s.color.b, 0.75f);
        haloRenderer.sortingOrder = 7;
        if (Glow != null) haloRenderer.sharedMaterial = Glow;
    }

    public static void HitBurst(Transform parent, Vector3 at, Style s, bool crit)
    {
        float k = 1f + 0.3f * s.level + (crit ? 0.5f : 0f);
        Particle(parent, s.glow, at, Vector2.zero, 0.4f * k, 1.5f * k, 0.2f, Color.white, 9, 0f);
        Particle(parent, s.ring, at, Vector2.zero, 0.2f, 1.6f * k, 0.32f, s.color, 9, Random.Range(-200f, 200f));
        if (s.level >= 1 || crit)
            Particle(parent, s.ring, at, Vector2.zero, 0.1f, 2.4f * k, 0.45f, new Color(1f, 0.9f, 0.6f), 9, Random.Range(-160f, 160f));
        int sparks = 6 + s.level * 4 + (crit ? 6 : 0);
        for (int i = 0; i < sparks; i++)
        {
            float angle = (i + Random.value * 0.6f) / sparks * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Color c = i % 3 == 0 ? Color.white : i % 3 == 1 ? s.color : new Color(1f, 0.85f, 0.55f);
            Particle(parent, s.spark, at, dir * Random.Range(2.5f, 4.5f) * (0.8f + 0.15f * s.level), 0.3f * (crit ? 1.3f : 1f), 0.05f,
                Random.Range(0.25f, 0.45f), c, 9, Random.Range(-540f, 540f));
        }
    }

    public static void UpgradeSparkle(Transform parent, Vector3 at, Style s)
    {
        Particle(parent, s.ring, at, Vector2.zero, 0.3f, 2.6f, 0.5f, s.color, 9, Random.Range(-120f, 120f));
        Particle(parent, s.glow, at + Vector3.up * 0.3f, Vector2.zero, 0.6f, 2.2f, 0.45f, new Color(1f, 0.9f, 0.6f), 9, 0f);
        for (int i = 0; i < 16; i++)
        {
            Vector3 start = at + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.4f, 0.2f), 0f);
            Particle(parent, s.spark, start, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(1.8f, 3.2f)), 0.28f, 0.08f,
                Random.Range(0.5f, 0.8f), i % 2 == 0 ? s.color : new Color(1f, 0.9f, 0.6f), 9, Random.Range(-360f, 360f));
        }
    }

    /// <summary>날아가며 커지거나 줄고 사라지는 빛 조각 하나.</summary>
    public static void Particle(Transform parent, Sprite sprite, Vector3 at, Vector2 velocity, float startSize, float endSize, float life, Color color, int order, float spin,
        float delay = 0f, Vector2 stretch = default(Vector2), float angle = float.NaN)
    {
        if (sprite == null) return;
        var go = new GameObject("Fx");
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        // 각도를 안 주면 고리·불티가 매번 다른 모양으로 보이게 아무렇게나 돌린다
        go.transform.rotation = Quaternion.Euler(0f, 0f, float.IsNaN(angle) ? Random.Range(0f, 360f) : angle);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (Glow != null) renderer.sharedMaterial = Glow;
        var p = go.AddComponent<FxParticle>();
        p.velocity = velocity;
        p.startSize = startSize;
        p.endSize = endSize;
        p.life = life;
        p.color = color;
        p.spin = spin;
        p.delay = delay;
        p.stretch = stretch == default(Vector2) ? Vector2.one : stretch;
        p.Tick(0f);
    }
}

/// <summary>BattleFx 조각 하나의 움직임(속도·감속·크기·투명도·회전).</summary>
public class FxParticle : MonoBehaviour
{
    public Vector2 velocity;
    public float startSize, endSize, life, spin;
    /// <summary>이만큼(초) 기다렸다가 나타난다(줄자가 앞에서부터 뻗는 연출).</summary>
    public float delay;
    /// <summary>가로·세로 배율(빛줄기·칼날처럼 길게 늘인 빛).</summary>
    public Vector2 stretch = Vector2.one;
    public Color color;
    float age;
    SpriteRenderer spriteRenderer;

    void Update() => Tick(Time.deltaTime);

    public void Tick(float dt)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (delay > 0f)
        {
            delay -= dt;
            spriteRenderer.enabled = false;
            if (delay > 0f) return;
            spriteRenderer.enabled = true;
        }
        age += dt;
        float t = Mathf.Clamp01(age / Mathf.Max(0.01f, life));
        transform.position += (Vector3)(velocity * dt);
        velocity *= Mathf.Exp(-4f * dt);
        transform.Rotate(0f, 0f, spin * dt);
        float size = Mathf.Lerp(startSize, endSize, 1f - (1f - t) * (1f - t));
        transform.localScale = new Vector3(size * stretch.x, size * stretch.y, 1f);
        Color c = color;
        c.a = color.a * (1f - t * t);
        spriteRenderer.color = c;
        if (age >= life) Destroy(gameObject);
    }
}

/// <summary>바늘 투사체: 빠르게 날아가며 꿈실 꼬리를 남기고, 닿으면 명중 폭발.</summary>
public class NeedleShotView : MonoBehaviour
{
    const float Speed = 17f;
    public Vector3 target;
    public BattleFx.Style style;
    public bool crit;
    float trailTimer;

    void Update()
    {
        Vector3 direction = target - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);

        trailTimer -= Time.deltaTime;
        if (trailTimer <= 0f)
        {
            trailTimer = 0.012f;
            Color c = Color.Lerp(style.color, Color.white, Random.value * 0.4f);
            BattleFx.Particle(transform.parent, style.glow, transform.position, Random.insideUnitCircle * 0.3f,
                0.28f + 0.04f * style.level, 0.05f, 0.28f + 0.05f * style.level, c, 7, 0f);
        }

        if ((transform.position - target).sqrMagnitude < 0.0004f)
        {
            BattleFx.HitBurst(transform.parent, target, style, crit);
            Destroy(gameObject);
        }
    }
}
