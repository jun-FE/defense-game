using UnityEngine;

/// <summary>사거리 안에서 기지에 가장 가까운 적을 노리고 투사체를 쏜다.</summary>
public class Tower : MonoBehaviour
{
    public string displayName = "기본 타워";
    public int cost = 50;

    [Header("공격")]
    public float range = 2.5f;
    public float fireRate = 2f;
    public float damage = 1f;
    public float splashRadius = 0f;
    public float projectileSpeed = 8f;

    [Header("참조")]
    public Transform head;
    public Projectile projectilePrefab;

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
        Vector3 origin = head != null ? head.position : transform.position;
        Projectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(target, damage, projectileSpeed, splashRadius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
