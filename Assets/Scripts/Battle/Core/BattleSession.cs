using System;
using System.Collections.Generic;
using System.Numerics;

namespace Akmong.Battle
{
    /// <summary>
    /// 한 번의 꿈 진입(전투). 고정 간격 Tick(dt)으로만 시간이 흐른다.
    /// 틱 순서(시스템 기획서 3장): 생성 → 저지 갱신 → 타워 공격 → 이동·중심 도달 → 웨이브 판정 → 종료 판정(실패 우선).
    /// 침식·돌발상황·상태 효과는 2주차에 같은 순서 안에 끼워 넣는다.
    /// 실패 조건은 중심 HP 0 하나뿐이다. 침식도는 100에서 멈추고 몬스터를 가장 강하게 만들 뿐 전투를 끝내지 않는다(기획 확정).
    /// </summary>
    public sealed class BattleSession
    {
        const double Epsilon = 1e-6;

        public StageDef Stage { get; }
        public GameRules Rules { get; }

        public BattlePhase Phase { get; private set; }
        public BattleResult Result { get; private set; }
        public EndReason EndReason { get; private set; }
        public int Coin { get; private set; }
        public int CoreHp { get; private set; }
        public int WaveIndex { get; private set; }
        public WaveDef CurrentWave => Stage.Waves[WaveIndex];
        public double PrepareRemaining { get; private set; }
        /// <summary>현재 웨이브 전투가 시작된 뒤 흐른 시간.</summary>
        public double WaveElapsed { get; private set; }
        /// <summary>준비 시간을 뺀 전체 전투 시간.</summary>
        public double CombatTime { get; private set; }
        public long TickCount { get; private set; }

        public IReadOnlyList<EnemyState> Enemies => enemies;
        public IReadOnlyList<TowerState> Towers => towers;
        public int AliveCount { get; private set; }
        public int RemainingSpawns { get; private set; }

        public event Action<EnemyState> EnemySpawned;
        public event Action<HitResult> TowerFired;
        public event Action<EnemyState> EnemyKilled;
        public event Action<EnemyState, int> EnemyReachedCore;
        public event Action<TowerState> TowerBuilt;
        public event Action<TowerState> TowerUpgraded;
        public event Action<int> WaveStarted;
        public event Action<int, int> WaveCleared;
        public event Action<BattleResult, EndReason> Ended;

        readonly IRandom random;
        readonly List<EnemyState> enemies = new List<EnemyState>();
        readonly List<TowerState> towers = new List<TowerState>();
        int[] groupSpawned;
        int nextEntityId = 1;
        int nextTowerId = 1;

        public BattleSession(StageDef stage, GameRules rules, IRandom random)
        {
            Stage = stage;
            Rules = rules;
            this.random = random;
            Coin = stage.StartCoin;
            CoreHp = stage.CoreMaxHp;
            BeginWave(0);
        }

        // ───────── 명령 ─────────

        /// <summary>타일(x, y)에 타워를 짓는다. 검사 순서: 종료 → 종류 → 건설 영역 → 점유 → 비용.</summary>
        public CommandError TryBuild(TowerDef def, int tileX, int tileY, out TowerState built)
        {
            built = null;
            CommandError error = CanBuild(def, tileX, tileY);
            if (error != CommandError.None) return error;

            Coin -= def.BuildCost;
            built = new TowerState
            {
                InstanceId = nextTowerId++,
                Def = def,
                TileX = tileX,
                TileY = tileY,
                TotalSpent = def.BuildCost,
            };
            // 새 타워는 설치 후 한 공격 간격이 지나야 첫 공격을 한다.
            built.AttackRemaining = BattleMath.AttackInterval(built.Level.AttackSec, Rules);
            towers.Add(built);
            TowerBuilt?.Invoke(built);
            return CommandError.None;
        }

        /// <summary>건설 가능 여부만 검사한다(미리보기용). 상태는 바꾸지 않는다.</summary>
        public CommandError CanBuild(TowerDef def, int tileX, int tileY)
        {
            if (Phase == BattlePhase.Ended) return CommandError.BattleOver;
            if (def == null || !Stage.Towers.Contains(def)) return CommandError.UnknownTower;
            if (!Stage.Map.IsBuildable(new Vector2(tileX, tileY))) return CommandError.OutsideBuildZone;
            if (TowerAt(tileX, tileY) != null) return CommandError.Occupied;
            if (Coin < def.BuildCost) return CommandError.NotEnoughCoin;
            return CommandError.None;
        }

