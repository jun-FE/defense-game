using UnityEngine;

[CreateAssetMenu(menuName = "Defense/전투 데이터/Enemy", fileName = "EN_NEW")]
public class EnemyAsset : DefinitionAsset
{
    public string displayName;
    public int maxHp = 100;
    public float armor = 25f;
    [Tooltip("월드 단위(타일)/초")]
    public float moveSpeed = 1.5f;
    [Tooltip("중심 도달 시 피해")]
    public int coreDamage = 5;
    [Tooltip("처치 시 전투 재화")]
    public int killCoin = 3;
    [Tooltip("보스는 밀어내기에 면역")]
    public bool isBoss;

    [Header("표시")]
    public Sprite sprite;
    public Color tint = Color.white;
    public float scale = 0.6f;
}
