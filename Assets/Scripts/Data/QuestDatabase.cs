using UnityEngine;

/// <summary>의뢰함에 보이는 의뢰 목록. Assets/Resources/QuestDatabase 에 있어야 한다.</summary>
[CreateAssetMenu(menuName = "Defense/Quest Database", fileName = "QuestDatabase")]
public class QuestDatabase : ScriptableObject
{
    public const string ResourcePath = "QuestDatabase";

    [Tooltip("의뢰함 최대 칸 수 (기획 시안: 10)")]
    public int capacity = 10;
    public QuestData[] quests = new QuestData[0];

    static QuestDatabase instance;

    public static QuestDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<QuestDatabase>(ResourcePath);
            if (instance == null) instance = CreateInstance<QuestDatabase>();
            return instance;
        }
    }
}
