using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 프롤로그 · 꿈속 방 전투 안내(ArtSource/Prologue/프롤로그_화면구성.xlsx 04~05). BattleHUD가 튜토리얼 중일 때 붙인다.
/// 04 툴팁 "타워를 선택하고 설치할 위치를 선택하세요." (왼쪽 아래 도구 슬롯 → 청록 설치 구역 → 녹색 미리보기)
///    → 툴팁 "준비가 되면 '전투 시작'을 누르세요." "전투 시작"을 누르기 전에는 웨이브가 흐르지 않는다(BattleHUD.ManualStart + 멈춤).
/// 05 툴팁 "몬스터는 밝혀진 길의 최단경로로 이동합니다." 쉬운 두 웨이브라 이길 수 있다.
/// 끝나면(결과 화면·게임 오버 화면 없이) 어두워지며 같은 꿈속 방으로 돌아가 문으로 걸어간다(SceneFlow.ReturnToDreamDoor).
/// </summary>
public class BattleTutorial : MonoBehaviour
{
    public BattleController controller;

    const string LampId = "TW_LAMP";
    const float FadeSec = 1.1f;

    readonly TutorialSteps steps = new TutorialSteps();
    int built;
    bool waveStarted, pausedByUs, attached, leaving;
    float black = 1f;
    float endedAt = -1f;

    BattleSession Session => controller != null ? controller.Session : null;

    // ───────── 튜토리얼용 전투 ─────────

    /// <summary>
    /// 전투 정의를 프롤로그용으로 바꾼다(BattleController가 탐색 맵을 적용한 뒤 부른다).
    /// 도구는 스탠드 조명만. 시작 몽결정은 스탠드 두 개 값 이상. 웨이브: 실뭉치 악몽 4마리 → 실뭉치 5 + 빠른 악몽 2(둘 다 쉬움).
    /// </summary>
    public static void SetupStage(BattleContent content)
    {
        StageDef stage = content.Stage;
        if (stage == null || stage.Waves.Count == 0) return;

        TowerDef lamp = null;
        foreach (TowerDef t in stage.Towers)
            if (t != null && t.Id == LampId) lamp = t;
        if (lamp != null) stage.StartCoin = Mathf.Max(stage.StartCoin, lamp.BuildCost * 2 + 20);

        string spawnId = null;
        var byId = new Dictionary<string, EnemyDef>();
        foreach (WaveDef wave in stage.Waves)
            foreach (SpawnGroupDef group in wave.Groups)
            {
                if (spawnId == null) spawnId = group.SpawnId;
                if (group.Enemy != null && !byId.ContainsKey(group.Enemy.Id)) byId[group.Enemy.Id] = group.Enemy;
            }
        EnemyDef toy = Find(byId, "EN_TOY"), rush = Find(byId, "EN_RUSH") ?? toy;
        if (toy == null) return;

        var first = new WaveDef { Id = "WV_PROLOGUE_1", PrepareSec = 30f, ClearCoin = 40 };
        first.Groups.Add(Group("SG_PROLOGUE_1", spawnId, toy, 4, 0f, 2.2f));
        var second = new WaveDef { Id = "WV_PROLOGUE_2", PrepareSec = 6f, ClearCoin = 0 };
        second.Groups.Add(Group("SG_PROLOGUE_2A", spawnId, toy, 5, 0f, 1.8f));
        second.Groups.Add(Group("SG_PROLOGUE_2B", spawnId, rush, 2, 4f, 2.5f));

        stage.Waves.Clear();
        stage.Waves.Add(first);
        stage.Waves.Add(second);
        stage.EnemyHpScale = 1f;
        content.Errors.Clear();
        content.Errors.AddRange(DefinitionValidator.Validate(stage));

        // 다른 도구는 자물쇠로(빈 자리). 검사기는 빈 자리를 오류로 보므로 BattleController.LockTowers처럼 검사 뒤에 비운다.
        for (int i = 0; i < stage.Towers.Count; i++)
            if (stage.Towers[i] != null && stage.Towers[i].Id != LampId) stage.Towers[i] = null;
    }

    static EnemyDef Find(Dictionary<string, EnemyDef> byId, string id) => byId.TryGetValue(id, out EnemyDef e) ? e : null;

    static SpawnGroupDef Group(string id, string spawn, EnemyDef enemy, int count, float start, float interval) =>
        new SpawnGroupDef { Id = id, SpawnId = spawn, Enemy = enemy, Count = count, StartSec = start, IntervalSec = interval };

    // ───────── 안내 ─────────

    void Start()
    {
        BattleHUD hud = GetComponent<BattleHUD>();
        if (hud != null) hud.ManualStart = true;
        steps
            .Guide("타워를 선택하고 설치할 위치를 선택하세요.", () => built >= 1 || waveStarted)
            .Guide("준비가 되면 '전투 시작'을 누르세요.", () => waveStarted)
            .Guide("몬스터는 밝혀진 길의 최단경로로 이동합니다.");
        steps.Start();
    }

    void Update()
    {
        if (Session == null) return;
        if (!attached)
        {
            attached = true;
            Session.TowerBuilt += t => built++;
            Session.WaveStarted += w => waveStarted = true;
        }
        steps.Update();

        // "전투 시작"을 누르기 전(첫 웨이브 준비 중)에는 준비 시간이 흐르지 않게 멈춘다(짓기는 멈춤 중에도 된다).
        bool hold = Session.Phase == BattlePhase.Prepare && !waveStarted;
        if (hold != pausedByUs)
        {
            controller.SetPaused(hold);
            pausedByUs = hold;
        }

        // 들어올 때 서서히 밝아지고, 끝나면 잠깐 뒤 어두워지며 꿈속 방으로
        float dt = Time.unscaledDeltaTime;
        if (Session.Phase == BattlePhase.Ended && endedAt < 0f) endedAt = Time.unscaledTime;
        if (endedAt >= 0f && Time.unscaledTime - endedAt > 1.2f) leaving = true;
        black = Mathf.MoveTowards(black, leaving ? 1f : 0f, dt / FadeSec);
        if (leaving && black >= 1f)
        {
            enabled = false;
            SceneFlow.ReturnToDreamDoor();
        }
    }

    void OnGUI()
    {
        if (Session == null) return;
        GUI.depth = -10; // 전투 HUD 위에
        UIKit.Begin();
        float w = UIKit.Width, h = UIKit.Height;
        if (!leaving && Session.Phase != BattlePhase.Ended && steps.Draw(w, h)) SceneFlow.FinishTutorial(showStory: false);
        if (black > 0f)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, black);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = old;
        }
        UIKit.End();
    }
}
