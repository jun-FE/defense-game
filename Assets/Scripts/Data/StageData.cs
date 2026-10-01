using UnityEngine;

/// <summary>스테이지 하나의 설정. Create > Defense > Stage 로 만들고 StageDatabase에 등록한다.</summary>
[CreateAssetMenu(menuName = "Defense/Stage", fileName = "NewStage")]
public class StageData : ScriptableObject
{
    public string stageId = "1-1";
    public string title = "새 스테이지";

    [Tooltip("스테이지 시작 전에 보여줄 스토리. 없으면 바로 게임이 시작된다.")]
    public StoryData introStory;

    public int startGold = 120;
    public int startLives = 20;
    public int totalWaves = 10;
    [Tooltip("적 체력 배율. 1.2면 20% 더 단단하다.")]
    public float enemyHealthMultiplier = 1f;
}
