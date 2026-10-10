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
        /// <summary>이 적을 붙잡고 있는 저지 타워. 없으면 null.</summary>
        public TowerState BlockedBy { get; internal set; }
        /// <summary>저지가 풀리기까지 남은 시간(초).</summary>
        public double BlockRemaining { get; internal set; }
        /// <summary>타워별로 남은 저지 시간. 0 이하면 그 타워를 뚫고 지나간 것이라 다시 붙잡히지 않는다.</summary>
        /// <summary>지금 걸린 감속 배율(1 = 없음)과 남은 시간(초).</summary>
        public float SlowMult { get; internal set; } = 1f;
        public double SlowRemaining { get; internal set; }
        public bool Slowed => SlowRemaining > 0;
        internal readonly System.Collections.Generic.Dictionary<int, double> BlockLeft = new System.Collections.Generic.Dictionary<int, double>();

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
        public bool Blocks => Level.BlockCount > 0;
        /// <summary>지금 붙잡고 있는 적 수.</summary>
        public int BlockingCount { get; internal set; }
    }

    public enum BattlePhase { Prepare, Combat, Ended }

    public enum BattleResult { None, Success, Failure, Retreat }

    /// <summary>전투 종료 사유. 실패는 중심 HP 0(CoreDestroyed)뿐이다. 침식도 100은 실패가 아니라 최대 강화 단계.</summary>
    public enum EndReason { None, AllWavesCleared, CoreDestroyed, Retreat }

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
        /// <summary>이 공격으로 적을 밀어냈는지.</summary>
        public bool Knockback;
        /// <summary>이번 공격으로 감속이 걸렸는지.</summary>
        public bool Slowed;
    }
}
