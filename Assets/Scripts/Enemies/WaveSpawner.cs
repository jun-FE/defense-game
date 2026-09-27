using System.Collections;
using UnityEngine;

/// <summary>
/// 웨이브를 순서대로 생성한다. 웨이브가 갈수록 적 수와 체력이 늘고,
/// 5웨이브마다 마지막에 보스가 나온다.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    public Enemy enemyPrefab;
    public PathRoute path;

    [Header("웨이브 진행")]
    public int totalWaves = 10;
    public float firstWaveDelay = 5f;
    public float timeBetweenWaves = 8f;
    public float spawnInterval = 0.8f;

    [Header("적 능력치 (1웨이브 기준)")]
    public int baseCount = 5;
    public int countPerWave = 2;
    public float baseHealth = 4f;
    public float healthGrowth = 1.25f;
    public float baseSpeed = 1.6f;
    public int baseReward = 4;

    public int CurrentWave { get; private set; }
    public float Countdown { get; private set; }
    public bool IsSpawning { get; private set; }
    public bool HasMoreWaves => CurrentWave < totalWaves;

    void Start()
    {
        Countdown = firstWaveDelay;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || IsSpawning) return;

        if (!HasMoreWaves)
        {
            if (Enemy.Active.Count == 0) gm.Win();
            return;
        }

        Countdown -= Time.deltaTime;
        if (Countdown <= 0f) StartNextWave();
    }

    /// <summary>다음 웨이브를 바로 시작한다. 남은 대기 시간만큼 보너스 골드를 준다.</summary>
    public void CallNextWaveEarly()
    {
        if (IsSpawning || !HasMoreWaves) return;
        if (GameManager.Instance != null) GameManager.Instance.AddGold(Mathf.FloorToInt(Countdown));
        StartNextWave();
    }

    void StartNextWave()
    {
        StartCoroutine(SpawnWave(CurrentWave));
    }

    IEnumerator SpawnWave(int index)
    {
        IsSpawning = true;
        CurrentWave = index + 1;

        int count = baseCount + index * countPerWave;
        float hp = baseHealth * Mathf.Pow(healthGrowth, index);
        float speed = baseSpeed + index * 0.05f;
        int reward = baseReward + index / 3;

        for (int i = 0; i < count; i++)
        {
            Spawn(hp, speed, reward, 1f);
            yield return new WaitForSeconds(spawnInterval);
        }

        if (CurrentWave % 5 == 0)
            Spawn(hp * 10f, speed * 0.7f, reward * 10, 1.5f);

        IsSpawning = false;
        Countdown = timeBetweenWaves;
    }

    void Spawn(float hp, float speed, int reward, float scale)
    {
        Enemy enemy = Instantiate(enemyPrefab);
        enemy.transform.localScale = Vector3.one * scale;
        enemy.Setup(path, hp, speed, reward);
    }
}
