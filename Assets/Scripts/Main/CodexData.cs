using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 도감 항목(수선 도구 = 타워, 악몽 = 몬스터)과 설정 글.
/// 수치는 전투 코어 샘플 데이터(SampleContent)에서 읽어 오므로 밸런스를 바꾸면 도감도 같이 바뀐다.
/// 그림: Resources/Codex/Entries/{타워ID}_{단계}.png, {적ID}.png (Tools/Art/build_codex_ui.py가 복사).
/// </summary>
public static class CodexData
{
    public enum Kind { Tool, Nightmare }

    public sealed class Entry
    {
        public string id;
        public Kind kind;
        public string name;
        /// <summary>한 줄 분류(예: "원거리 · 한 명 집중").</summary>
        public string role;
        /// <summary>설정 글(도감 본문).</summary>
        public string lore;
        /// <summary>도하의 수선 메모(공략 한마디).</summary>
        public string memo;
        public TowerDef tower;
        public EnemyDef enemy;
    }

    static List<Entry> tools, nightmares;

    public static List<Entry> Tools
    {
        get
        {
            if (tools == null) Build();
            return tools;
        }
    }

    public static List<Entry> Nightmares
    {
        get
        {
            if (nightmares == null) Build();
            return nightmares;
        }
    }

    public static string ImagePath(Entry e, int level = 1) =>
        e.kind == Kind.Tool ? $"Codex/Entries/{e.id}_{Mathf.Clamp(level, 1, 3)}" : $"Codex/Entries/{e.id}";

    static void Build()
    {
        var towers = new Dictionary<string, TowerDef>();
        foreach (TowerDef t in SampleContent.Roster()) towers[t.Id] = t;

        tools = new List<Entry>
        {
            Tool(towers["TW_LAMP"], "원거리 · 한 명 집중",
                "도하의 작업대를 밤새 비추던 낡은 스탠드. 이안의 랜턴에서 불씨를 나눠 받은 뒤로, 빛줄기 끝에 닿은 악몽의 실을 바삭하게 태워 버린다. " +
                "빛이 닿는 곳의 실은 다시 엉키지 않는다는 것이 공방의 오래된 믿음이다.",
                "가장 먼저 놓기 좋은 도구야. 길이 잘 보이는 모퉁이에 세워 두면 오래 비춰 줄 수 있어."),
            Tool(towers["TW_SCISSORS"], "근거리 · 여러 명 싹둑",
                "할머니 재단사 때부터 공방에 걸려 있던 무쇠 재단 가위. 가까이 다가온 엉킨 실을 여러 가닥씩 한 번에 끊어 낸다. " +
                "날을 세 번 갈면 쉬지 않고 연달아 가위질을 할 만큼 손에 익는다.",
                "길이 꺾이는 안쪽 칸에 두면 지나가는 악몽을 몇 번이고 자를 수 있어."),
            Tool(towers["TW_SEWING"], "원거리 · 빠른 바늘",
                "공방의 심장이라 불리는 발판 재봉틀. 바늘이 쉴 새 없이 오르내리며 은빛 바늘을 쏘아 찢어진 꿈을 박음질한다. " +
                "한 땀은 가볍지만 셀 수 없이 빠르고, 가끔 정확히 급소를 꿰뚫는다.",
                "실밥 그림자처럼 빠른 악몽도 재봉틀 바늘은 놓치지 않아."),
            Tool(towers["TW_TAPE"], "원거리 · 발 묶기",
                "눈금이 바래도록 쓴 천 줄자. 휙 감겨 나가 악몽의 발목을 묶어 걸음을 늦춘다. " +
                "\"치수를 재는 동안엔 아무도 못 지나간다\"는 말처럼, 3단계 줄자는 두 악몽을 한꺼번에 감는다.",
                "줄자 혼자서는 약해. 스탠드나 가위 앞쪽에 두면 묶어 둔 사이에 다 같이 때릴 수 있어."),
            Tool(towers["TW_PIN"], "근거리 · 방어 깎기",
                "토마토 모양의 오래된 바늘꽂이. 둘레의 악몽에게 시침핀을 촘촘히 꽂아, 단단하게 뭉친 실 껍질을 느슨하게 풀어 놓는다. " +
                "핀이 꽂힌 악몽은 다른 도구에게 훨씬 아프게 베인다.",
                "누더기 곰처럼 두꺼운 악몽이 나오면 꼭 길 옆에 하나 놓아 줘."),
            Tool(towers["TW_TAGBOW"], "원거리 · 수선 표식",
                "수선 의뢰표를 화살 삼아 쏘는 활. 꼬리표가 붙은 악몽은 '수선 대상'으로 기록되어, 공방의 모든 도구가 그 악몽을 더 세게 친다. " +
                "큰 악몽은 표를 반쯤 떼어 내기도 한다.",
                "멀리서 먼저 표를 붙여 두면 뒤에 있는 도구들이 편해져."),
        };

        nightmares = new List<Entry>
        {
            Nightmare(SampleContent.Toy(), "평범 · 가장 흔한 악몽",
                "풀지 못한 걱정이 굴러다니며 뭉친 실타래. 몸에 박힌 단추는 꿈 주인이 잃어버린 작은 물건들의 조각이다. " +
                "혼자서는 약하지만 언제나 떼로 몰려와 꿈의 빛길을 따라 중심으로 굴러간다.",
                "실 껍질이 조금 단단해. 수가 많으니까 가위로 한꺼번에 잘라 줘."),
            Nightmare(SampleContent.Rush(), "빠름 · 약한 몸",
                "터진 솔기 사이로 빠져나온 실밥이 그림자처럼 내달리는 악몽. 쫓기는 꿈, 도망치는 꿈에서 태어나며 " +
                "꼬리 끝에는 부러진 바늘이 매달려 있다. 몸이 가늘어 쉽게 끊어지지만, 눈 깜짝할 새 지나간다.",
                "놓치면 꿈의 불빛이 깎여. 줄자로 발을 묶고 재봉틀로 쏘아 맞혀."),
            Nightmare(SampleContent.Heavy(), "느림 · 두꺼운 껍질",
                "버려진 곰인형이 외로움을 삼켜 거대해진 악몽. 골무와 단추를 갑옷처럼 몸에 박아 넣었고, 찢어진 입에서는 엉킨 실이 흘러나온다. " +
                "걸음은 무겁고 느리지만, 꿈의 중심에 닿으면 큰 구멍을 낸다.",
                "그냥 때리면 잘 안 들어가. 바늘꽂이로 껍질을 먼저 풀고, 수선표를 붙여서 몰아쳐."),
        };
    }

