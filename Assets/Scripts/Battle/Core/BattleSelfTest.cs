using System;
using System.Collections.Generic;
using System.Numerics;

namespace Akmong.Battle
{
    /// <summary>
    /// 시스템 기획서의 검산 사례를 그대로 재현하는 자동 검사.
    /// Unity 메뉴 Defense > 전투 검산 테스트 실행, 또는 Tools/CoreTests 에서 실행한다.
    /// </summary>
    public static class BattleSelfTest
    {
        public struct Case
        {
            public string Name;
            public bool Passed;
            public string Detail;
        }

        sealed class FixedRandom : IRandom
        {
            readonly double value;
            public FixedRandom(double value) { this.value = value; }
            public double NextDouble() => value;
        }

        public static List<Case> RunAll()
        {
            var cases = new List<Case>();
            Action<string, Action<List<Case>, string>> run = (name, body) =>
            {
                try { body(cases, name); }
                catch (Exception e) { cases.Add(new Case { Name = name, Passed = false, Detail = "예외: " + e.Message }); }
            };

            run("피해 공식: 기획서 계산 사례 6개", DamageCases);
            run("피해 공식: 최소 피해와 효과 전용 공격", DamageEdgeCases);
            run("이동 속도: 배율 곱과 하한·상한, 속박", SpeedCases);
            run("공격 간격 하한 0.20초", (c, n) => Check(c, n, BattleMath.AttackInterval(0.1f, new GameRules()) == 0.2f, "0.1초 → 0.2초"));
            run("샘플 데이터 검사 통과", (c, n) =>
            {
                List<string> errors = DefinitionValidator.Validate(SampleContent.StageQ01());
                Check(c, n, errors.Count == 0, string.Join(" / ", errors));
            });
            run("검사기: 중복 ID와 없는 출현 지점", ValidatorCatchesErrors);
            run("정원 맵: 데이터 검사 통과, 길 위에는 못 짓고 풀밭에는 지음", GardenCase);
            run("HP 100·방어 25 적은 16피해 7회, 설치 7초 뒤 처치", KillTimeCase);
            run("1웨이브: 12마리 생성, 북 10.5초·동 10초 마지막 생성", WaveSpawnCase);
            run("경제: 스탠드 2개 + 전부 처치 = 잔액 76", EconomyCase);
            run("누수: 중심 피해 5, 처치 재화 없음", LeakCase);
            run("같은 틱 성공·실패면 실패 우선", FailFirstCase);
            run("준비 시간에는 생성 없음, 즉시 시작 가능", PrepareCase);
            run("건설 거부: 영역 밖·점유·재화 부족, 상태 변화 없음", BuildRejectCases);
            run("강화: 잔액 69로 70 강화 실패, 70이면 2단계", UpgradeCases);
            run("타겟: 중심에 가까운 적 우선, 동률은 먼저 생성된 적", TargetingCase);
            run("같은 시드는 같은 결과(재현성)", DeterminismCase);
            run("병정인형: 적을 멈춰 세우고 저지 시간이 지나면 보낸다", BlockCase);
            run("병정인형: 1단계는 1명만, 2단계는 2명 저지", BlockCountCase);
            run("병정인형 3단계: 일반 적은 밀어내고 보스는 면역", KnockbackCase);
            return cases;
        }

        // ───────── 공식 ─────────

        static void DamageCases(List<Case> cases, string name)
        {
            var rules = new GameRules();
            var results = new List<string>();
            bool ok = true;
            Action<string, DamageInput, int> expect = (label, input, want) =>
            {
                int got = BattleMath.ResolveDamage(input, rules);
                results.Add($"{label}={got}");
                ok &= got == want;
            };
            expect("기본", new DamageInput { BaseDamage = 20, Armor = 25 }, 16);
            expect("강화20%", new DamageInput { BaseDamage = 20, DamagePct = 0.2f, Armor = 25 }, 19);
            expect("강화+치명", new DamageInput { BaseDamage = 20, DamagePct = 0.2f, Armor = 25, Crit = true, CritMult = 1.5f }, 29);
            expect("방어-10", new DamageInput { BaseDamage = 20, DamagePct = 0.2f, Armor = 25, ArmorAdd = -10 }, 21);
            expect("방어0", new DamageInput { BaseDamage = 20, Armor = 0 }, 20);
            expect("HP100→71", new DamageInput { BaseDamage = 20, DamagePct = 0.2f, Armor = 25, Crit = true, CritMult = 1.5f }, 29);
            Check(cases, name, ok, string.Join(", ", results));
        }