        public CommandError TryUpgrade(TowerState tower)
        {
            if (Phase == BattlePhase.Ended) return CommandError.BattleOver;
            if (tower == null || !towers.Contains(tower)) return CommandError.UnknownTower;
            if (!tower.CanUpgrade) return CommandError.MaxLevel;
            int cost = tower.Level.UpgradeCost;
            if (Coin < cost) return CommandError.NotEnoughCoin;

            double oldInterval = BattleMath.AttackInterval(tower.Level.AttackSec, Rules);
            Coin -= cost;
            tower.TotalSpent += cost;
            tower.LevelIndex++;
            // 남은 공격 시간을 새 간격 비율로 환산한다(공짜 즉발 공격 없음).
            double newInterval = BattleMath.AttackInterval(tower.Level.AttackSec, Rules);
            tower.AttackRemaining = tower.AttackRemaining / oldInterval * newInterval;
            TowerUpgraded?.Invoke(tower);
            return CommandError.None;
        }

        public TowerState TowerAt(int tileX, int tileY)
        {
            foreach (TowerState tower in towers)
                if (tower.TileX == tileX && tower.TileY == tileY) return tower;
            return null;
        }

        /// <summary>준비 시간을 건너뛰고 웨이브를 바로 시작한다.</summary>
        public void StartWaveNow()
        {
            if (Phase == BattlePhase.Prepare) StartCombat();
        }

        /// <summary>중도 귀환. 의뢰는 진행 중으로 남고 이번 전투만 끝난다.</summary>
        public void Retreat()
        {
            if (Phase != BattlePhase.Ended) End(BattleResult.Retreat, EndReason.Retreat);
        }

        // ───────── 개발용 ─────────

        public void DebugAddCoin(int amount) => Coin += amount;

        public void DebugKillAll()
        {
            foreach (EnemyState enemy in enemies)
                if (enemy.Alive) Kill(enemy);
            RemoveFinished();
        }

        // ───────── 시뮬레이션 ─────────

        public void Tick(double dt)
        {
            if (Phase == BattlePhase.Ended) return;
            TickCount++;

            if (Phase == BattlePhase.Prepare)
            {
                // 준비 시간에는 건설만 가능하고 전투 시계는 멈춘다.
                PrepareRemaining -= dt;
                if (PrepareRemaining <= Epsilon) StartCombat();
                return;
            }

            CombatTime += dt;
            WaveElapsed += dt;

            foreach (EnemyState enemy in enemies) enemy.PreviousPosition = enemy.Position;

            SpawnDue();
            UpdateBlocking(dt);
            foreach (TowerState tower in towers) UpdateTower(tower, dt);
            foreach (EnemyState enemy in enemies)
                if (enemy.Alive) Move(enemy, dt);
            RemoveFinished();

            // 실패 우선: 같은 틱에 마지막 적을 처치해도 중심이 무너졌으면 실패.
            if (CoreHp <= 0)
            {
                End(BattleResult.Failure, EndReason.CoreDestroyed);
                return;
            }

            if (RemainingSpawns == 0 && AliveCount == 0)
            {
                int clearCoin = CurrentWave.ClearCoin;
                Coin += clearCoin;
                WaveCleared?.Invoke(WaveIndex, clearCoin);
                if (WaveIndex + 1 >= Stage.Waves.Count) End(BattleResult.Success, EndReason.AllWavesCleared);
                else BeginWave(WaveIndex + 1);
            }
        }

        void BeginWave(int index)
        {
            WaveIndex = index;
            Phase = BattlePhase.Prepare;
            PrepareRemaining = CurrentWave.PrepareSec;
            WaveElapsed = 0;
            groupSpawned = new int[CurrentWave.Groups.Count];
            RemainingSpawns = 0;
            foreach (SpawnGroupDef group in CurrentWave.Groups) RemainingSpawns += group.Count;
        }

