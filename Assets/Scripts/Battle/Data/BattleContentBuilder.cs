using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;
using NVector2 = System.Numerics.Vector2;

/// <summary>ScriptableObject 데이터를 전투 코어가 읽는 정의로 바꾸고 검사한 결과.</summary>
public class BattleContent
{
    public StageDef Stage;
    public GameRules Rules;
    public StageAsset StageAsset;
    public readonly Dictionary<EnemyDef, EnemyAsset> Enemies = new Dictionary<EnemyDef, EnemyAsset>();
    public readonly Dictionary<TowerDef, TowerAsset> Towers = new Dictionary<TowerDef, TowerAsset>();
    public readonly List<string> Errors = new List<string>();

    public bool IsValid => Errors.Count == 0;
}

public static class BattleContentBuilder
{
    public static BattleContent Build(StageAsset stageAsset, GameRulesAsset rulesAsset)
    {
        var content = new BattleContent { StageAsset = stageAsset, Rules = ToRules(rulesAsset) };
        if (stageAsset == null)
        {
            content.Errors.Add("전투 Stage 데이터가 지정되지 않았습니다.");
            return content;
        }

        var enemyCache = new Dictionary<EnemyAsset, EnemyDef>();
        var stage = new StageDef
        {
            Id = stageAsset.id,
            Name = stageAsset.displayName,
            StartCoin = stageAsset.startCoin,
            CoreMaxHp = stageAsset.coreMaxHp,
            EnemyHpScale = stageAsset.enemyHpScale,
            Map = ToMap(stageAsset.map),
        };

        foreach (TowerAsset towerAsset in stageAsset.towers)
        {
            if (towerAsset == null) { stage.Towers.Add(null); continue; }
            TowerDef tower = ToTower(towerAsset);
            stage.Towers.Add(tower);
            content.Towers[tower] = towerAsset;
        }

        foreach (WaveData waveData in stageAsset.waves)
        {
            var wave = new WaveDef { Id = waveData.id, PrepareSec = waveData.prepareSec, ClearCoin = waveData.clearCoin };
            foreach (SpawnGroupData groupData in waveData.groups)
            {
                EnemyDef enemy = null;
                if (groupData.enemy != null && !enemyCache.TryGetValue(groupData.enemy, out enemy))
                {
                    enemy = ToEnemy(groupData.enemy);
                    enemyCache[groupData.enemy] = enemy;
                    content.Enemies[enemy] = groupData.enemy;
                }
                wave.Groups.Add(new SpawnGroupDef
                {
                    Id = groupData.id,
                    SpawnId = groupData.spawnId,
                    Enemy = enemy,
                    Count = groupData.count,
                    StartSec = groupData.startSec,
                    IntervalSec = groupData.intervalSec,
                });
            }
            stage.Waves.Add(wave);
        }

        content.Stage = stage;
        content.Errors.AddRange(DefinitionValidator.Validate(stage));
        return content;
    }

    static GameRules ToRules(GameRulesAsset asset)
    {
        if (asset == null) return new GameRules();
        return new GameRules
        {
            Id = asset.id,
            ArmorConstant = asset.armorConstant,
            MinAttackSec = asset.minAttackSec,
            FixedDt = asset.fixedDt,
            MinMoveRatio = asset.minMoveRatio,
            MaxMoveRatio = asset.maxMoveRatio,
        };
    }

    static MapDef ToMap(MapAsset asset)
    {
        if (asset == null) return null;
        var map = new MapDef
        {
            Id = asset.id,
            CorePos = ToN(asset.corePos),
            CameraX = asset.cameraBounds.x,
            CameraY = asset.cameraBounds.y,
            CameraWidth = asset.cameraBounds.width,
            CameraHeight = asset.cameraBounds.height,
            PathClearance = asset.pathClearance,
        };
        foreach (SpawnPointData spawnData in asset.spawnPoints)
        {
            var spawn = new SpawnPointDef { Id = spawnData.id };
            foreach (Vector2 point in spawnData.path) spawn.Path.Add(ToN(point));
            map.SpawnPoints.Add(spawn);
        }
        foreach (Rect zone in asset.buildZones)
            map.BuildZones.Add(new BuildZone(zone.xMin, zone.yMin, zone.xMax, zone.yMax));
        return map;
    }

    static EnemyDef ToEnemy(EnemyAsset asset) => new EnemyDef
    {
        Id = asset.id,
        Name = asset.displayName,
        MaxHp = asset.maxHp,
        Armor = asset.armor,
        MoveSpeed = asset.moveSpeed,
        CoreDamage = asset.coreDamage,
        KillCoin = asset.killCoin,
        IsBoss = asset.isBoss,
    };

    static TowerDef ToTower(TowerAsset asset)
    {
        var tower = new TowerDef { Id = asset.id, Name = asset.displayName, BuildCost = asset.buildCost };
        foreach (TowerLevelData level in asset.levels)
        {
            tower.Levels.Add(new TowerLevelDef
            {
                Id = level.id,
                Damage = level.damage,
                Range = level.range,
                AttackSec = level.attackSec,
                CritChance = level.critChance,
                CritMult = level.critMult,
                UpgradeCost = level.upgradeCost,
                BlockCount = level.blockCount,
                BlockSec = level.blockSec,
                Knockback = level.knockback,
                SlowMult = level.slowMult <= 0f ? 1f : level.slowMult,
                SlowSec = level.slowSec,
            });
        }
        return tower;
    }

    public static NVector2 ToN(Vector2 v) => new NVector2(v.x, v.y);

    public static Vector2 ToUnity(NVector2 v) => new Vector2(v.X, v.Y);
}
