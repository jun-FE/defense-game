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

    /// <summary>근거리 타워(저지)는 투사체 없이 찌르기 모션만 보인다.</summary>
    public bool Melee => State.Blocks;

    public void Face(Vector2 direction)
    {
        if (visual != null) visual.Face(direction);
    }

    public void PlayAttack(Vector3 target)
    {
        if (visual == null) return;
        visual.Face(target - transform.position);
        visual.PlayAttack(BattleMath.AttackInterval(State.Level.AttackSec, rules));
    }

    /// <summary>단계별 아트가 있으면 외형을 바꾸고, 없으면 크기를 조금 키워 단계를 표시한다.</summary>
    public void OnUpgraded()
    {
        if (visual != null && visual.levels != null && visual.levels.Length > State.LevelIndex) visual.SetLevel(State.LevelIndex);
        else transform.localScale = Vector3.one * (1f + 0.12f * State.LevelIndex);
    }
}
