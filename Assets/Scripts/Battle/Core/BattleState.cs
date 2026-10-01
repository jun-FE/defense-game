using System.Numerics;

namespace Akmong.Battle
{
    /// <summary>몬스터 인스턴스(EntityState). 정의는 읽기만 하고 변하는 값은 여기에만 둔다.</summary>
    public sealed class EnemyState
    {
        public int EntityId { get; internal set; }
        public EnemyDef Def { get; internal set; }
        public SpawnPointDef Spawn { get; internal set; }
        public float MaxHp { get; internal set; }
        public float Hp { get; internal set; }
        /// <summary>경로를 따라 이동한 거리.</summary>
        public float Traveled { get; internal set; }
        public Vector2 Position { get; internal set; }
        /// <summary>직전 틱의 위치. 화면에서 두 틱 사이를 보간할 때 쓴다.</summary>
        public Vector2 PreviousPosition { get; internal set; }
        public bool Alive { get; internal set; } = true;
        public bool ReachedCore { get; internal set; }

        /// <summary>중심까지 남은 경로 거리. NEAREST_CORE 타겟팅 기준.</summary>
        public float RemainingDistance => Spawn.Length - Traveled;
    }

    /// <summary>설치된 타워(TowerInstance).</summary>
    public sealed class TowerState
    {
        public int InstanceId { get; internal set; }
        public TowerDef Def { get; internal set; }
        public int LevelIndex { get; internal set; }
        public int TileX { get; internal set; }
        public int TileY { get; internal set; }
        public Vector2 Position => new Vector2(TileX, TileY);
        /// <summary>다음 공격까지 남은 시간(초).</summary>
        public double AttackRemaining { get; internal set; }
        public int TotalSpent { get; internal set; }

        public TowerLevelDef Level => Def.Levels[LevelIndex];
        public bool CanUpgrade => LevelIndex + 1 < Def.Levels.Count;
    }

    public enum BattlePhase { Prepare, Combat, Ended }

    public enum BattleResult { None, Success, Failure, Retreat }

    public enum EndReason { None, AllWavesCleared, CoreDestroyed, ErosionMaxed, Retreat }

    /// <summary>건설·강화 거부 사유. 실패하면 상태는 바뀌지 않는다.</summary>
    public enum CommandError
    {
        None,
        BattleOver,
        UnknownTower,
        OutsideBuildZone,
        Occupied,
        NotEnoughCoin,
        MaxLevel,
    }

    /// <summary>타워 공격 1회의 결과(DamageResult).</summary>
    public struct HitResult
    {
        public TowerState Tower;
        public EnemyState Target;
        public int Damage;
        public bool Crit;
        public float HpBefore;
        public float HpAfter;
        public bool Killed;
    }
}