        void StartCombat()
        {
            Phase = BattlePhase.Combat;
            PrepareRemaining = 0;
            WaveElapsed = 0;
            WaveStarted?.Invoke(WaveIndex);
        }

        void SpawnDue()
        {
            List<SpawnGroupDef> groups = CurrentWave.Groups;
            for (int g = 0; g < groups.Count; g++)
            {
                SpawnGroupDef group = groups[g];
                while (groupSpawned[g] < group.Count && WaveElapsed + Epsilon >= group.SpawnTime(groupSpawned[g]))
                {
                    groupSpawned[g]++;
                    RemainingSpawns--;
                    Spawn(group);
                }
            }
        }

        void Spawn(SpawnGroupDef group)
        {
            SpawnPointDef spawn = Stage.Map.FindSpawn(group.SpawnId);
            // 스테이지 HP 배율. 2주차: 침식 단계 HP 배율도 여기서 곱한다(신규 생성 적에만).
            float hp = (float)Math.Ceiling(group.Enemy.MaxHp * (double)Stage.EnemyHpScale);
            var enemy = new EnemyState
            {
                EntityId = nextEntityId++,
                Def = group.Enemy,
                Spawn = spawn,
                MaxHp = hp,
                Hp = hp,
                Position = spawn.Path[0],
                PreviousPosition = spawn.Path[0],
            };
            enemies.Add(enemy);
            AliveCount++;
            EnemySpawned?.Invoke(enemy);
        }

        void UpdateTower(TowerState tower, double dt)
        {
            tower.AttackRemaining -= dt;
            if (tower.AttackRemaining > Epsilon) return;

            TowerLevelDef level = tower.Level;
            // 저지 타워는 자기가 붙잡은 적부터 친다.
            EnemyState target = (tower.Blocks ? SelectTarget(tower.Position, level.Range, tower) : null)
                                ?? SelectTarget(tower.Position, level.Range, null);
            if (target == null)
            {
                // 대상이 없으면 준비된 상태로 기다린다.
                tower.AttackRemaining = 0;
                return;
            }

            bool crit = level.CritChance > 0f && random.NextDouble() < level.CritChance;
            int damage = BattleMath.ResolveDamage(new DamageInput
            {
                BaseDamage = level.Damage,
                Armor = target.Def.Armor,
                Crit = crit,
                CritMult = level.CritMult,
            }, Rules);

            float before = target.Hp;
            target.Hp = Math.Max(0f, target.Hp - damage);
            tower.AttackRemaining += BattleMath.AttackInterval(level.AttackSec, Rules);

            bool killed = target.Hp <= 0f;
            bool knockback = !killed && level.Knockback > 0f && !target.Def.IsBoss;
            if (knockback)
            {
                // 밀어내기: 경로 뒤로 민다. 저지는 풀리지만 남은 저지 시간은 기억했다가 다시 붙잡을 때 이어 쓴다.
                Release(target);
                target.Traveled = Math.Max(0f, target.Traveled - level.Knockback);
                target.Position = target.Spawn.PointAt(target.Traveled);
            }
            TowerFired?.Invoke(new HitResult
            {
                Tower = tower,
                Target = target,
                Damage = damage,
                Crit = crit,
                HpBefore = before,
                HpAfter = target.Hp,
                Killed = killed,
                Knockback = knockback,
            });
            if (killed) Kill(target);
        }

        /// <summary>
        /// 근거리 저지: 사거리 안의 적을 중심에 가까운 순으로 붙잡는다(최대 BlockCount).
        /// 붙잡힌 적은 멈추고, 한 타워에 붙잡힌 시간이 모두 BlockSec가 되면 풀려나 그 타워를 지나간다.
        /// 다른 저지 타워에는 다시 붙잡힐 수 있다.
        /// </summary>
        void UpdateBlocking(double dt)
        {
            foreach (EnemyState enemy in enemies)
            {
                TowerState blocker = enemy.BlockedBy;
                if (blocker == null) continue;
                enemy.BlockRemaining -= dt;
                if (enemy.BlockRemaining <= Epsilon) Release(enemy);
            }

            foreach (TowerState tower in towers)
            {
                if (!tower.Blocks) continue;
                while (tower.BlockingCount < tower.Level.BlockCount)
                {
                    EnemyState next = SelectBlockCandidate(tower);
                    if (next == null) break;
                    double left;
                    next.BlockedBy = tower;
                    next.BlockRemaining = next.BlockLeft.TryGetValue(tower.InstanceId, out left) ? left : tower.Level.BlockSec;
                    tower.BlockingCount++;
                }
            }
        }

