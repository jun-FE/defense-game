using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 전투 세션을 만들고 고정 간격(기본 0.05초)으로 Tick을 돌린다. 렌더 프레임과 분리된 누적 시간 방식.
/// 화면 쪽 스크립트는 Session을 읽기만 하고, 바꿀 때는 명령 함수(TryBuild 등)만 쓴다.
/// </summary>
public class BattleController : MonoBehaviour
{
    [Tooltip("스테이지 선택 없이 이 씬을 바로 실행할 때 쓰는 전투 데이터")]
    public StageAsset defaultStage;
    public GameRulesAsset rules;
    [Tooltip("0이면 실행할 때마다 새 시드")]
    public int fixedSeed;

    const int MaxStepsPerFrame = 12;

    public BattleSession Session { get; private set; }
    public BattleContent Content { get; private set; }
    public int Seed { get; private set; }
    public float Speed { get; private set; } = 1f;
    public bool Paused { get; private set; }
    /// <summary>두 틱 사이 보간 비율(0~1). 이동을 부드럽게 그릴 때 쓴다.</summary>
    public float Alpha { get; private set; }

    double accumulator;

    void Awake()
    {
        Time.timeScale = 1f;
        StageAsset stage = defaultStage;
        StageData selected = SceneFlow.CurrentStage;
        if (selected != null && selected.battleStage != null) stage = selected.battleStage;

        Content = BattleContentBuilder.Build(stage, rules);
        if (!Content.IsValid)
        {
            foreach (string error in Content.Errors) Debug.LogError("[전투 데이터] " + error);
            return;
        }

        Seed = fixedSeed != 0 ? fixedSeed : System.Environment.TickCount;
        Session = new BattleSession(Content.Stage, Content.Rules, new SeededRandom(Seed));
        Debug.Log($"[전투] {Content.Stage.Id} 시작, 시드 {Seed}");
        Session.Ended += OnEnded;
    }

    void Update()
    {
        if (Session == null || Paused || Session.Phase == BattlePhase.Ended) return;

        double dt = Content.Rules.FixedDt;
        accumulator += Mathf.Min(Time.deltaTime, 0.25f) * Speed;
        int steps = 0;
        while (accumulator >= dt && steps < MaxStepsPerFrame && Session.Phase != BattlePhase.Ended)
        {
            Session.Tick(dt);
            accumulator -= dt;
            steps++;
        }
        if (steps == MaxStepsPerFrame) accumulator = 0;
        Alpha = (float)(accumulator / dt);
    }

    void OnEnded(BattleResult result, EndReason reason)
    {
        // 스테이지 선택 화면의 해금 기록. 의뢰 정산·보상은 4주차에 SettlementService로 옮긴다.
        if (result == BattleResult.Success) Progress.MarkCleared(SceneFlow.CurrentStageIndex);
        Debug.Log($"[전투] 종료: {result} ({reason}), 중심 HP {Session.CoreHp}, 전투 {Session.CombatTime:0.0}초, 틱 {Session.TickCount}, 시드 {Seed}");
    }

    public void SetSpeed(float speed) => Speed = speed;

    public void SetPaused(bool paused) => Paused = paused;
}
