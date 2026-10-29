using Akmong.Battle;
using UnityEngine;

/// <summary>설치된 타워의 화면 표시. 공격·강화 연출만 담당한다.</summary>
public class TowerView : MonoBehaviour
{
    public TowerState State { get; private set; }
    public Sprite ProjectileSprite { get; private set; }
    /// <summary>화려한 공격 연출(BattleFx)을 쓰는지와 그 스타일.</summary>
    public bool Fancy { get; private set; }
    TowerAsset asset;

    TowerVisual visual;
    Transform firePoint;
    GameRules rules;

    public Vector3 FirePoint
    {
        get
        {
            if (firePoint == null) return transform.position;
            Vector3 local = firePoint.localPosition;
            if (visual != null && visual.Flipped) local.x = -local.x; // 그림이 뒤집히면 바늘 끝도 반대편
            return transform.TransformPoint(local);
        }
    }

    public void Init(TowerState tower, TowerAsset asset, GameRules gameRules)
    {
        State = tower;
        rules = gameRules;
        ProjectileSprite = asset != null ? asset.projectile : null;
        this.asset = asset;
        Fancy = asset != null && asset.fancyShot && asset.fxGlow != null;
        visual = GetComponentInChildren<TowerVisual>();
        firePoint = transform.Find("FirePoint");
    }

    public BattleFx.Style FxStyle => new BattleFx.Style
    {
        needle = ProjectileSprite, spark = asset.fxSpark, ring = asset.fxRing, glow = asset.fxGlow, slash = asset.fxSlash,
        color = asset.fxColor, level = State.LevelIndex,
        scale = asset.fxKind == TowerFxKind.Pins ? 0.55f : asset.fxKind == TowerFxKind.Tag ? 0.85f : 1f,
    };

    public TowerFxKind FxKind => asset != null ? asset.fxKind : TowerFxKind.Needle;

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
        if (visual != null && visual.HasLevelArt(State.LevelIndex)) visual.SetLevel(State.LevelIndex);
        else transform.localScale = Vector3.one * (1f + 0.12f * State.LevelIndex);
    }
}
