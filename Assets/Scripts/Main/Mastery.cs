using System;
using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 기술(마스터리) = "꿈 수틀": 수틀 위에 거미줄 모양으로 수놓는 기술 나무.
/// 가운데 "도하의 바늘"에서 시작해, 이미 수놓은 매듭과 실로 이어진 매듭만 새로 수놓을 수 있다.
/// - 살 6줄 = 수선 도구 6종(위에서부터 시계 방향: 스탠드·가위·재봉틀·줄자·바늘꽂이·수선표 활).
///   안쪽 고리 = 공격력, 가운데 고리 = 도구 특기, 바깥 고리 = 큰 매듭(도구가 크게 바뀜).
/// - 가운데 고리의 살 사이 6칸 = 공방 전체 기술(시작 꿈실, 꿈의 불빛 등).
/// 비용은 "실마리". 실마리 = 기본 3 + 의뢰(스테이지)를 깰 때마다 2. 언제든 무료로 모두 풀 수 있다.
/// 효과는 전투를 시작할 때 전투 정의(StageDef)에 더한다(BattleController).
/// </summary>
public static class Mastery
{
    public enum NodeKind { Center, Inner, Spoke, Weave, Keystone }

    public sealed class Node
    {
        public string id, name, desc;
        public NodeKind kind;
        public int cost;
        /// <summary>수틀 안 위치: 반지름(수틀 반지름 비율), 각도(도, 위 = -90, 시계 방향).</summary>
        public float radius, angle;
        /// <summary>도구 줄 매듭이면 그 타워 ID(그림 표시용).</summary>
        public string towerId;
        public string icon;
        public Action<StageDef> apply;
        public readonly List<Node> links = new List<Node>();
    }

    const string LearnedKey = "Mastery.Learned";
    public const int BasePoints = 3, PointsPerClear = 2;

    static readonly string[] SpokeTowers = { "TW_LAMP", "TW_SCISSORS", "TW_SEWING", "TW_TAPE", "TW_PIN", "TW_TAGBOW" };

    static List<Node> nodes;
    static HashSet<string> learned;

    public static List<Node> Nodes
    {
        get
        {
            if (nodes == null) Build();
            return nodes;
        }
    }

    // ───────── 실마리·배우기 ─────────

    public static int TotalPoints => BasePoints + PointsPerClear * Mathf.Max(0, Progress.HighestCleared + 1);

    public static int SpentPoints
    {
        get
        {
            int spent = 0;
            foreach (Node n in Nodes) if (IsLearned(n) && n.kind != NodeKind.Center) spent += n.cost;
            return spent;
        }
    }

    public static int FreePoints => TotalPoints - SpentPoints;

    public static bool IsLearned(Node n)
    {
        if (n.kind == NodeKind.Center) return true;
        Load();
        return learned.Contains(n.id);
    }

    /// <summary>이미 수놓은 매듭과 실로 이어져 있으면 수놓을 수 있다(실마리는 따로 본다).</summary>
    public static bool IsReachable(Node n)
    {
        foreach (Node l in n.links) if (IsLearned(l)) return true;
        return false;
    }

    public static bool CanLearn(Node n) => !IsLearned(n) && IsReachable(n) && FreePoints >= n.cost;

    public static bool Learn(Node n)
    {
        if (!CanLearn(n)) return false;
        learned.Add(n.id);
        Save();
        return true;
    }

    /// <summary>모두 풀기: 실마리를 전부 돌려받는다.</summary>
    public static void Unlearn()
    {
        Load();
        learned.Clear();
        Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(LearnedKey);
        PlayerPrefs.Save();
        learned = null;
    }

