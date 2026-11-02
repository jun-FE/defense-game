using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 첫 수선 튜토리얼 · 전투 화면 안내(기획서 5장 "도구 배치와 방어 시작").
/// 도구 짓기 → 강화 → 꿈의 불빛 → 웨이브 시작 → 첫 웨이브 막기 → 이길 수 없는 악몽 무리 → 마무리.
/// - 웨이브는 SetupStage가 튜토리얼용으로 바꾼다: 1웨이브 실뭉치 몇 마리(쉬움), 2웨이브 단단한 악몽 무리(못 막음).
/// - 안내를 읽는 동안(웨이브 시작 전)은 준비 시간이 흐르지 않게 전투를 멈춰 둔다(짓기·강화는 멈춤 중에도 된다).
/// - 끝나면 결과 화면 대신 마무리 카드 → SceneFlow.FinishTutorial(마무리 스토리 → 수선소).
/// BattleHUD가 튜토리얼 중일 때 붙인다.
/// </summary>
public class BattleTutorial : MonoBehaviour
{
    public BattleController controller;

    /// <summary>강화 단계에서 모자라면 채워 주는 몽결정.</summary>
    const int UpgradeCoin = 160;

    readonly TutorialSteps steps = new TutorialSteps();
    int built, upgraded, kills;
    bool waveStarted, pausedByUs, attached;
    GUIStyle titleStyle, bodyStyle;

    BattleSession Session => controller != null ? controller.Session : null;

    // ───────── 튜토리얼용 웨이브 ─────────

    /// <summary>
    /// 전투 정의를 튜토리얼용으로 바꾼다(BattleController가 탐색 맵을 적용한 뒤 부른다).
    /// 1웨이브: 실뭉치 악몽 6마리(스탠드 하나로도 막힘). 2웨이브: 체력·껍질이 몇 배인 악몽 무리 100마리 → 꿈의 불빛이 꺼진다.
    /// </summary>
    public static void SetupStage(BattleContent content)
    {
        StageDef stage = content.Stage;
        if (stage == null || stage.Waves.Count == 0) return;
        string spawnId = null;
        var byId = new Dictionary<string, EnemyDef>();
        foreach (WaveDef wave in stage.Waves)
            foreach (SpawnGroupDef group in wave.Groups)
            {
                if (spawnId == null) spawnId = group.SpawnId;
                if (group.Enemy != null && !byId.ContainsKey(group.Enemy.Id)) byId[group.Enemy.Id] = group.Enemy;
            }
        EnemyDef toy = Find(byId, "EN_TOY"), rush = Find(byId, "EN_RUSH") ?? toy, heavy = Find(byId, "EN_HEAVY") ?? toy;
        if (toy == null) return;

        var first = new WaveDef { Id = "WV_TUTORIAL_1", PrepareSec = 30f, ClearCoin = 60 };
        first.Groups.Add(new SpawnGroupDef { Id = "SG_TUTORIAL_1", SpawnId = spawnId, Enemy = toy, Count = 6, StartSec = 0f, IntervalSec = 1.8f });

        var flood = new WaveDef { Id = "WV_TUTORIAL_2", PrepareSec = 8f, ClearCoin = 0 };
        flood.Groups.Add(Group("SG_TUTORIAL_2A", spawnId, Tough(content, heavy), 30, 0f, 0.9f));
        flood.Groups.Add(Group("SG_TUTORIAL_2B", spawnId, Tough(content, rush), 30, 2f, 0.6f));
        flood.Groups.Add(Group("SG_TUTORIAL_2C", spawnId, Tough(content, toy), 40, 1f, 0.5f));

        stage.Waves.Clear();
        stage.Waves.Add(first);
        stage.Waves.Add(flood);
        content.Errors.Clear();
        content.Errors.AddRange(DefinitionValidator.Validate(stage));
    }

    static EnemyDef Find(Dictionary<string, EnemyDef> byId, string id) => byId.TryGetValue(id, out EnemyDef e) ? e : null;

    static SpawnGroupDef Group(string id, string spawn, EnemyDef enemy, int count, float start, float interval) =>
        new SpawnGroupDef { Id = id, SpawnId = spawn, Enemy = enemy, Count = count, StartSec = start, IntervalSec = interval };

    /// <summary>못 막을 만큼 단단한 복사본(그림은 원래 악몽 그대로).</summary>
    static EnemyDef Tough(BattleContent content, EnemyDef source)
    {
        var tough = new EnemyDef
        {
            Id = source.Id + "_TUTORIAL",
            Name = source.Name,
            MaxHp = source.MaxHp * 8,
            Armor = source.Armor + 40f,
            MoveSpeed = source.MoveSpeed,
            CoreDamage = Mathf.Max(20, source.CoreDamage * 3),
            KillCoin = 1,
            IsBoss = source.IsBoss,
        };
        if (content.Enemies.TryGetValue(source, out EnemyAsset asset)) content.Enemies[tough] = asset;
        return tough;
    }

