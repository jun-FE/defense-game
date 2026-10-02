using UnityEngine;

/// <summary>
/// 의뢰 하나(시스템 기획서 Quest 테이블의 프로토타입판). 의뢰함 목록과 편지 카드에 보인다.
/// Project 창에서 우클릭 > Create > Defense > Quest 로 만들고 QuestDatabase에 등록한다.
/// </summary>
[CreateAssetMenu(menuName = "Defense/Quest", fileName = "Q_NEW")]
public class QuestData : ScriptableObject
{
    [Tooltip("불변 ID. 예: Q_MAIN_01")]
    public string questId = "Q_NEW";
    public string title = "새 의뢰";
    [Tooltip("#없이 입력. 예: 인형, 상실")]
    public string[] tags = new string[0];
    [TextArea(3, 8)]
    public string letter = "";
    [Tooltip("의뢰 보상 표시 문구 (보상 지급은 4주차 정산에서 연결)")]
    public string[] rewards = new string[0];
    public Sprite thumbnail;
    public Sprite photo;

    [Tooltip("이 의뢰가 시작하는 스테이지(StageDatabase 순서). -1이면 아직 준비 중")]
    public int stageIndex = -1;

    /// <summary>준비된 스테이지가 있고, 그 스테이지가 열려 있으면 수락할 수 있다.</summary>
    public bool IsAvailable => stageIndex >= 0 && stageIndex < StageDatabase.Instance.Count && Progress.IsUnlocked(stageIndex);

    public bool IsCompleted => stageIndex >= 0 && Progress.IsCleared(stageIndex);

    public bool IsInProgress => !IsCompleted && QuestProgress.IsAccepted(questId);
}
