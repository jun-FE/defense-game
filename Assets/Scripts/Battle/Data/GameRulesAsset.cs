using UnityEngine;

[CreateAssetMenu(menuName = "Defense/전투 데이터/Game Rule", fileName = "RULE_BASE")]
public class GameRulesAsset : DefinitionAsset
{
    public float armorConstant = 100f;
    [Label("공격속도 하한 (초)")]
    [Tooltip("어떤 타워도 이보다 빨리 공격하지 못한다 (기획서 GameRule.min_attack_sec)")]
    public float minAttackSec = 0.20f;
    [Tooltip("전투 시뮬레이션 간격(초). 0.05 = 20Hz")]
    public float fixedDt = 0.05f;
    public float minMoveRatio = 0.25f;
    public float maxMoveRatio = 2.0f;
}
