using UnityEngine;

/// <summary>모든 스테이지 목록. Assets/Resources/StageDatabase 에 있어야 실행 중에 불러올 수 있다.</summary>
[CreateAssetMenu(menuName = "Defense/Stage Database", fileName = "StageDatabase")]
public class StageDatabase : ScriptableObject
{
    public const string ResourcePath = "StageDatabase";

    public StageData[] stages = new StageData[0];

    static StageDatabase instance;

    public static StageDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<StageDatabase>(ResourcePath);
            if (instance == null) instance = CreateInstance<StageDatabase>();
            return instance;
        }
    }

    public int Count => stages.Length;

    public StageData Get(int index) => index >= 0 && index < stages.Length ? stages[index] : null;
}
