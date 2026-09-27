using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Victory, Defeat }

/// <summary>골드, 라이프, 승패 상태를 관리한다. 씬마다 하나.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int startGold = 120;
    public int startLives = 20;

    public int Gold { get; private set; }
    public int Lives { get; private set; }
    public GameState State { get; private set; }

    void Awake()
    {
        Instance = this;
        Gold = startGold;
        Lives = startLives;
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
        if (State == GameState.Playing) EndGame(GameState.Victory);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void EndGame(GameState result)
    {
        State = result;
        Time.timeScale = 0f;
    }
}
