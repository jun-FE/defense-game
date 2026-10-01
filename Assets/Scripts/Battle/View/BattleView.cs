using System.Collections;
using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 전투 사건(생성·공격·처치·누수·건설)을 받아 화면 오브젝트를 만들고 움직인다.
/// 게임 규칙은 바꾸지 않는다(UI는 결과를 구독만 한다).
/// </summary>
public class BattleView : MonoBehaviour
{
    public BattleController controller;
    public MapView mapView;
    public Sprite square;
    public Sprite circle;

    readonly Dictionary<EnemyState, EnemyView> enemyViews = new Dictionary<EnemyState, EnemyView>();
    readonly Dictionary<TowerState, TowerView> towerViews = new Dictionary<TowerState, TowerView>();

    void Start()
    {
        BattleSession session = controller.Session;
        if (session == null) return;
        session.EnemySpawned += OnEnemySpawned;
        session.EnemyKilled += OnEnemyKilled;
        session.EnemyReachedCore += OnEnemyReachedCore;
        session.TowerBuilt += OnTowerBuilt;
        session.TowerUpgraded += OnTowerUpgraded;
        session.TowerFired += OnTowerFired;
    }

    void LateUpdate()
    {
        float alpha = controller.Alpha;
        foreach (KeyValuePair<EnemyState, EnemyView> pair in enemyViews) pair.Value.Sync(alpha);
    }

    public TowerView FindTowerView(TowerState tower)
    {
        TowerView view;
        return towerViews.TryGetValue(tower, out view) ? view : null;
    }

    void OnEnemySpawned(EnemyState enemy)
    {
        EnemyAsset asset;
        controller.Content.Enemies.TryGetValue(enemy.Def, out asset);
        var go = new GameObject("Enemy " + enemy.Def.Id + " #" + enemy.EntityId);
        go.transform.SetParent(transform, false);
        EnemyView view = go.AddComponent<EnemyView>();
        view.Init(enemy, asset, circle, square);
        enemyViews[enemy] = view;
    }

    void OnEnemyKilled(EnemyState enemy) => RemoveEnemy(enemy, true);

    void OnEnemyReachedCore(EnemyState enemy, int damage)
    {
        RemoveEnemy(enemy, false);
        if (mapView != null && mapView.CoreTransform != null) StartCoroutine(Flash(mapView.CoreTransform));
    }

    void RemoveEnemy(EnemyState enemy, bool killed)
    {
        EnemyView view;
        if (!enemyViews.TryGetValue(enemy, out view)) return;
        enemyViews.Remove(enemy);
        view.Finish(killed);
    }

    void OnTowerBuilt(TowerState tower)
    {
        TowerAsset asset;
        controller.Content.Towers.TryGetValue(tower.Def, out asset);
        GameObject go = asset != null && asset.prefab != null
            ? Instantiate(asset.prefab, transform)
            : CreateFallbackTower();
        go.name = tower.Def.Id + " #" + tower.InstanceId;
        go.transform.position = new Vector3(tower.TileX, tower.TileY, 0f);
        TowerView view = go.AddComponent<TowerView>();
        view.Init(tower, asset, controller.Content.Rules);
        towerViews[tower] = view;
    }

    void OnTowerUpgraded(TowerState tower)
    {
        TowerView view = FindTowerView(tower);
        if (view != null) view.OnUpgraded();
    }

    void OnTowerFired(HitResult hit)
    {
        TowerView towerView = FindTowerView(hit.Tower);
        if (towerView == null) return;
        towerView.PlayAttack();

        EnemyView target;
        enemyViews.TryGetValue(hit.Target, out target);
        Vector3 to = target != null ? target.transform.position : (Vector3)BattleContentBuilder.ToUnity(hit.Target.Position);
        ProjectileView.Spawn(transform, towerView.FirePoint, to, towerView.ProjectileSprite != null ? towerView.ProjectileSprite : circle, hit.Crit);
    }

    GameObject CreateFallbackTower()
    {
        var go = new GameObject();
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(0.4f, 0.6f, 0.95f);
        renderer.sortingOrder = 1;
        go.transform.localScale = Vector3.one * 0.8f;
        return go;
    }

    static IEnumerator Flash(Transform target)
    {
        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;
        Color original = renderer.color;
        renderer.color = new Color(1f, 0.3f, 0.3f);
        yield return new WaitForSeconds(0.15f);
        renderer.color = original;
    }
}
