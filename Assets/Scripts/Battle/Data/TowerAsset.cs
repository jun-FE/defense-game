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

    [Header("여러 적 동시 공격 (가위·바늘꽂이)")]
    [Label("한 번에 맞히는 적 수")]
    [Tooltip("사거리 안에서 중심에 가까운 순으로 이만큼 한꺼번에 친다. 1이면 한 적.")]
    [Min(1)] public int hitCount = 1;

    [Header("표식 (바늘꽂이·수선표 활)")]
    [Label("방어 감소")]
    [Tooltip("맞은 적의 방어력을 이만큼 깎는다. 0이면 없음.")]
    public float armorShred;
    [Label("받는 피해 증가")]
    [Tooltip("맞은 적이 모든 타워에게 이 비율만큼 더 맞는다(0.2 = 20%). 보스는 절반. 0이면 없음.")]
    [Range(0f, 1f)] public float vulnerability;
    [Label("표식 시간 (초)")]
    [Tooltip("표식이 이어지는 시간. 다시 맞으면 새로 이어진다.")]
    public float markSec;
}

/// <summary>공격 연출 종류(BattleFx). fancyShot을 켠 타워만 쓴다.</summary>
public enum TowerFxKind
{
    [InspectorName("바늘(재봉틀)")] Needle,
    [InspectorName("빛줄기(스탠드 조명)")] Beam,
    [InspectorName("가위질(가위)")] Snip,
    [InspectorName("줄자 휘감기(줄자)")] Whip,
    [InspectorName("시침핀(바늘꽂이)")] Pins,
    [InspectorName("꼬리표 바늘(수선표 활)")] Tag,
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

    [Header("화려한 공격 연출(BattleFx)")]
    [Tooltip("켜면 발사 불꽃 + 꿈실 꼬리를 남기는 투사체 + 명중 폭발로 그린다(projectile을 바늘로 사용).")]
    public bool fancyShot;
    public TowerFxKind fxKind = TowerFxKind.Needle;
    public Color fxColor = new Color(0.72f, 0.55f, 1f);
    public Sprite fxSpark;
    public Sprite fxRing;
    public Sprite fxGlow;
    [Tooltip("가위질 연출용 칼날 빛(가위). 비우면 빛 점으로 대신한다.")]
    public Sprite fxSlash;
}
