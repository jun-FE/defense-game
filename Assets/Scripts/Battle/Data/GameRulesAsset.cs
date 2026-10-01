using UnityEngine;

[CreateAssetMenu(menuName = "Defense/전투 데이터/Game Rule", fileName = "RULE_BASE")]
public class GameRulesAsset : DefinitionAsset
{
    public float armorConstant = 100f;
    public float minAttackSec = 0.20f;
    [Tooltip("전투 시뮬레이션 간격(초). 0.05 = 20Hz")]
    public float fixedDt = 0.05f;
    public float minMoveRatio = 0.25f;
    public float maxMoveRatio = 2.0f;
}