    static Entry Tool(TowerDef def, string role, string lore, string memo) =>
        new Entry { id = def.Id, kind = Kind.Tool, name = def.Name, role = role, lore = lore, memo = memo, tower = def };

    static Entry Nightmare(EnemyDef def, string role, string lore, string memo) =>
        new Entry { id = def.Id, kind = Kind.Nightmare, name = def.Name, role = role, lore = lore, memo = memo, enemy = def };
}

/// <summary>도감 기록: 만난 악몽과 처치 수(PlayerPrefs). 수선 도구는 처음부터 모두 기록되어 있다.</summary>
public static class CodexProgress
{
    const string SeenKey = "Codex.Seen.", KillKey = "Codex.Kills.";
    static readonly string[] Ids = { "EN_TOY", "EN_RUSH", "EN_HEAVY" };

    public static bool Seen(string enemyId) => PlayerPrefs.GetInt(SeenKey + enemyId, 0) == 1;
    public static int Kills(string enemyId) => PlayerPrefs.GetInt(KillKey + enemyId, 0);

    /// <summary>전투 코어 이벤트에 붙여, 나온 악몽을 기록하고 처치 수를 센다. 저장은 전투가 끝날 때 한 번.</summary>
    public static void Attach(BattleSession session)
    {
        session.EnemySpawned += enemy =>
        {
            if (!Seen(enemy.Def.Id)) PlayerPrefs.SetInt(SeenKey + enemy.Def.Id, 1);
        };
        session.EnemyKilled += enemy => PlayerPrefs.SetInt(KillKey + enemy.Def.Id, Kills(enemy.Def.Id) + 1);
        session.Ended += (result, reason) => PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        foreach (string id in Ids)
        {
            PlayerPrefs.DeleteKey(SeenKey + id);
            PlayerPrefs.DeleteKey(KillKey + id);
        }
        PlayerPrefs.Save();
    }
}
