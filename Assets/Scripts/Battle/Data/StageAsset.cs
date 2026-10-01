using System;
using UnityEngine;

[Serializable]
public class SpawnGroupData
{
    public string id;
    [Tooltip("Map의 SpawnPoint ID")]
    public string spawnId;
    public EnemyAsset enemy;
    public int count = 1;
    public float startSec;
    public float intervalSec = 1f;
}

[Serializable]
public class WaveData
{
    public string id;
    public float prepareSec = 10f;
    public int clearCoin = 20;
    public SpawnGroupData[] groups = new SpawnGroupData[0];
}

[CreateAssetMenu(menuName = "Defense/전투 데이터/Stage", fileName = "STG_NEW")]
public class StageAsset : DefinitionAsset
{
    public string displayName;
    public MapAsset map;
    public int startCoin = 120;
    public int coreMaxHp = 100;
    public TowerAsset[] towers = new TowerAsset[0];
    public WaveData[] waves = new WaveData[0];
}
