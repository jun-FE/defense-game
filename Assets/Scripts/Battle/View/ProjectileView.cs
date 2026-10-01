using UnityEngine;

/// <summary>
/// 연출용 투사체. 피해는 발사 순간 전투 코어에서 이미 계산되었고(기획서: 즉시 명중 방식),
/// 이 오브젝트는 날아가는 모습만 보여준다.
/// </summary>
public class ProjectileView : MonoBehaviour
{
    const float Speed = 14f;

    Vector3 target;

    public static void Spawn(Transform parent, Vector3 from, Vector3 to, Sprite sprite, bool crit)
    {
        var go = new GameObject("Projectile");
        go.transform.SetParent(parent, false);
        go.transform.position = from;
        go.transform.localScale = Vector3.one * (crit ? 1.35f : 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 6;
        if (crit) renderer.color = new Color(1f, 0.85f, 0.5f);
        go.AddComponent<ProjectileView>().target = to;
    }

    void Update()
    {
        Vector3 direction = target - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude < 0.0004f) Destroy(gameObject);
    }
}
