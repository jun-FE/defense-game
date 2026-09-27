using System.Collections.Generic;
using UnityEngine;

/// <summary>경로를 따라 이동하는 적. 끝에 도달하면 라이프를 깎고, 죽으면 골드를 준다.</summary>
public class Enemy : MonoBehaviour
{
    /// <summary>살아있는 모든 적. 타워가 타겟을 찾을 때 쓴다.</summary>
    public static readonly List<Enemy> Active = new List<Enemy>();

    public Transform healthFill;
    public float maxHealth = 5f;
    public float speed = 1.5f;
    public int reward = 5;
    public int damageToBase = 1;

    PathRoute path;
    int nextIndex;
    float health;
    float barWidth;

    /// <summary>경로를 따라 이동한 거리. 클수록 기지에 가깝다.</summary>
    public float Progress { get; private set; }
    public bool IsDead { get; private set; }

    void Awake()
    {
        if (healthFill != null) barWidth = healthFill.localScale.x;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    public void Setup(PathRoute route, float hp, float moveSpeed, int gold)
    {
        path = route;
        maxHealth = hp;
        health = hp;
        speed = moveSpeed;
        reward = gold;
        transform.position = route.GetPoint(0);
        nextIndex = 1;
        UpdateHealthBar();
    }

    void Update()
    {
        if (path == null || IsDead) return;

        Vector3 target = path.GetPoint(nextIndex);
        Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        Progress += Vector3.Distance(transform.position, next);
        transform.position = next;

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            nextIndex++;
            if (nextIndex >= path.Count) ReachBase();
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        health -= amount;
        UpdateHealthBar();
        if (health <= 0f)
        {
            IsDead = true;
            if (GameManager.Instance != null) GameManager.Instance.AddGold(reward);
            Destroy(gameObject);
        }
    }

    void ReachBase()
    {
        IsDead = true;
        if (GameManager.Instance != null) GameManager.Instance.LoseLife(damageToBase);
        Destroy(gameObject);
    }

    void UpdateHealthBar()
    {
        if (healthFill == null) return;
        float t = Mathf.Clamp01(health / maxHealth);
        Vector3 scale = healthFill.localScale;
        scale.x = barWidth * t;
        healthFill.localScale = scale;
        Vector3 pos = healthFill.localPosition;
        pos.x = -barWidth * (1f - t) * 0.5f;
        healthFill.localPosition = pos;
    }
}
