using System;
using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 기술(꿈 수틀) = 도구별 강화 나무. 도구마다 수틀 하나에 거미줄 모양으로 매듭을 수놓는다.
/// - 가운데: 도구(해금하면 수놓을 수 있음).
/// - 안쪽 고리 = 기본 손질 3개(공격력·공격 속도·사거리 등). 배우면 그 도구에 늘 적용된다.
/// - 바깥 살 3줄 = 속성 분기(불씨·독·번개·서리 등, 도구마다 다름). 각 분기는 "각성"(속성이 생김) → "심화"(더 세짐).
///   한 도구에 여러 분기를 배워도 전투에는 "고른 속성" 하나만 들어간다(불과 독을 같이 뿜지 않게). 고르기는 기술 화면에서.
/// - 비용은 실마리(공용). 실마리 = 기본 2 + 도감 기록 보상(CodexRewards).
/// - 해금되지 않은 도구는 수놓을 수 없다(TowerUnlocks).
/// 효과는 전투를 시작할 때 전투 정의(TowerLevelDef)에 더한다(BattleController → Apply).
/// </summary>
public static class Mastery
{
    public enum NodeKind { Center, Base, Awaken, Deep }

    public sealed class Branch
    {
        public string id, name, element, summary;
        public Color color;
        public Node awaken, deep;
    }

    public sealed class Node
    {
        public string id, name, desc, towerId;
        public NodeKind kind;
        public int cost;
        public Branch branch;
        /// <summary>수틀 안 위치: 반지름(수틀 반지름 비율), 각도(도, 위 = -90, 시계 방향).</summary>
        public float radius, angle;
        public Action<TowerLevelDef> apply;
        public readonly List<Node> links = new List<Node>();
    }

    public sealed class Tree
    {
        public string towerId;
        public Node center;
        public readonly List<Node> nodes = new List<Node>();
        public readonly List<Branch> branches = new List<Branch>();
    }

    const string LearnedKey = "Mastery.Learned", ActiveKey = "Mastery.Active.";
    public const int BasePoints = 2;

    static Dictionary<string, Tree> trees;
    static List<string> order;
    static HashSet<string> learned;

    /// <summary>도구 순서(건설 고리 순서와 같음).</summary>
    public static IReadOnlyList<string> TowerOrder
    {
        get
        {
            if (trees == null) Build();
            return order;
        }
    }

    public static Tree TreeOf(string towerId)
    {
        if (trees == null) Build();
        return trees.TryGetValue(towerId, out Tree t) ? t : null;
    }

    // ───────── 실마리·배우기 ─────────

    public static int TotalPoints => BasePoints + CodexRewards.ClaimedPoints;

    public static int SpentPoints
    {
        get
        {
            Load();
            int spent = 0;
            foreach (string id in TowerOrder)
                foreach (Node n in TreeOf(id).nodes)
                    if (n.kind != NodeKind.Center && learned.Contains(n.id)) spent += n.cost;
            return spent;
        }
    }

    public static int FreePoints => TotalPoints - SpentPoints;

    public static bool IsLearned(Node n)
    {
        if (n.kind == NodeKind.Center) return TowerUnlocks.IsUnlocked(n.towerId);
        Load();
        return learned.Contains(n.id);
    }

    public static bool IsReachable(Node n)
    {
        if (!TowerUnlocks.IsUnlocked(n.towerId)) return false;
        foreach (Node l in n.links) if (IsLearned(l)) return true;
        return false;
    }

    public static bool CanLearn(Node n) => n.kind != NodeKind.Center && !IsLearned(n) && IsReachable(n) && FreePoints >= n.cost;

    public static bool Learn(Node n)
    {
        if (!CanLearn(n)) return false;
        learned.Add(n.id);
        // 처음 각성한 속성은 바로 쓰게 한다
        if (n.kind == NodeKind.Awaken && ActiveBranch(n.towerId) == null) SetActive(n.branch);
        Save();
        return true;
    }

