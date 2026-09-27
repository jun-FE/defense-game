using UnityEngine;

/// <summary>
/// 프로토타입용 화면 UI와 입력 처리(IMGUI).
/// IMGUI 이벤트는 Input Manager / Input System 설정과 상관없이 동작한다.
/// 나중에 uGUI나 UI Toolkit으로 바꾸면 된다.
/// </summary>
public class HUD : MonoBehaviour
{
    public Camera worldCamera;
    public BuildManager buildManager;
    public WaveSpawner spawner;

    GUIStyle label;
    GUIStyle title;
    GUIStyle button;
    string message;
    float messageUntil;

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        CreateStyles();

        DrawTopBar(gm);
        DrawTowerButtons(gm);
        DrawMessage();
        if (gm.State != GameState.Playing) DrawResult(gm);

        HandleInput(gm, Event.current);
    }

    void CreateStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        label.normal.textColor = Color.white;
        title = new GUIStyle(label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
        button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
    }

    void DrawTopBar(GameManager gm)
    {
        GUILayout.BeginArea(new Rect(12, 8, Screen.width - 24, 44));
        GUILayout.BeginHorizontal();
        GUILayout.Label($"골드 {gm.Gold}", label, GUILayout.Width(140));
        GUILayout.Label($"라이프 {gm.Lives}", label, GUILayout.Width(140));
        GUILayout.Label($"웨이브 {spawner.CurrentWave}/{spawner.totalWaves}", label, GUILayout.Width(160));
        if (!spawner.IsSpawning && spawner.HasMoreWaves)
        {
            GUILayout.Label($"다음 웨이브 {Mathf.CeilToInt(spawner.Countdown)}초", label, GUILayout.Width(200));
            if (GUILayout.Button("지금 시작 [Space]", button, GUILayout.Width(180))) spawner.CallNextWaveEarly();
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("x1", button, GUILayout.Width(50))) SetSpeed(gm, 1f);
        if (GUILayout.Button("x2", button, GUILayout.Width(50))) SetSpeed(gm, 2f);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void DrawTowerButtons(GameManager gm)
    {
        const float width = 190f;
        const float height = 56f;
        Tower[] towers = buildManager.towerPrefabs;
        float x = (Screen.width - towers.Length * (width + 10f)) * 0.5f;
        float y = Screen.height - height - 12f;

        for (int i = 0; i < towers.Length; i++)
        {
            Tower tower = towers[i];
            bool selected = i == buildManager.SelectedIndex;
            GUI.color = gm.Gold >= tower.cost ? Color.white : new Color(1f, 0.6f, 0.6f);
            string text = $"{(selected ? "▶ " : "")}[{i + 1}] {tower.displayName}\n{tower.cost} 골드";
            if (GUI.Button(new Rect(x + i * (width + 10f), y, width, height), text, button)) buildManager.Select(i);
        }
        GUI.color = Color.white;
    }

    void DrawMessage()
    {
        if (string.IsNullOrEmpty(message) || Time.unscaledTime > messageUntil) return;
        GUI.Label(new Rect(0, Screen.height - 120, Screen.width, 30), message,
            new GUIStyle(label) { alignment = TextAnchor.MiddleCenter });
    }

    void DrawResult(GameManager gm)
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        string text = gm.State == GameState.Victory ? "승리!" : "패배...";
        GUI.Label(new Rect(0, Screen.height * 0.5f - 80, Screen.width, 80), text, title);
        if (GUI.Button(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 10, 200, 50), "다시 하기", button))
            gm.Restart();
    }

    void HandleInput(GameManager gm, Event e)
    {
        if (gm.State != GameState.Playing) return;

        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Space) { spawner.CallNextWaveEarly(); e.Use(); }
            else if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            {
                buildManager.Select(e.keyCode - KeyCode.Alpha1);
                e.Use();
            }
        }
        // 버튼이 클릭을 먼저 소비하므로, 여기까지 온 클릭은 게임 화면 클릭이다.
        else if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2 screen = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            Vector2 world = worldCamera.ScreenToWorldPoint(screen);
            ShowMessage(buildManager.TryBuildAt(world));
            e.Use();
        }
    }

    void SetSpeed(GameManager gm, float scale)
    {
        if (gm.State == GameState.Playing) Time.timeScale = scale;
    }

    void ShowMessage(string text)
    {
        if (text == null) return;
        message = text;
        messageUntil = Time.unscaledTime + 1.5f;
    }
}