        EnemyState SelectBlockCandidate(TowerState tower)
        {
            EnemyState best = null;
            float rangeSq = tower.Level.Range * tower.Level.Range + (float)Epsilon;
            foreach (EnemyState enemy in enemies)
            {
                if (!enemy.Alive || enemy.BlockedBy != null) continue;
                double left;
                if (enemy.BlockLeft.TryGetValue(tower.InstanceId, out left) && left <= Epsilon) continue;
                if (Vector2.DistanceSquared(tower.Position, enemy.Position) > rangeSq) continue;
                if (best == null || Closer(enemy, best)) best = enemy;
            }
            return best;
        }

        void Release(EnemyState enemy)
        {
            TowerState blocker = enemy.BlockedBy;
            if (blocker == null) return;
            blocker.BlockingCount--;
            enemy.BlockLeft[blocker.InstanceId] = enemy.BlockRemaining;
            enemy.BlockedBy = null;
            enemy.BlockRemaining = 0;
        }

        static bool Closer(EnemyState a, EnemyState b) =>
            a.RemainingDistance < b.RemainingDistance - Epsilon
            || (Math.Abs(a.RemainingDistance - b.RemainingDistance) <= Epsilon && a.EntityId < b.EntityId);

        /// <summary>NEAREST_CORE: 사거리 안에서 중심까지 남은 경로가 가장 짧은 적. 동률은 먼저 생성된 적.
        /// blockedBy를 주면 그 타워가 붙잡은 적만 고른다.</summary>
        EnemyState SelectTarget(Vector2 from, float range, TowerState blockedBy)
        {
            EnemyState best = null;
            float rangeSq = range * range + (float)Epsilon;
            foreach (EnemyState enemy in enemies)
            {
                if (!enemy.Alive) continue;
                if (blockedBy != null && enemy.BlockedBy != blockedBy) continue;
                if (blockedBy == null && Vector2.DistanceSquared(from, enemy.Position) > rangeSq) continue;
                if (best == null || Closer(enemy, best)) best = enemy;
            }
            return best;
        }

        void Move(EnemyState enemy, double dt)
        {
            if (enemy.BlockedBy != null) return; // 저지당한 적은 제자리에 멈춘다.
            // 2주차: 침식 속도 배율, 가속·감속·속박 효과
            float speed = BattleMath.EffectiveSpeed(enemy.Def.MoveSpeed, 1f, 1f, 1f, false, Rules);
            enemy.Traveled += (float)(speed * dt);
            if (enemy.Traveled + Epsilon >= enemy.Spawn.Length)
            {
                enemy.Traveled = enemy.Spawn.Length;
                enemy.Position = enemy.Spawn.PointAt(enemy.Traveled);
                ReachCore(enemy);
                return;
            }
            enemy.Position = enemy.Spawn.PointAt(enemy.Traveled);
        }

        void ReachCore(EnemyState enemy)
        {
            // 중심 도달: 누수 피해 1회 후 퇴장. 처치 재화 없음.
            enemy.Alive = false;
            enemy.ReachedCore = true;
            Release(enemy);
            AliveCount--;
            int damage = enemy.Def.CoreDamage;
            CoreHp = Math.Max(0, CoreHp - damage);
            EnemyReachedCore?.Invoke(enemy, damage);
        }

        void Kill(EnemyState enemy)
        {
            if (!enemy.Alive) return;
            enemy.Alive = false;
            Release(enemy);
            AliveCount--;
            // 처치 재화는 개체마다 한 번만.
            Coin += enemy.Def.KillCoin;
            EnemyKilled?.Invoke(enemy);
        }

        void RemoveFinished() => enemies.RemoveAll(e => !e.Alive);

        void End(BattleResult result, EndReason reason)
        {
            Phase = BattlePhase.Ended;
            Result = result;
            EndReason = reason;
            Ended?.Invoke(result, reason);
        }
    }
}