    /// <summary>이 도구의 매듭을 모두 풀어 실마리를 돌려받는다.</summary>
    public static void Unlearn(string towerId)
    {
        Load();
        foreach (Node n in TreeOf(towerId).nodes) learned.Remove(n.id);
        PlayerPrefs.DeleteKey(ActiveKey + towerId);
        Save();
    }

    /// <summary>전투에 쓸 속성(각성한 분기 중 고른 것). 없으면 null.</summary>
    public static Branch ActiveBranch(string towerId)
    {
        Tree t = TreeOf(towerId);
        if (t == null) return null;
        string id = PlayerPrefs.GetString(ActiveKey + towerId, "");
        foreach (Branch b in t.branches)
            if (b.id == id && IsLearned(b.awaken)) return b;
        foreach (Branch b in t.branches) // 고른 게 없거나 풀렸으면 배운 것 중 첫 번째
            if (IsLearned(b.awaken)) return b;
        return null;
    }

    public static void SetActive(Branch b)
    {
        PlayerPrefs.SetString(ActiveKey + b.awaken.towerId, b.id);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(LearnedKey);
        foreach (string id in TowerOrder) PlayerPrefs.DeleteKey(ActiveKey + id);
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

    /// <summary>수놓은 기술을 전투 정의에 더한다: 기본 손질 전부 + 고른 속성 하나(각성, 배웠으면 심화). 실마리보다 많이 배운 기록은 무시.</summary>
    public static void Apply(StageDef stage)
    {
        if (stage == null) return;
        int budget = TotalPoints;
        foreach (TowerDef tower in stage.Towers)
        {
            if (tower == null) continue;
            Tree t = TreeOf(tower.Id);
            if (t == null) continue;
            var use = new List<Node>();
            foreach (Node n in t.nodes)
                if (n.kind == NodeKind.Base && IsLearned(n)) use.Add(n);
            Branch active = ActiveBranch(tower.Id);
            if (active != null)
            {
                use.Add(active.awaken);
                if (IsLearned(active.deep)) use.Add(active.deep);
            }
            foreach (Node n in use)
            {
                if (budget < n.cost) continue;
                budget -= n.cost;
                foreach (TowerLevelDef l in tower.Levels) n.apply(l);
            }
            if (active != null)
                foreach (TowerLevelDef l in tower.Levels) l.Element = active.element;
        }
    }

    // ───────── 속성 색(전투 연출·화면 공용) ─────────

    public static Color ElementColor(string element)
    {
        switch (element)
        {
            case "fire": return new Color(1f, 0.5f, 0.15f);
            case "poison": return new Color(0.45f, 0.95f, 0.3f);
            case "lightning": return new Color(1f, 0.93f, 0.35f);
            case "frost": return new Color(0.6f, 0.9f, 1f);
            case "moon": return new Color(0.8f, 0.72f, 1f);
            case "prism": return new Color(1f, 0.6f, 0.95f);
            case "gold": return new Color(1f, 0.82f, 0.35f);
            case "silver": return new Color(0.85f, 0.92f, 1f);
            case "thread": return new Color(1f, 0.68f, 0.8f);
            case "thorn": return new Color(0.5f, 0.8f, 0.3f);
            case "spring": return new Color(0.95f, 0.65f, 0.4f);
            case "magnet": return new Color(0.7f, 0.5f, 1f);
            case "blast": return new Color(1f, 0.42f, 0.25f);
            case "sniper": return new Color(0.6f, 0.82f, 1f);
            default: return Color.white;
        }
    }

    /// <summary>속성 이름(전투 정보 창 등): 지금 쓰는 분기 이름. 없으면 빈 문자열.</summary>
    public static string ElementName(string towerId)
    {
        Branch b = ActiveBranch(towerId);
        return b != null ? b.name : "";
    }

    // ───────── 매듭 정의 ─────────

    // 분기 하나: (id, 이름, 속성, 한 줄 요약, 각성 이름, 각성 설명, 각성 효과, 심화 이름, 심화 설명, 심화 효과)
    sealed class B
    {
        public string id, name, element, summary, n1, d1, n2, d2;
        public Action<TowerLevelDef> a1, a2;
    }

    static B Br(string id, string name, string element, string summary,
        string n1, string d1, Action<TowerLevelDef> a1, string n2, string d2, Action<TowerLevelDef> a2) =>
        new B { id = id, name = name, element = element, summary = summary, n1 = n1, d1 = d1, a1 = a1, n2 = n2, d2 = d2, a2 = a2 };

    static void Build()
    {
        trees = new Dictionary<string, Tree>();
        order = new List<string>();

        Define("TW_LAMP",
            new[] { Ba("전구 조이기", "공격력 +10%.", l => l.Damage *= 1.1f),
                    Ba("스위치 손질", "공격 간격 -8%.", l => l.AttackSec *= 0.92f),
                    Ba("갓 넓히기", "사거리 +0.5칸.", l => l.Range += 0.5f) },
            Br("ember", "불씨 등불", "fire", "빛줄기가 닿은 악몽에 불이 붙어요.",
                "불씨 옮기기", "맞은 악몽이 3초 동안 초당 8 화상 피해를 입어요(방어 무시).", l => { l.DotDps += 8f; l.DotSec = Mathf.Max(l.DotSec, 3f); },
                "타오르는 심지", "화상 초당 +8, 맞은 자리 둘레 0.8칸에 불똥(피해 35%).", l => { l.DotDps += 8f; l.SplashRadius = Mathf.Max(l.SplashRadius, 0.8f); l.SplashPct = 0.35f; }),
            Br("moon", "달빛 등불", "moon", "달빛에 비친 악몽은 약점이 드러나요.",
                "달빛 비추기", "맞은 악몽이 3초 동안 모든 도구에게 12% 더 아프게 맞아요.", l => { l.Vulnerability += 0.12f; l.MarkSec = Mathf.Max(l.MarkSec, 3f); },
                "보름달", "치명타 확률 +20%, 치명타 피해 +0.4배.", l => { l.CritChance += 0.2f; l.CritMult += 0.4f; }),
            Br("prism", "프리즘 등불", "prism", "빛줄기가 갈라져 옆의 악몽에게 튀어요.",
                "빛 가르기", "빛줄기가 가까운 악몽 1명에게 더 튀어요(피해 70%).", l => { l.ChainCount += 1; l.ChainRange = 2.5f; l.ChainFalloff = 0.7f; },
                "무지개 갈래", "1명 더 튀고, 튈 때 피해가 덜 줄어요(80%).", l => { l.ChainCount += 1; l.ChainFalloff = 0.8f; }));

        Define("TW_SCISSORS",
            new[] { Ba("날 갈기", "공격력 +10%.", l => l.Damage *= 1.1f),
                    Ba("나사 조이기", "공격 간격 -8%.", l => l.AttackSec *= 0.92f),
                    Ba("긴 손잡이", "사거리 +0.3칸.", l => l.Range += 0.3f) },
            Br("rust", "녹슨 가위", "poison", "베인 자리에 독이 스며들어요.",
                "녹물 묻히기", "맞은 악몽이 4초 동안 초당 6 독 피해를 입어요(방어 무시).", l => { l.DotDps += 6f; l.DotSec = Mathf.Max(l.DotSec, 4f); },
                "파상풍", "독 초당 +6, 맞은 악몽 방어 -10(3초).", l => { l.DotDps += 6f; l.ArmorShred += 10f; l.MarkSec = Mathf.Max(l.MarkSec, 3f); }),
            Br("frost", "서리 가위", "frost", "차가운 날에 베이면 몸이 굳어요.",
                "서리 날", "맞은 악몽이 1.5초 동안 30% 느려져요.", l => { l.SlowMult = Mathf.Min(l.SlowMult, 0.7f); l.SlowSec = Mathf.Max(l.SlowSec, 1.5f); },
                "얼음 가위질", "15% 확률로 0.8초 얼어붙어 멈춰요.", l => { l.StunChance += 0.15f; l.StunSec = Mathf.Max(l.StunSec, 0.8f); }),
            Br("tailor", "재단사의 가위", "gold", "정확하고 큰 가위질.",
                "본 뜨기", "치명타 확률 +20%.", l => l.CritChance += 0.2f,
                "한 번에 재단", "한 번에 자르는 악몽 +2명.", l => l.HitCount += 2));

        Define("TW_SEWING",
            new[] { Ba("바늘 갈기", "공격력 +10%.", l => l.Damage *= 1.1f),
                    Ba("벨트 갈기", "공격 간격 -8%.", l => l.AttackSec *= 0.92f),
                    Ba("노루발 손질", "사거리 +0.4칸.", l => l.Range += 0.4f) },
            Br("motor", "전기 모터", "lightning", "바늘에 전기가 흘러 옆으로 번져요.",
                "모터 달기", "바늘이 가까운 악몽 2명에게 더 튀어요(피해 60%씩 줄어듦).", l => { l.ChainCount += 2; l.ChainRange = 2.5f; l.ChainFalloff = 0.6f; },
                "번개 박음질", "1명 더 튀고, 8% 확률로 0.5초 감전(멈춤).", l => { l.ChainCount += 1; l.StunChance += 0.08f; l.StunSec = Mathf.Max(l.StunSec, 0.5f); }),
            Br("silver", "은바늘", "silver", "단단한 껍질도 뚫어요.",
                "은바늘 끼우기", "맞은 악몽 방어 -12(2초).", l => { l.ArmorShred += 12f; l.MarkSec = Mathf.Max(l.MarkSec, 2f); },
                "꿰뚫는 바늘", "공격력 +20%.", l => l.Damage *= 1.2f),
            Br("double", "두 줄 박음질", "thread", "바늘 두 개로 동시에 박아요.",
                "쌍바늘", "한 번에 맞히는 악몽 +1명.", l => l.HitCount += 1,
                "고속 박음질", "공격 간격 -15%.", l => l.AttackSec *= 0.85f));

        Define("TW_TAPE",
            new[] { Ba("눈금 다시 긋기", "공격력 +15%.", l => l.Damage *= 1.15f),
                    Ba("감개 손질", "공격 간격 -8%.", l => l.AttackSec *= 0.92f),
                    Ba("줄 늘이기", "사거리 +0.5칸.", l => l.Range += 0.5f) },
            Br("ice", "얼음 줄자", "frost", "감긴 자리가 얼어붙어요.",
                "차가운 줄", "느려지는 정도 +15%, 시간 +1초.", l => { l.SlowMult = Mathf.Max(0.25f, l.SlowMult - 0.15f); l.SlowSec += 1f; },
                "꽁꽁 묶기", "25% 확률로 1.2초 얼어붙어 멈춰요.", l => { l.StunChance += 0.25f; l.StunSec = Mathf.Max(l.StunSec, 1.2f); }),
            Br("thorn", "가시 줄자", "thorn", "가시가 박혀 계속 아파요.",
                "가시 박기", "맞은 악몽이 4초 동안 초당 5 피해를 입어요(방어 무시).", l => { l.DotDps += 5f; l.DotSec = Mathf.Max(l.DotSec, 4f); },
                "가시덩굴", "가시 초당 +5, 한 번에 감는 악몽 +1명.", l => { l.DotDps += 5f; l.HitCount += 1; }),
            Br("spring", "고무 줄자", "spring", "탄력으로 악몽을 뒤로 튕겨요.",
                "튕겨 내기", "맞은 악몽을 길 뒤로 0.8칸 밀어내요(큰 악몽 제외).", l => l.Knockback += 0.8f,
                "힘껏 튕기기", "밀어내기 +0.7칸, 공격력 +30%.", l => { l.Knockback += 0.7f; l.Damage *= 1.3f; }));

        Define("TW_PIN",
            new[] { Ba("핀 갈기", "공격력 +15%.", l => l.Damage *= 1.15f),
                    Ba("쿠션 채우기", "공격 간격 -8%.", l => l.AttackSec *= 0.92f),
                    Ba("긴 핀", "방어 깎기 +5.", l => l.ArmorShred += 5f) },
            Br("venom", "독침", "poison", "핀 끝에 독을 발라요.",
                "독 묻히기", "맞은 악몽이 3초 동안 초당 8 독 피해를 입어요(방어 무시).", l => { l.DotDps += 8f; l.DotSec = Mathf.Max(l.DotSec, 3f); },
                "독침 소나기", "핀을 꽂는 악몽 +2명.", l => l.HitCount += 2),
            Br("magnet", "자석 핀꽂이", "magnet", "자석이 핀을 끌어당겨 악몽을 붙잡아요.",
                "자력", "12% 확률로 0.8초 붙잡혀 멈춰요.", l => { l.StunChance += 0.12f; l.StunSec = Mathf.Max(l.StunSec, 0.8f); },
                "자기장", "핀이 가까운 악몽 1명에게 더 튀어요(피해 70%).", l => { l.ChainCount += 1; l.ChainRange = 2.5f; l.ChainFalloff = 0.7f; }),
            Br("cracker", "폭죽 핀", "blast", "꽂힌 핀이 펑 터져요.",
                "작은 폭죽", "맞은 자리 둘레 1칸에 폭발(피해 60%).", l => { l.SplashRadius = Mathf.Max(l.SplashRadius, 1f); l.SplashPct = 0.6f; },
                "큰 폭죽", "공격력 +40%, 폭발 범위 1.5칸.", l => { l.Damage *= 1.4f; l.SplashRadius = Mathf.Max(l.SplashRadius, 1.5f); }));

        Define("TW_TAGBOW",
            new[] { Ba("활대 손질", "공격력 +10%.", l => l.Damage *= 1.1f),
                    Ba("시위 갈기", "공격 간격 -8%.", l => l.AttackSec *= 0.92f),
                    Ba("붉은 실", "받는 피해 증가 +4%.", l => l.Vulnerability += 0.04f) },
            Br("flame", "불화살 꼬리표", "fire", "꼬리표가 불타며 떨어져요.",
                "불붙은 꼬리표", "3초 동안 초당 10 화상, 둘레 0.8칸에 불똥(피해 40%).", l => { l.DotDps += 10f; l.DotSec = Mathf.Max(l.DotSec, 3f); l.SplashRadius = Mathf.Max(l.SplashRadius, 0.8f); l.SplashPct = 0.4f; },
                "활활", "화상 초당 +8.", l => l.DotDps += 8f),
            Br("thunder", "번개 꼬리표", "lightning", "꼬리표에서 번개가 번져요.",
                "번개 꼬리표", "가까운 악몽 2명에게 더 튀어요(피해 65%씩 줄어듦).", l => { l.ChainCount += 2; l.ChainRange = 3f; l.ChainFalloff = 0.65f; },
                "천둥", "1명 더 튀고, 받는 피해 증가 +10%.", l => { l.ChainCount += 1; l.Vulnerability += 0.1f; }),
            Br("sniper", "저격 꼬리표", "sniper", "아주 먼 곳의 악몽을 정확히 맞혀요.",
                "망원 조준", "사거리 +1.5칸, 치명타 확률 +15%.", l => { l.Range += 1.5f; l.CritChance += 0.15f; },
                "한 발 수선", "치명타 피해 +0.8배, 공격력 +25%.", l => { l.CritMult += 0.8f; l.Damage *= 1.25f; }));
    }

    sealed class BaseDef
    {
        public string name, desc;
        public Action<TowerLevelDef> apply;
    }

    static BaseDef Ba(string name, string desc, Action<TowerLevelDef> apply) => new BaseDef { name = name, desc = desc, apply = apply };

    static void Define(string towerId, BaseDef[] bases, params B[] branches)
    {
        var t = new Tree { towerId = towerId };
        t.center = Add(t, new Node { id = $"{towerId}_C", towerId = towerId, kind = NodeKind.Center, name = "도구", radius = 0f, angle = 0f });
        var baseNodes = new Node[3];
        for (int i = 0; i < 3; i++)
        {
            float angle = -90f + i * 120f;
            baseNodes[i] = Add(t, new Node { id = $"{towerId}_B{i}", towerId = towerId, kind = NodeKind.Base, cost = 1, radius = 0.32f, angle = angle,
                name = bases[i].name, desc = bases[i].desc + " 늘 적용돼요.", apply = bases[i].apply });
            Link(t.center, baseNodes[i]);
        }
        for (int i = 0; i < 3; i++) Link(baseNodes[i], baseNodes[(i + 1) % 3]);

        for (int i = 0; i < branches.Length && i < 3; i++)
        {
            B b = branches[i];
            float angle = -90f + i * 120f;
            var branch = new Branch { id = b.id, name = b.name, element = b.element, summary = b.summary, color = ElementColor(b.element) };
            branch.awaken = Add(t, new Node { id = $"{towerId}_{b.id}_1", towerId = towerId, kind = NodeKind.Awaken, cost = 2, radius = 0.6f, angle = angle,
                name = b.n1, desc = b.d1, apply = b.a1, branch = branch });
            branch.deep = Add(t, new Node { id = $"{towerId}_{b.id}_2", towerId = towerId, kind = NodeKind.Deep, cost = 3, radius = 0.86f, angle = angle,
                name = b.n2, desc = b.d2, apply = b.a2, branch = branch });
            Link(baseNodes[i], branch.awaken);
            Link(branch.awaken, branch.deep);
            t.branches.Add(branch);
        }
        trees[towerId] = t;
        order.Add(towerId);
    }

    static Node Add(Tree t, Node n)
    {
        t.nodes.Add(n);
        return n;
    }

    static void Link(Node a, Node b)
    {
        a.links.Add(b);
        b.links.Add(a);
    }

    /// <summary>실 목록(한 번씩): 그리기용.</summary>
    public static IEnumerable<(Node a, Node b)> Threads(Tree t)
    {
        foreach (Node a in t.nodes)
            foreach (Node b in a.links)
                if (t.nodes.IndexOf(a) < t.nodes.IndexOf(b)) yield return (a, b);
    }
}

/// <summary>
/// 도구 해금: 처음엔 스탠드 조명만 있고, 의뢰(스테이지)를 해결할 때마다 새 도구가 열린다.
/// 열린 도구는 전투 건설 고리·기술(꿈 수틀)·도감에 나온다. 조건은 아래 표만 고치면 된다.
/// </summary>
public static class TowerUnlocks
{
    /// <summary>도구 → 해결해야 하는 스테이지 번호(0부터). -1이면 처음부터.</summary>
    static readonly Dictionary<string, int> RequiredClear = new Dictionary<string, int>
    {
        { "TW_LAMP", -1 },
        { "TW_SCISSORS", 0 },
        { "TW_SEWING", 0 },
        { "TW_TAPE", 1 },
        { "TW_PIN", 1 },
        { "TW_TAGBOW", 2 },
    };

    public static bool IsUnlocked(string towerId) =>
        !RequiredClear.TryGetValue(towerId, out int stage) || stage <= Progress.HighestCleared;

    /// <summary>잠긴 도구 안내 문구.</summary>
    public static string UnlockHint(string towerId)
    {
        if (!RequiredClear.TryGetValue(towerId, out int stage) || stage < 0) return "";
        StageData data = StageDatabase.Instance != null ? StageDatabase.Instance.Get(stage) : null;
        string title = data != null && !string.IsNullOrEmpty(data.title) ? $"'{data.title}'" : $"{stage + 1}번째 의뢰";
        return $"{title}를 해결하면 열려요";
    }
}
