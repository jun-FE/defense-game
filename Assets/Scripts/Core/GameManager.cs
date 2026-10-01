using UnityEngine;

public enum GameState { Playing, Victory, Defeat }

/// <summary>골드, 라이프, 일시정지, 승패 상태를 관리한다. 씬마다 하나.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("스테이지 데이터가 없을 때(이 씬을 직접 실행할 때 등) 쓰는 기본값")]
    public int startGold = 120;
    public int startLives = 20;

    public StageData Stage { get; private set; }
    public int Gold { get; private set; }
    public int Lives { get; private set; }
    public GameState State { get; private set; }
    public bool IsPaused { get; private set; }
    public float GameSpeed { get; private set; } = 1f;

    void Awake()
    {
        Instance = this;
        Stage = SceneFlow.CurrentStage;
        Gold = Stage != null ? Stage.startGold : startGold;
        Lives = Stage != null ? Stage.startLives : startLives;
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TrySpend(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        return true;
    }

    public void AddGold(int amount) => Gold += amount;

    public void LoseLife(int amount)
    {
        if (State != GameState.Playing) return;
        Lives = Mathf.Max(0, Lives - amount);
        if (Lives == 0) EndGame(GameState.Defeat);
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        Progress.MarkCleared(SceneFlow.CurrentStageIndex);
        EndGame(GameState.Victory);
    }

    public void SetSpeed(float speed)
    {
        GameSpeed = speed;
        if (State == GameState.Playing && !IsPaused) Time.timeScale = speed;
    }

    public void TogglePause()
    {
        if (State != GameState.Playing) return;
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : GameSpeed;
    }

    public void Restart() => SceneFlow.RestartStage();

    void EndGame(GameState result)
    {
        State = result;
        IsPaused = false;
        Time.timeScale = 0f;
    }
}
