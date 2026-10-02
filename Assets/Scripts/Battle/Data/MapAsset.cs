using System;
using UnityEngine;

[Serializable]
public class SpawnPointData
{
    public string id;
    [Tooltip("출현 위치부터 중심 접근 위치까지. 최소 2개")]
    public Vector2[] path = new Vector2[0];
}

[CreateAssetMenu(menuName = "Defense/전투 데이터/Map", fileName = "MAP_NEW")]
public class MapAsset : DefinitionAsset
{
    public Vector2 corePos;
    public SpawnPointData[] spawnPoints = new SpawnPointData[0];
    [Tooltip("건설 가능 영역 (x, y, 폭, 높이). 타일 중심이 안에 있으면 건설 가능")]
    public Rect[] buildZones = new Rect[0];
    [Tooltip("카메라가 보여줄 영역 (x, y, 폭, 높이)")]
    public Rect cameraBounds = new Rect(-8, -4, 26, 17);
    [Tooltip("건설 칸 중심이 길 중심선에서 이만큼(타일) 떨어져야 지을 수 있다. 0이면 건설 구역만 본다.")]
    public float pathClearance;

    [Header("그림 (없으면 임시 도형으로 그린다)")]
    [Tooltip("맵 바닥 그림. 카메라 영역(cameraBounds)에 꽉 맞춰 깔린다.")]
    public Sprite background;
    [Tooltip("몬스터·타워보다 앞에 보이는 투명 PNG(선택). background와 같은 크기.")]
    public Sprite foreground;
}
