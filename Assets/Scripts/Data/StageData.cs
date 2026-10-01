using UnityEngine;

/// <summary>스테이지 하나의 설정. Create > Defense > Stage 로 만들고 StageDatabase에 등록한다.</summary>
[CreateAssetMenu(menuName = "Defense/Stage", fileName = "NewStage")]
public class StageData : ScriptableObject
{
    public string stageId = "1-1";
    public string title = "새 스테이지";

    [Tooltip("스테이지 시작 전에 보여줄 스토리. 없으면 바로 게임이 시작된다.")]
    public StoryData introStory;

    [Tooltip("이 스테이지의 전투 데이터(맵, 웨이브, 시작 재화, 지을 수 있는 타워)")]
    public StageAsset battleStage;
}
