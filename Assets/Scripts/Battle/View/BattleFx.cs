using UnityEngine;

/// <summary>
/// 화려한 공격 연출(재봉틀 등). 피해는 전투 코어에서 이미 계산되었고 여기서는 보이기만 한다.
/// - 발사 불꽃(MuzzleFlash): 발사 지점에서 빛이 터지고 작은 불티가 튄다.
/// - 바늘 투사체(NeedleShot): 빛나는 바늘이 날아가며 꿈실 꼬리(빛 점)를 남긴다.
/// - 명중 폭발(HitBurst): 빛 고리가 퍼지고 별 불티가 사방으로 튄다. 치명타·높은 단계일수록 크고 많다.
/// - 강화 반짝임(UpgradeSparkle): 타워 둘레로 별이 솟는다.
/// 스프라이트는 TowerAsset의 fx 항목, 가산(빛) 머티리얼은 BattleView.glowMaterial.
/// </summary>
public static class BattleFx
{
    public static Material Glow;

    public struct Style
    {
        public Sprite needle, spark, ring, glow;
        public Color color;
        public int level;      // 0부터
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
        go.transform.localScale = Vector3.one * (0.55f + 0.08f * s.level) * (crit ? 1.3f : 1f);
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
        Particle(parent, s.ring, at, Vector2.zero, 0.2f, 1.6f * k, 0.32f, s.color, 9, 0f);
        if (s.level >= 1 || crit)
            Particle(parent, s.ring, at, Vector2.zero, 0.1f, 2.4f * k, 0.45f, new Color(1f, 0.9f, 0.6f), 9, 0f);
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
        Particle(parent, s.ring, at, Vector2.zero, 0.3f, 2.6f, 0.5f, s.color, 9, 0f);
        Particle(parent, s.glow, at + Vector3.up * 0.3f, Vector2.zero, 0.6f, 2.2f, 0.45f, new Color(1f, 0.9f, 0.6f), 9, 0f);
        for (int i = 0; i < 16; i++)
        {
            Vector3 start = at + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.4f, 0.2f), 0f);
            Particle(parent, s.spark, start, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(1.8f, 3.2f)), 0.28f, 0.08f,
                Random.Range(0.5f, 0.8f), i % 2 == 0 ? s.color : new Color(1f, 0.9f, 0.6f), 9, Random.Range(-360f, 360f));
        }
    }

    /// <summary>날아가며 커지거나 줄고 사라지는 빛 조각 하나.</summary>
    public static void Particle(Transform parent, Sprite sprite, Vector3 at, Vector2 velocity, float startSize, float endSize, float life, Color color, int order, float spin)
    {
        if (sprite == null) return;
        var go = new GameObject("Fx");
        go.transform.SetParent(parent, false);
        go.transform.position = at;
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
        p.Tick(0f);
    }
}

/// <summary>BattleFx 조각 하나의 움직임(속도·감속·크기·투명도·회전).</summary>
public class FxParticle : MonoBehaviour
{
    public Vector2 velocity;
    public float startSize, endSize, life, spin;
    public Color color;
    float age;
    SpriteRenderer spriteRenderer;

    void Update() => Tick(Time.deltaTime);

    public void Tick(float dt)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        age += dt;
        float t = Mathf.Clamp01(age / Mathf.Max(0.01f, life));
        transform.position += (Vector3)(velocity * dt);
        velocity *= Mathf.Exp(-4f * dt);
        transform.Rotate(0f, 0f, spin * dt);
        float size = Mathf.Lerp(startSize, endSize, 1f - (1f - t) * (1f - t));
        transform.localScale = Vector3.one * size;
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
