using System;
using UnityEngine;

[Serializable]
public class TowerLevelData
{
    public string id;
    public float damage = 20f;
    public float range = 4f;
    [Label("공격속도 (초)")]
    [Tooltip("한 번 공격한 뒤 다음 공격까지 걸리는 시간(초). 낮을수록 빠르다. 최소 0.2초 (기획서 TowerLevel.attack_sec)")]
    public float attackSec = 1f;
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critMult = 1.5f;
    [Tooltip("다음 단계 강화 비용. 마지막 단계는 0")]
    public int upgradeCost;

    [Header("근거리 저지 (병정인형)")]
    [Label("저지 인원")]
    [Tooltip("동시에 붙잡아 둘 수 있는 적 수. 0이면 저지하지 않는다.")]
    public int blockCount;
    [Label("저지 시간 (초)")]
    [Tooltip("한 적을 붙잡아 둘 수 있는 최대 시간. 다 되면 그 적은 이 타워를 지나간다.")]
    public float blockSec;
    [Label("밀어내기 (타일)")]
    [Tooltip("공격할 때 적을 길 뒤로 미는 거리. 보스는 면역.")]
    public float knockback;

    [Header("감속 (스노우볼)")]
    [Label("감속 배율")]
    [Tooltip("맞은 적의 이동 속도에 곱한다. 0.7이면 30% 느려짐, 1이면 감속 없음. 보스는 효과가 절반.")]
    [Range(0.1f, 1f)] public float slowMult = 1f;
    [Label("감속 시간 (초)")]
    [Tooltip("감속이 이어지는 시간. 다시 맞으면 새로 이어진다.")]
    public float slowSec;
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