        static void DamageEdgeCases(List<Case> cases, string name)
        {
            var rules = new GameRules();
            int tiny = BattleMath.ResolveDamage(new DamageInput { BaseDamage = 0.1f, Armor = 400 }, rules);
            int effectOnly = BattleMath.ResolveDamage(new DamageInput { BaseDamage = 0, Armor = 0 }, rules);
            int floored = BattleMath.ResolveDamage(new DamageInput { BaseDamage = 20, DamagePct = -5f, Armor = 0 }, rules);
            Check(cases, name, tiny == 1 && effectOnly == 0 && floored == 2,
                $"작은 공격={tiny}(기대 1), 효과 전용={effectOnly}(기대 0), damage_pct 하한 -0.9 적용={floored}(기대 2)");
        }

        static void SpeedCases(List<Case> cases, string name)
        {
            var rules = new GameRules();
            float mixed = BattleMath.EffectiveSpeed(1.5f, 1.10f, 1.20f, 0.70f, false, rules);
            float floor = BattleMath.EffectiveSpeed(1.5f, 1f, 1f, 0.1f, false, rules);
            float cap = BattleMath.EffectiveSpeed(1.5f, 1.5f, 2f, 1f, false, rules);
            float rooted = BattleMath.EffectiveSpeed(1.5f, 1f, 1f, 1f, true, rules);
            bool ok = Math.Abs(mixed - 1.386f) < 1e-4 && Math.Abs(floor - 0.375f) < 1e-4 && Math.Abs(cap - 3f) < 1e-4 && rooted == 0f;
            Check(cases, name, ok, $"혼합={mixed:0.####}(1.386), 하한={floor}(0.375), 상한={cap}(3), 속박={rooted}(0)");
        }

        static void ValidatorCatchesErrors(List<Case> cases, string name)
        {
            StageDef stage = SampleContent.StageQ01();
            stage.Waves[1].Id = stage.Waves[0].Id;
            stage.Waves[0].Groups[0].SpawnId = "SP_NOWHERE";
            List<string> errors = DefinitionValidator.Validate(stage);
            bool duplicate = errors.Exists(e => e.Contains("중복"));
            bool missing = errors.Exists(e => e.Contains("SP_NOWHERE"));
            Check(cases, name, duplicate && missing, string.Join(" / ", errors));
        }

        static void GardenCase(List<Case> cases, string name)
        {
            StageDef stage = SampleContent.StageGarden();
            List<string> errors = DefinitionValidator.Validate(stage);
            MapDef map = stage.Map;
            int buildable = 0;
            for (int x = 0; x < 23; x++)
                for (int y = 0; y < 13; y++)
                    if (map.IsBuildable(new Vector2(x, y))) buildable++;
            bool onPath = map.IsBuildable(new Vector2(17, 6));   // 오른쪽 바깥 길(x 17.67) 바로 위
            bool onGrass = map.IsBuildable(new Vector2(16, 6));  // 오른쪽 풀밭
            Check(cases, name, errors.Count == 0 && !onPath && onGrass && buildable >= 15,
                $"오류 {errors.Count}개 {string.Join(" / ", errors)}, 길 위 건설 {onPath}(False), 풀밭 {onGrass}(True), 지을 수 있는 칸 {buildable}개");
        }

        // ───────── 시뮬레이션 ─────────

        static StageDef SingleTargetStage(EnemyDef enemy, float pathLength)
        {
            StageDef stage = SampleContent.SpecSampleStage();
            stage.Map.SpawnPoints[0].Path = new List<Vector2> { new Vector2(0, pathLength), new Vector2(0, 0) };
            stage.Waves[0].PrepareSec = 0;
            stage.Waves[0].Groups = new List<SpawnGroupDef>
            {
                new SpawnGroupDef { Id = "SG_TEST", SpawnId = "SP_ROOM_N", Enemy = enemy, Count = 1, StartSec = 0, IntervalSec = 1 },
            };
            stage.Towers[0].Levels[0].CritChance = 0;
            return stage;
        }

