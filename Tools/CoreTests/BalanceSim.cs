// 모의 플레이어(봇)로 스테이지를 여러 번 돌려 성공률·잔여 중심 HP·소요 시간을 잰다.
// 실행: bash Tools/CoreTests/balance.sh [횟수] [mix]
//   mix: 세 번째 건설마다 병정인형을 길 바로 옆 칸에 짓는다(없으면 스탠드만).
// 봇은 "적당히 하는 플레이어"를 흉내낸다: 좋은 칸 8곳을 무작위 순서로 짓고, 가끔 강화한다.
// 실제 사람과 다르므로 방향 확인용으로만 쓰고, 최종 수치는 플레이테스트로 정한다.
using System;
using System.Collections.Generic;
using System.Linq;
using Akmong.Battle;

static class BalanceSim
{
    static readonly int[][] GoodTiles =
    {
        new[] { 1, 2 }, new[] { -1, 2 }, new[] { 4, 1 }, new[] { 2, -1 }, new[] { 1, 4 }, new[] { -1, 4 },
        new[] { 6, 1 }, new[] { 4, -1 }, new[] { 1, 6 }, new[] { -1, 6 }, new[] { 3, 1 }, new[] { 1, 3 },
        new[] { -1, 3 }, new[] { 3, -1 }, new[] { 8, 1 }, new[] { 6, -1 }, new[] { -2, 5 }, new[] { 2, 8 },
    };

    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        int runs = args.Length > 0 ? int.Parse(args[0]) : 200;
        bool mix = args.Length > 1 && args[1] == "mix";
        int wins = 0;
        var coreHp = new List<int>();
        var seconds = new List<double>();
        var failWave = new int[8];

        for (int seed = 1; seed <= runs; seed++)
        {
            var rnd = new Random(seed);
            StageDef stage = SampleContent.StageQ01();
            var session = new BattleSession(stage, new GameRules(), new SeededRandom(seed));
            List<int[]> order = GoodTiles.Take(8).OrderBy(_ => rnd.Next()).Concat(GoodTiles.Skip(8)).ToList();
            double upgradeBias = rnd.NextDouble();
            double total = 0;

            while (session.Phase != BattlePhase.Ended)
            {
                if (session.Towers.Count >= 3 && rnd.NextDouble() < upgradeBias * 0.02)
                    foreach (TowerState tower in session.Towers)
                        if (session.TryUpgrade(tower) == CommandError.None) break;
                TowerDef next = mix && stage.Towers.Count > 1 && session.Towers.Count % 3 == 2 ? stage.Towers[1] : stage.Towers[0];
                if (session.Coin >= next.BuildCost)
                    foreach (int[] tile in order)
                    {
                        // 병정인형은 길에 붙은 칸에만 짓는다.
                        if (next.Levels[0].BlockCount > 0 && Math.Abs(tile[0]) != 1 && Math.Abs(tile[1]) != 1) continue;
                        TowerState built;
                        if (session.TryBuild(next, tile[0], tile[1], out built) == CommandError.None) break;
                    }
                session.Tick(session.Rules.FixedDt);
                total += session.Rules.FixedDt;
            }

            if (session.Result == BattleResult.Success) wins++;
            else failWave[Math.Min(session.WaveIndex, failWave.Length - 1)]++;
            coreHp.Add(session.CoreHp);
            seconds.Add(total);
        }

        coreHp.Sort();
        seconds.Sort();
        Console.WriteLine($"스테이지 {SampleContent.StageQ01().Id}, {runs}회 모의 플레이{(mix ? " (병정인형 섞어 짓기)" : "")}");
        Console.WriteLine($"  성공률        {100.0 * wins / runs:0}%   (기획 목표 70~85%)");
        Console.WriteLine($"  잔여 중심 HP  중앙값 {coreHp[runs / 2]}   (기획 목표 40~70)");
        Console.WriteLine($"  소요 시간     중앙값 {seconds[runs / 2]:0}초, 준비 포함   (기획 목표 120~180초)");
        Console.WriteLine($"  실패한 웨이브 " + string.Join(", ", failWave.Select((n, i) => n > 0 ? $"{i + 1}웨이브 {n}회" : null).Where(x => x != null)));
        return 0;
    }
}
