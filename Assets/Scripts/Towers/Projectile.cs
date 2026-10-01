using UnityEngine;

/// <summary>
/// 타겟을 따라가 맞으면 피해를 준다. splashRadius가 있으면 범위 피해.
/// 스프라이트는 오른쪽(+x)을 앞으로 그리면 날아가는 방향으로 자동 회전한다.
/// </summary>
public class Projectile : MonoBehaviour
{
    Enemy target;
    Vector3 lastTargetPosition;
    float damage;
    float speed;
    float splashRadius;

    public void Launch(Enemy enemy, float hitDamage, float moveSpeed, float splash)
    {
        target = enemy;
        lastTargetPosition = enemy.transform.position;
        damage = hitDamage;
        speed = moveSpeed;
        splashRadius = splash;
    }

    void Update()
    {
        if (target != null && !target.IsDead) lastTargetPosition = target.transform.position;

        Vector3 direction = lastTargetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        transform.position = Vector3.MoveTowards(transform.position, lastTargetPosition, speed * Time.deltaTime);
        if ((transform.position - lastTargetPosition).sqrMagnitude < 0.01f) Hit();
    }

    void Hit()
    {
        if (splashRadius > 0f)
        {
            float radiusSqr = splashRadius * splashRadius;
            foreach (Enemy enemy in Enemy.Active.ToArray())
            {
                if ((enemy.transform.position - transform.position).sqrMagnitude <= radiusSqr)
                    enemy.TakeDamage(damage);
            }
        }
        else if (target != null && !target.IsDead)
        {
            target.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
