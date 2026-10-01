using Akmong.Battle;
using UnityEngine;

/// <summary>설치된 타워의 화면 표시. 공격·강화 연출만 담당한다.</summary>
public class TowerView : MonoBehaviour
{
    public TowerState State { get; private set; }
    public Sprite ProjectileSprite { get; private set; }

    TowerVisual visual;
    Transform firePoint;
    GameRules rules;

    public Vector3 FirePoint => firePoint != null ? firePoint.position : transform.position;

    public void Init(TowerState tower, TowerAsset asset, GameRules gameRules)
    {
        State = tower;
        rules = gameRules;
        ProjectileSprite = asset != null ? asset.projectile : null;
        visual = GetComponentInChildren<TowerVisual>();
        firePoint = transform.Find("FirePoint");
    }

    public void PlayAttack()
    {
        if (visual != null) visual.PlayAttack(BattleMath.AttackInterval(State.Level.AttackSec, rules));
    }

    /// <summary>2단계 아트가 나오기 전까지는 크기를 조금 키워 단계를 표시한다.</summary>
    public void OnUpgraded()
    {
        transform.localScale = Vector3.one * (1f + 0.12f * State.LevelIndex);
    }
}
