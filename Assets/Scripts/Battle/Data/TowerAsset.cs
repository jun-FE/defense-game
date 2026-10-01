using System;
using UnityEngine;

[Serializable]
public class TowerLevelData
{
    public string id;
    public float damage = 20f;
    public float range = 4f;
    [Tooltip("공격 사이 간격(초). 최소 0.2")]
    public float attackSec = 1f;
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critMult = 1.5f;
    [Tooltip("다음 단계 강화 비용. 마지막 단계는 0")]
    public int upgradeCost;
}

[CreateAssetMenu(menuName = "Defense/전투 데이터/Tower", fileName = "TW_NEW")]
public class TowerAsset : DefinitionAsset
{
    public string displayName;
    public int buildCost = 50;
    public TowerLevelData[] levels = new TowerLevelData[0];

    [Header("표시")]
    [Tooltip("타일 중심에 놓이는 프리팹. 자식에 TowerVisual, 'FirePoint'가 있으면 사용한다.")]
    public GameObject prefab;
    public Sprite icon;
    public Sprite iconDisabled;
    [Tooltip("연출용 투사체(오른쪽이 앞). 피해는 발사 순간 즉시 계산된다.")]
    public Sprite projectile;
}
