using UnityEngine;

/// <summary>사거리 안에서 기지에 가장 가까운 적을 노리고 투사체를 쏜다.</summary>
public class Tower : MonoBehaviour
{
    public string displayName = "기본 타워";
    public int cost = 50;
    [Tooltip("하단 건설 버튼 아이콘 (골드가 충분할 때 / 부족할 때)")]
    public Sprite icon;
    public Sprite iconDisabled;

    [Header("공격")]
    public float range = 2.5f;
    public float fireRate = 2f;
    public float damage = 1f;
    public float splashRadius = 0f;
    public float projectileSpeed = 8f;

    [Header("참조")]
    [Tooltip("타겟 쪽으로 회전하는 포신 (없으면 회전 안 함)")]
    public Transform head;
    [Tooltip("투사체가 나가는 위치 (없으면 head, 그것도 없으면 타워 중심)")]
    public Transform firePoint;
    public Projectile projectilePrefab;
    [Tooltip("스프라이트 애니메이션 (없으면 생략)")]
    public TowerVisual visual;

    Enemy target;
    float cooldown;

    void Update()
    {
        if (target == null || target.IsDead || !InRange(target)) target = FindTarget();
        cooldown -= Time.deltaTime;
        if (target == null) return;

        if (head != null)
        {
            Vector2 dir = target.transform.position - head.position;
            head.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        if (cooldown <= 0f)
        {
            cooldown = 1f / fireRate;
            Fire();
        }
    }

    Enemy FindTarget()
    {
        Enemy best = null;
        foreach (Enemy enemy in Enemy.Active)
        {
            if (enemy.IsDead || !InRange(enemy)) continue;
            if (best == null || enemy.Progress > best.Progress) best = enemy;
        }
        return best;
    }

    bool InRange(Enemy enemy)
    {
        return (enemy.transform.position - transform.position).sqrMagnitude <= range * range;
    }

    void Fire()
    {
        if (visual != null) visual.PlayAttack(1f / fireRate);
        Vector3 origin = firePoint != null ? firePoint.position : head != null ? head.position : transform.position;
        Projectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(target, damage, projectileSpeed, splashRadius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
