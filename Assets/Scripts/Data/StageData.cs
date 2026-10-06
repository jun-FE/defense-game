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

    [Tooltip("탐색 맵 ID(맵 에디터로 만든 Assets/Resources/Maps/{ID}.json). 있으면 전투 전에 탐색을 하고, " +
             "밝힌 길의 최단 경로가 몬스터 경로가 된다. 비우면 전투 데이터의 맵으로 바로 전투.")]
    public string dreamMapId = "";

    [Tooltip("탐색 판에서 적 HP 배율(전투 데이터의 배율 대신 사용). 예시 맵 봇 기준 1.0: 지름길 98%, 결정 많은 우회로 100%, 결정 적은 우회로 29% 성공")]
    public float dreamEnemyHpScale = 1f;
}