        static void KillTimeCase(List<Case> cases, string name)
        {
            // 거의 멈춘 적을 사거리 안에 두고, 치명타 없이 스탠드 1개로 공격한다.
            EnemyDef slow = SampleContent.Toy();
            slow.MoveSpeed = 0.0001f;
            StageDef stage = SingleTargetStage(slow, 1000);
            stage.Map.SpawnPoints[0].Path = new List<Vector2> { new Vector2(0, 5), new Vector2(0, -995) };
            var session = new BattleSession(stage, new GameRules(), new FixedRandom(0.99));
            CommandError error = session.TryBuild(stage.Towers[0], 2, 5, out _);

            int hits = 0;
            double killedAt = -1;
            session.TowerFired += hit => { hits++; if (hit.Killed) killedAt = session.CombatTime; };
            Run(session, 20);
            Check(cases, name, error == CommandError.None && hits == 7 && Math.Abs(killedAt - 7.0) < 1e-6,
                $"건설={error}, 타격 {hits}회(7), 처치 시각 {killedAt:0.00}초(7.00)");
        }

        static void WaveSpawnCase(List<Case> cases, string name)
        {
            StageDef stage = SampleContent.SpecSampleStage();
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(1));
            var lastSpawn = new Dictionary<string, double>();
            int spawned = 0;
            bool countsConsistent = true;
            session.EnemySpawned += e =>
            {
                spawned++;
                lastSpawn[e.Spawn.Id] = session.WaveElapsed;
            };
            while (session.Phase != BattlePhase.Ended && session.TickCount < 10000)
            {
                session.Tick(session.Rules.FixedDt);
                if (session.Phase == BattlePhase.Combat && spawned + session.RemainingSpawns != 12) countsConsistent = false;
            }
            double north = lastSpawn["SP_ROOM_N"], east = lastSpawn["SP_ROOM_E"];
            Check(cases, name, spawned == 12 && countsConsistent && Math.Abs(north - 10.5) < 0.051 && Math.Abs(east - 10.0) < 0.051,
                $"생성 {spawned}(12), 예약+생성 일치={countsConsistent}, 북 마지막 {north:0.00}초, 동 마지막 {east:0.00}초");
        }

