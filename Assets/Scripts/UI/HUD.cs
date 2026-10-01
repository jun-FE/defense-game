using UnityEngine;

/// <summary>
/// 게임 화면 UI와 입력 처리(IMGUI).
/// IMGUI 이벤트는 Input Manager / Input System 설정과 상관없이 동작한다.
/// </summary>
public class HUD : MonoBehaviour
{
    public Camera worldCamera;
    public BuildManager buildManager;
    public WaveSpawner spawner;

    GUIStyle label;
    GUIStyle centerLabel;
    GUIStyle button;
    string message;
    float messageUntil;

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        CreateStyles();

        // 일시정지/결과 화면이 떠 있으면 아래 UI는 클릭되지 않게 한다.
        bool overlay = gm.State != GameState.Playing || gm.IsPaused;
        GUI.enabled = !overlay;
        DrawTopBar(gm);
        DrawTowerButtons(gm);
        DrawMessage();
        GUI.enabled = true;

        if (gm.State != GameState.Playing) DrawResult(gm);
        else if (gm.IsPaused) DrawPause(gm);

        HandleInput(gm, Event.current, overlay);
    }

    void CreateStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        label.normal.textColor = Color.white;
        centerLabel = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
    }

    void DrawTopBar(GameManager gm)
    {
        GUILayout.BeginArea(new Rect(12, 8, Screen.width - 24, 44));
        GUILayout.BeginHorizontal();
        if (gm.Stage != null) GUILayout.Label($"{gm.Stage.stageId} {gm.Stage.title}", label, GUILayout.Width(220));
        GUILayout.Label($"골드 {gm.Gold}", label, GUILayout.Width(120));
        GUILayout.Label($"라이프 {gm.Lives}", label, GUILayout.Width(120));
        GUILayout.Label($"웨이브 {spawner.CurrentWave}/{spawner.totalWaves}", label, GUILayout.Width(140));
        if (!spawner.IsSpawning && spawner.HasMoreWaves)
        {
            GUILayout.Label($"다음 웨이브 {Mathf.CeilToInt(spawner.Countdown)}초", label, GUILayout.Width(180));
            if (GUILayout.Button("지금 시작 [Space]", button, GUILayout.Width(170))) spawner.CallNextWaveEarly();
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(gm.GameSpeed == 1f ? "▶ x1" : "x1", button, GUILayout.Width(60))) gm.SetSpeed(1f);
        if (GUILayout.Button(gm.GameSpeed == 2f ? "▶ x2" : "x2", button, GUILayout.Width(60))) gm.SetSpeed(2f);
        if (GUILayout.Button("일시정지 [Esc]", button, GUILayout.Width(140))) gm.TogglePause();
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

        Color old = GUI.color;
        for (int i = 0; i < towers.Length; i++)
        {
            Tower tower = towers[i];
            bool selected = i == buildManager.SelectedIndex;
            GUI.color = gm.Gold >= tower.cost ? Color.white : new Color(1f, 0.6f, 0.6f);
            string text = $"{(selected ? "▶ " : "")}[{i + 1}] {tower.displayName}\n{tower.cost} 골드";
            if (GUI.Button(new Rect(x + i * (width + 10f), y, width, height), text, button)) buildManager.Select(i);
        }
        GUI.color = old;
    }

    void DrawMessage()
    {
        if (string.IsNullOrEmpty(message) || Time.unscaledTime > messageUntil) return;
        GUI.Label(new Rect(0, Screen.height - 120, Screen.width, 30), message, centerLabel);
    }

    void DrawPause(GameManager gm)
    {
        UIKit.Begin();
        UIKit.DimScreen();
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0f, 260f, w, 100f), "일시정지", UIKit.Heading);

        float x = (w - 400f) * 0.5f;
        if (UIKit.DrawButton(new Rect(x, 420f, 400f, 80f), "계속하기")) gm.TogglePause();
        if (UIKit.DrawButton(new Rect(x, 520f, 400f, 80f), "다시 하기")) gm.Restart();
        if (UIKit.DrawButton(new Rect(x, 620f, 400f, 80f), "스테이지 선택")) SceneFlow.GoToStageSelect();
        if (UIKit.DrawButton(new Rect(x, 720f, 400f, 80f), "메인 메뉴")) SceneFlow.GoToTitle();
        UIKit.End();
    }

    void DrawResult(GameManager gm)
    {
        UIKit.Begin();
        UIKit.DimScreen();
        float w = UIKit.Width;
        bool victory = gm.State == GameState.Victory;
        UIKit.ShadowLabel(new Rect(0f, 230f, w, 130f), victory ? "승리!" : "패배...", UIKit.Title);
        if (gm.Stage != null)
            GUI.Label(new Rect(0f, 360f, w, 50f), $"{gm.Stage.stageId} {gm.Stage.title}", UIKit.Small);

        float x = (w - 400f) * 0.5f;
        float y = 440f;
        if (victory && SceneFlow.HasNextStage)
        {
            if (UIKit.DrawButton(new Rect(x, y, 400f, 80f), "다음 스테이지")) SceneFlow.StartNextStage();
            y += 100f;
        }
        if (UIKit.DrawButton(new Rect(x, y, 400f, 80f), "다시 하기")) gm.Restart();
        y += 100f;
        if (UIKit.DrawButton(new Rect(x, y, 400f, 80f), "스테이지 선택")) SceneFlow.GoToStageSelect();
        y += 100f;
        if (UIKit.DrawButton(new Rect(x, y, 400f, 80f), "메인 메뉴")) SceneFlow.GoToTitle();
        UIKit.End();
    }

    void HandleInput(GameManager gm, Event e, bool overlay)
    {
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            gm.TogglePause();
            e.Use();
            return;
        }
        if (overlay) return;

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

    void ShowMessage(string text)
    {
        if (text == null) return;
        message = text;
        messageUntil = Time.unscaledTime + 1.5f;
    }
}
