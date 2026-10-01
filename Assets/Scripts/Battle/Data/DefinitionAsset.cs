using UnityEngine;

// 기획자가 Inspector에서 편집하는 전투 데이터(시스템 기획서 7장의 고정 테이블).
// 전투 시작 시 BattleContentBuilder가 Akmong.Battle 정의로 변환하고 검사한다.

public abstract class DefinitionAsset : ScriptableObject
{
    [Tooltip("불변 문자열 ID. 예: EN_TOY")]
    public string id;
}
