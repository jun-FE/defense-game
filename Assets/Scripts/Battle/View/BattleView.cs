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
        // 처음에는 가장 가까운 길 쪽을 바라본다.
        System.Numerics.Vector2 toPath = NearestPathPoint(tower.Position) - tower.Position;
        view.Face(new Vector2(toPath.X, toPath.Y));
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
        EnemyView target;
        enemyViews.TryGetValue(hit.Target, out target);
        Vector3 to = target != null ? target.transform.position : (Vector3)BattleContentBuilder.ToUnity(hit.Target.Position);
        towerView.PlayAttack(to);
        if (towerView.Melee)
        {
            // 찌르기는 투사체 없이 맞은 적이 번쩍인다(6프레임 중 4번째가 타격).
            if (target != null) target.Flash(0.15f);
            return;
        }
        ProjectileView.Spawn(transform, towerView.FirePoint, to, towerView.ProjectileSprite != null ? towerView.ProjectileSprite : circle, hit.Crit);
    }

    System.Numerics.Vector2 NearestPathPoint(System.Numerics.Vector2 from)
    {
        var best = from;
        float bestSq = float.MaxValue;
        foreach (SpawnPointDef spawn in controller.Session.Stage.Map.SpawnPoints)
            for (float d = 0f; d <= spawn.Length; d += 0.25f)
            {
                System.Numerics.Vector2 p = spawn.PointAt(d);
                float sq = System.Numerics.Vector2.DistanceSquared(p, from);
                if (sq < bestSq) { bestSq = sq; best = p; }
            }
        return best;
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