        static void EconomyCase(List<Case> cases, string name)
        {
            StageDef stage = SampleContent.SpecSampleStage();
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(7));
            session.TryBuild(stage.Towers[0], 2, 4, out _);
            session.TryBuild(stage.Towers[0], 2, 6, out _);
            int afterBuild = session.Coin;
            while (session.Phase != BattlePhase.Ended && session.TickCount < 10000)
            {
                session.Tick(session.Rules.FixedDt);
                session.DebugKillAll();
            }
            Check(cases, name, afterBuild == 20 && session.Coin == 76 && session.Result == BattleResult.Success && session.CoreHp == 100,
                $"건설 후 {afterBuild}(20), 종료 잔액 {session.Coin}(76), 결과 {session.Result}, 중심 HP {session.CoreHp}");
        }

        static void LeakCase(List<Case> cases, string name)
        {
            StageDef stage = SingleTargetStage(SampleContent.Toy(), 3);
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(1));
            int leaks = 0;
            session.EnemyReachedCore += (e, d) => leaks++;
            Run(session, 30);
            Check(cases, name, leaks == 1 && session.CoreHp == 95 && session.Coin == 120 + 20 && session.AliveCount == 0,
                $"누수 {leaks}회, 중심 HP {session.CoreHp}(95), 잔액 {session.Coin}(140 = 시작 120 + 웨이브 20)");
        }

        static void FailFirstCase(List<Case> cases, string name)
        {
            // 마지막 적이 중심에 도달해 웨이브가 끝나는 바로 그 틱에 중심 HP도 0이 된다.
            StageDef stage = SingleTargetStage(SampleContent.Toy(), 3);
            stage.CoreMaxHp = 5;
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(1));
            Run(session, 30);
            Check(cases, name, session.Result == BattleResult.Failure && session.EndReason == EndReason.CoreDestroyed,
                $"결과 {session.Result}, 사유 {session.EndReason}");
        }

        static void PrepareCase(List<Case> cases, string name)
        {
            StageDef stage = SampleContent.SpecSampleStage();
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(1));
            int spawned = 0;
            session.EnemySpawned += e => spawned++;
            for (int i = 0; i < 199; i++) session.Tick(0.05);
            bool quietDuringPrepare = spawned == 0 && session.Phase == BattlePhase.Prepare;
            session.StartWaveNow();
            session.Tick(0.05);
            Check(cases, name, quietDuringPrepare && session.Phase == BattlePhase.Combat && spawned == 1,
                $"준비 9.95초 동안 생성 {(quietDuringPrepare ? 0 : spawned)}, 즉시 시작 후 단계 {session.Phase}, 생성 {spawned}");
        }

        static void BuildRejectCases(List<Case> cases, string name)
        {
            StageDef stage = SampleContent.StageQ01();
            TowerDef lamp = stage.Towers[0];
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(1));
            CommandError onPath = session.TryBuild(lamp, 0, 5, out _);
            CommandError first = session.TryBuild(lamp, 2, 4, out _);
            CommandError occupied = session.TryBuild(lamp, 2, 4, out _);
            CommandError second = session.TryBuild(lamp, 2, 6, out _);
            int before = session.Coin;
            CommandError broke = session.TryBuild(lamp, 2, 8, out _);
            Check(cases, name,
                onPath == CommandError.OutsideBuildZone && first == CommandError.None && occupied == CommandError.Occupied
                && second == CommandError.None && broke == CommandError.NotEnoughCoin && session.Coin == before && session.Towers.Count == 2,
                $"길 위 {onPath}, 첫 건설 {first}, 같은 칸 {occupied}, 둘째 {second}, 재화 부족 {broke}, 잔액 {session.Coin}(20), 타워 {session.Towers.Count}개");
        }

        static void UpgradeCases(List<Case> cases, string name)
        {
            StageDef stage = SampleContent.StageQ01();
            stage.StartCoin = 119;
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(1));
            TowerState tower;
            session.TryBuild(stage.Towers[0], 2, 4, out tower);
            CommandError poor = session.TryUpgrade(tower);
            bool unchanged = session.Coin == 69 && tower.LevelIndex == 0;
            session.DebugAddCoin(1);
            CommandError ok = session.TryUpgrade(tower);
            CommandError max = session.TryUpgrade(tower);
            Check(cases, name, poor == CommandError.NotEnoughCoin && unchanged && ok == CommandError.None && tower.LevelIndex == 1 && session.Coin == 0 && max == CommandError.MaxLevel,
                $"잔액 69 강화 {poor}(변화 없음 {unchanged}), 70 강화 {ok} → {tower.LevelIndex + 1}단계, 잔액 {session.Coin}, 추가 강화 {max}");
        }

        static void TargetingCase(List<Case> cases, string name)
        {
            // 북쪽 적 2마리: 먼저 나온 적이 중심에 더 가깝다. 동쪽 적은 사거리 밖.
            StageDef stage = SampleContent.SpecSampleStage();
            stage.Waves[0].PrepareSec = 0;
            stage.Towers[0].Levels[0].CritChance = 0;
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(1));
            session.TryBuild(stage.Towers[0], 1, 9, out _);
            var targets = new List<int>();
            session.TowerFired += hit => targets.Add(hit.Target.EntityId);
            for (int i = 0; i < 80; i++) session.Tick(0.05);
            // 첫 공격(1초)은 1번 적, 두 번째(2초)도 여전히 1번 적(중심에 더 가까움).
            Check(cases, name, targets.Count >= 2 && targets[0] == 1 && targets[1] == 1, "공격 대상 순서: " + string.Join(",", targets));
        }

        static void DeterminismCase(List<Case> cases, string name)
        {
            Func<string> play = () =>
            {
                StageDef stage = SampleContent.StageQ01();
                var session = new BattleSession(stage, new GameRules(), new SeededRandom(42));
                session.TryBuild(stage.Towers[0], 2, 4, out _);
                session.TryBuild(stage.Towers[0], 4, 2, out _);
                while (session.Phase != BattlePhase.Ended && session.TickCount < 100000) session.Tick(session.Rules.FixedDt);
                return $"{session.Result}/{session.TickCount}/{session.CoreHp}/{session.Coin}";
            };
            string a = play(), b = play();
            Check(cases, name, a == b, $"1회차 {a}, 2회차 {b}");
        }

        // ───────── 병정인형(근거리 저지) ─────────

        /// <summary>북쪽 길에 적만 나오고 타워는 병정인형 하나. 피해 0이라 저지만 본다.</summary>
        static StageDef SoldierStage(int level, EnemyDef enemy, int count)
        {
            StageDef stage = SingleTargetStage(enemy, 12);
            stage.Waves[0].Groups[0].Count = count;
            stage.Waves[0].Groups[0].IntervalSec = 0.5f;
            stage.StartCoin = 1000;
            TowerDef soldier = SampleContent.Soldier();
            foreach (TowerLevelDef l in soldier.Levels) { l.CritChance = 0; l.Damage = 0; }
            stage.Towers = new List<TowerDef> { soldier };
            return stage;
        }

        static TowerState BuildSoldier(BattleSession session, int level)
        {
            TowerState tower;
            session.TryBuild(session.Stage.Towers[0], 1, 5, out tower);
            for (int i = 1; i < level; i++) session.TryUpgrade(tower);
            return tower;
        }

        static void BlockCase(List<Case> cases, string name)
        {
            StageDef stage = SoldierStage(1, SampleContent.Toy(), 1);
            var session = new BattleSession(stage, new GameRules(), new FixedRandom(0.99));
            BuildSoldier(session, 1);
            EnemyState enemy = null;
            session.EnemySpawned += e => enemy = e;
            double blockedAt = -1, releasedAt = -1;
            float heldY = 0;
            bool stayed = true;
            while (session.Phase != BattlePhase.Ended && session.CombatTime < 30)
            {
                session.Tick(session.Rules.FixedDt);
                if (enemy == null) continue;
                if (enemy.BlockedBy != null && blockedAt < 0) { blockedAt = session.CombatTime; heldY = enemy.Position.Y; }
                if (blockedAt >= 0 && releasedAt < 0)
                {
                    if (enemy.BlockedBy == null) releasedAt = session.CombatTime;
                    else stayed &= Math.Abs(enemy.Position.Y - heldY) < 1e-4;
                }
                if (releasedAt >= 0 && enemy.BlockedBy != null) stayed = false; // 같은 타워에 다시 붙잡히면 안 됨
            }
            double held = releasedAt - blockedAt;
            Check(cases, name, blockedAt > 0 && stayed && Math.Abs(held - 3.0) < 0.051 && session.CoreHp == 95 && heldY > 5.9,
                $"붙잡은 위치 y={heldY:0.00}, 붙잡은 시간 {held:0.00}초(3), 멈춤 유지 {stayed}, 결국 통과해 중심 HP {session.CoreHp}(95)");
        }

        static void BlockCountCase(List<Case> cases, string name)
        {
            Func<int, int> maxBlocked = level =>
            {
                StageDef stage = SoldierStage(level, SampleContent.Toy(), 3);
                var session = new BattleSession(stage, new GameRules(), new FixedRandom(0.99));
                TowerState tower = BuildSoldier(session, level);
                int max = 0;
                while (session.Phase != BattlePhase.Ended && session.CombatTime < 30)
                {
                    session.Tick(session.Rules.FixedDt);
                    max = Math.Max(max, tower.BlockingCount);
                }
                return max;
            };
            int one = maxBlocked(1), two = maxBlocked(2);
            Check(cases, name, one == 1 && two == 2, $"1단계 최대 {one}명(1), 2단계 최대 {two}명(2)");
        }

        static void KnockbackCase(List<Case> cases, string name)
        {
            Func<bool, float> pushed = isBoss =>
            {
                EnemyDef enemy = SampleContent.Heavy();
                enemy.MaxHp = 100000;
                enemy.IsBoss = isBoss;
                StageDef stage = SoldierStage(3, enemy, 1);
                var session = new BattleSession(stage, new GameRules(), new FixedRandom(0.99));
                BuildSoldier(session, 3);
                EnemyState target = null;
                session.EnemySpawned += e => target = e;
                float lastTraveled = 0, back = 0;
                while (session.Phase != BattlePhase.Ended && session.CombatTime < 20)
                {
                    session.Tick(session.Rules.FixedDt);
                    if (target == null) continue;
                    back = Math.Max(back, lastTraveled - target.Traveled);
                    lastTraveled = target.Traveled;
                }
                return back;
            };
            float normal = pushed(false), bossPushed = pushed(true);
            // 밀린 같은 틱에 저지가 풀려 한 틱(0.8×0.05=0.04)만큼 다시 걷는다.
            Check(cases, name, Math.Abs(normal - 0.56f) < 1e-3 && bossPushed == 0f, $"일반 적 한 틱 동안 뒤로 {normal:0.00}(0.6 밀림 − 0.04 걸음), 보스 {bossPushed:0.00}(0)");
        }

        static void Run(BattleSession session, double seconds)
        {
            int ticks = (int)Math.Round(seconds / session.Rules.FixedDt);
            for (int i = 0; i < ticks && session.Phase != BattlePhase.Ended; i++) session.Tick(session.Rules.FixedDt);
        }

        static void Check(List<Case> cases, string name, bool passed, string detail)
        {
            cases.Add(new Case { Name = name, Passed = passed, Detail = detail });
        }
    }
}