    static void Load()
    {
        if (learned != null) return;
        learned = new HashSet<string>(PlayerPrefs.GetString(LearnedKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
    }

    static void Save()
    {
        PlayerPrefs.SetString(LearnedKey, string.Join(",", learned));
        PlayerPrefs.Save();
    }

    /// <summary>수놓은 기술을 전투 정의에 더한다. 실마리가 모자라면(진행 초기화 등) 앞에서부터 들어가는 만큼만.</summary>
    public static void Apply(StageDef stage)
    {
        if (stage == null) return;
        int budget = TotalPoints;
        foreach (Node n in Nodes)
        {
            if (n.kind == NodeKind.Center || !IsLearned(n) || n.apply == null) continue;
            if (budget < n.cost) continue;
            budget -= n.cost;
            n.apply(stage);
        }
    }

    // ───────── 매듭 정의 ─────────

    static void Build()
    {
        nodes = new List<Node>();
        Node center = Add(new Node { id = "M_CENTER", name = "도하의 바늘", kind = NodeKind.Center, cost = 0, radius = 0f, angle = 0f,
            desc = "모든 수놓기가 시작되는 매듭. 도하가 처음 이안에게 쥐여 준 은바늘이 여기 꽂혀 있다." });

        var inner = new Node[6];
        var spoke = new Node[6];
        var keystone = new Node[6];
        var weave = new Node[6];

        // 도구별 (이름, 설명, 효과): 안쪽 고리 / 가운데 고리 / 바깥 고리
        var tool = new (string n1, string n2, string d2, Action<TowerLevelDef> a2, string n3, string d3, Action<TowerLevelDef> a3)[]
        {
            ("스탠드 손질", "넓은 갓", "사거리 +0.6칸", l => l.Range += 0.6f,
                "랜턴 불씨", "치명타 확률 +15%, 치명타 피해 +0.3배", l => { l.CritChance += 0.15f; l.CritMult += 0.3f; }),
            ("가위 손질", "날 세우기", "한 번에 자르는 적 +1명", l => l.HitCount += 1,
                "할머니의 가위", "공격력 +25%, 사거리 +0.3칸", l => { l.Damage *= 1.25f; l.Range += 0.3f; }),
            ("재봉틀 손질", "기름칠", "공격 간격 -12%", l => l.AttackSec *= 0.88f,
                "쉬지 않는 바늘", "공격 간격 -20%", l => l.AttackSec *= 0.8f),
            ("줄자 손질", "질긴 줄자", "느리게 하는 정도 +10%, 시간 +0.5초", l => { l.SlowMult = Mathf.Max(0.3f, l.SlowMult - 0.1f); l.SlowSec += 0.5f; },
                "한 번 더 감기", "한 번에 감는 적 +1명, 묶는 시간 +1초", l => { l.HitCount += 1; l.SlowSec += 1f; }),
            ("바늘꽂이 손질", "긴 시침핀", "방어 깎기 +10", l => l.ArmorShred += 10f,
                "바늘 폭풍", "핀을 꽂는 적 +2명, 표식 시간 +1초", l => { l.HitCount += 2; l.MarkSec += 1f; }),
            ("수선표 활 손질", "빨간 꼬리표", "받는 피해 증가 +5%", l => l.Vulnerability += 0.05f,
                "수선 확정", "받는 피해 증가 +10%, 사거리 +1칸", l => { l.Vulnerability += 0.1f; l.Range += 1f; }),
        };
        string[] lore =
        {
            "갓에 묻은 먼지를 털고 전구를 조인다.", "녹을 닦아 날을 맞춘다.", "벨트를 갈고 바늘을 곧게 편다.",
            "늘어난 눈금을 다시 꿰맨다.", "무뎌진 핀을 갈아 끼운다.", "활시위를 새 실로 바꾼다.",
        };

        for (int i = 0; i < 6; i++)
        {
            string tid = SpokeTowers[i];
            float angle = -90f + i * 60f;
            var t = tool[i];
            inner[i] = Add(new Node { id = $"M_{tid}_1", name = t.n1, kind = NodeKind.Inner, cost = 1, radius = 0.3f, angle = angle, towerId = tid,
                desc = $"{lore[i]} 공격력 +10%.", apply = s => Tower(s, tid, l => l.Damage *= 1.1f) });
            spoke[i] = Add(new Node { id = $"M_{tid}_2", name = t.n2, kind = NodeKind.Spoke, cost = 2, radius = 0.57f, angle = angle, towerId = tid,
                desc = t.d2 + ".", apply = s => Tower(s, tid, t.a2) });
            keystone[i] = Add(new Node { id = $"M_{tid}_3", name = t.n3, kind = NodeKind.Keystone, cost = 3, radius = 0.82f, angle = angle, towerId = tid,
                desc = "큰 매듭. " + t.d3 + ".", apply = s => Tower(s, tid, t.a3) });
        }

        // 살 사이 공방 전체 기술 (가운데 고리, 각도 +30도)
        var general = new (string name, string desc, string icon, Action<StageDef> apply)[]
        {
            ("넉넉한 실타래", "전투 시작 꿈실 +30.", "cx_icon_reward", s => s.StartCoin += 30),
            ("꼼꼼한 정리", "악몽을 처치할 때마다 꿈실 +1.", "cx_icon_reward", s => ForEachEnemy(s, e => e.KillCoin += 1)),
            ("도하의 응원", "모든 도구 치명타 확률 +5%.", "cx_icon_damage", s => AllTowers(s, l => l.CritChance += 0.05f)),
            ("단단한 불빛", "꿈의 불빛(중심 체력) +10.", "cx_icon_core", s => s.CoreMaxHp += 10),
            ("알뜰 수선", "도구 강화 비용 -15%.", "cx_icon_reward", s => AllTowers(s, l => l.UpgradeCost = Mathf.RoundToInt(l.UpgradeCost * 0.85f))),
            ("헌 천 재활용", "도구 짓기 비용 -10%.", "cx_icon_reward", s =>
            {
                foreach (TowerDef t in s.Towers) if (t != null) t.BuildCost = Mathf.RoundToInt(t.BuildCost * 0.9f);
            }),
        };
        for (int i = 0; i < 6; i++)
        {
            var g = general[i];
            weave[i] = Add(new Node { id = $"M_WEAVE_{i}", name = g.name, kind = NodeKind.Weave, cost = 1, radius = 0.57f, angle = -60f + i * 60f,
                icon = g.icon, desc = g.desc, apply = g.apply });
        }

        // 실 잇기: 가운데 → 안쪽 고리, 안쪽 고리끼리, 살 따라 바깥으로, 가운데 고리는 살 ↔ 사이 칸
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            Link(center, inner[i]);
            Link(inner[i], inner[next]);
            Link(inner[i], spoke[i]);
            Link(spoke[i], keystone[i]);
            Link(spoke[i], weave[i]);
            Link(weave[i], spoke[next]);
        }
    }

    static Node Add(Node n)
    {
        nodes.Add(n);
        return n;
    }

    static void Link(Node a, Node b)
    {
        a.links.Add(b);
        b.links.Add(a);
    }

    /// <summary>실 목록(한 번씩): 그리기용.</summary>
    public static IEnumerable<(Node a, Node b)> Threads()
    {
        foreach (Node a in Nodes)
            foreach (Node b in a.links)
                if (Nodes.IndexOf(a) < Nodes.IndexOf(b)) yield return (a, b);
    }

    static void Tower(StageDef s, string towerId, Action<TowerLevelDef> change)
    {
        foreach (TowerDef t in s.Towers)
            if (t != null && t.Id == towerId)
                foreach (TowerLevelDef l in t.Levels) change(l);
    }

    static void AllTowers(StageDef s, Action<TowerLevelDef> change)
    {
        foreach (TowerDef t in s.Towers)
            if (t != null)
                foreach (TowerLevelDef l in t.Levels) change(l);
    }

    static void ForEachEnemy(StageDef s, Action<EnemyDef> change)
    {
        var seen = new HashSet<EnemyDef>();
        foreach (WaveDef w in s.Waves)
            foreach (SpawnGroupDef g in w.Groups)
                if (g.Enemy != null && seen.Add(g.Enemy)) change(g.Enemy);
    }
}