    // ───────── 안내 ─────────

    void Start()
    {
        steps
            .Say("이안", "도구를 꺼내자. 길목부터 막아야 해.")
            .Guide("도구 짓기", "길 옆 지을 수 있는 칸을 클릭하면 수선 도구 고리가 펼쳐져요. <b>스탠드 조명</b>을 골라 지어 보세요. (고리가 열린 뒤 숫자 키 1~6으로도 지을 수 있어요)",
                () => built >= 1 || waveStarted)
            .Guide("도구 강화", "지은 도구를 클릭하면 정보 창이 열려요. <b>강화</b>를 눌러 한 단계 올려 보세요. (도하가 몽결정을 조금 보태 줬어요)",
                () => upgraded >= 1 || waveStarted, GiveUpgradeCoin)
            .Guide("꿈의 불빛", "오른쪽 위 <b>꿈의 불빛</b>은 정원의 중심이 버티는 힘이에요. 악몽이 중심에 닿으면 줄어들고, 0이 되면 수선에 실패해요.",
                () => waveStarted)
            .Guide("방어 시작", "준비가 끝나면 왼쪽 위 <b>바로 시작</b>을 누르거나 <b>Space</b>로 첫 웨이브를 부르세요.",
                () => waveStarted)
            .Guide("방어", "악몽이 밝힌 길을 따라 와요. 악몽을 쓰러뜨리면 몽결정이 생기니, 남는 몽결정으로 도구를 더 지어도 좋아요.",
                () => Session.WaveIndex >= 1)
            .Say("도하", "이안…… 뭔가 커다란 게 와. 하나가 아니야.",
                () => Session.WaveIndex >= 1 && Session.Phase == BattlePhase.Combat)
            .Guide("끝까지", "정원의 중심이 위험해요. 할 수 있는 만큼 버텨 보세요.",
                () => Session.Phase == BattlePhase.Ended);
        steps.Start();
    }

    void GiveUpgradeCoin()
    {
        if (Session == null) return;
        if (Session.Coin < UpgradeCoin) Session.DebugAddCoin(UpgradeCoin - Session.Coin);
    }

    void Update()
    {
        if (Session == null) return;
        if (!attached)
        {
            attached = true;
            Session.TowerBuilt += t => built++;
            Session.TowerUpgraded += t => upgraded++;
            Session.WaveStarted += w => waveStarted = true;
            Session.EnemyKilled += e => kills++;
        }
        steps.Update();

        // 웨이브 전 안내를 읽는 동안은 준비 시간이 흐르지 않게 멈춘다.
        bool hold = Session.Phase == BattlePhase.Prepare && !waveStarted;
        if (hold != pausedByUs)
        {
            controller.SetPaused(hold);
            pausedByUs = hold;
        }
    }

    void OnGUI()
    {
        if (Session == null) return;
        GUI.depth = -10; // 전투 HUD 위에
        UIKit.Begin();
        float w = UIKit.Width;
        if (Session.Phase == BattlePhase.Ended) DrawEnd(w, UIKit.Height);
        else if (steps.Draw(w, 110f)) SceneFlow.FinishTutorial(showStory: false);
        UIKit.End();
    }

    /// <summary>결과 화면 대신: 이번 꿈은 막을 수 없었다 → 마무리 스토리로.</summary>
    void DrawEnd(float w, float h)
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(UIKit.Title) { fontSize = 64 };
            bodyStyle = new GUIStyle(UIKit.Dialogue) { fontSize = 26, alignment = TextAnchor.UpperCenter, wordWrap = true };
        }
        UIKit.DimScreen(0.75f);
        UIKit.ShadowLabel(new Rect(0f, h * 0.24f, w, 100f), "꿈의 불빛이 꺼졌어요", titleStyle);
        GUI.Label(new Rect(w / 2f - 480f, h * 0.24f + 120f, 960f, 140f),
            $"이번 꿈은 너무 깊이 엉켜 있었어요.\n쓰러뜨린 악몽 {kills}마리 · 지은 도구 {built}개\n\n첫 수선 튜토리얼을 마쳤어요.",
            bodyStyle);
        if (UIKit.ClickButton(new Rect(w / 2f - 210f, h * 0.24f + 300f, 420f, 80f), "계속", UIKit.Button))
            SceneFlow.FinishTutorial();
    }
}
