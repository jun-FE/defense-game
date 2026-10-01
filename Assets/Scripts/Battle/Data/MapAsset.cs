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
}
