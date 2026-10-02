using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 프로토타입 전투 UI(IMGUI, 1080p 기준 좌표). 5주차에 uGUI로 교체한다.
/// 입력은 명령으로만 전달한다: 건설 / 강화 / 웨이브 시작 / 속도 / 일시정지 / 귀환.
/// 조작: 타워 버튼(또는 1~9) → 밝은 칸 클릭으로 건설, 우클릭·Esc 취소.
///       타워 클릭 → 정보·강화. Space 웨이브 바로 시작. Esc 일시정지.
///       개발용(에디터·개발 빌드): F1 재화 +100, F2 적 전부 처치.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    public BattleController controller;
    public BattleView view;
    public Camera worldCamera;
    public Sprite square;
    public Sprite circle;

    const float TopBarHeight = 96f;
    const float BottomBarHeight = 130f;
    const float PanelWidth = 360f;

    BattleSession Session => controller.Session;

    TowerDef buildSelection;
    TowerState selectedTower;
    bool pauseMenu;
    string message;
    float messageUntil;
    Vector2 mouseScreen;       // IMGUI 좌표(왼쪽 위 원점, 픽셀)
    Vector2Int hoverTile;
    bool hoverOnMap;

    SpriteRenderer tilePreview;
    SpriteRenderer rangePreview;
    GUIStyle hudLabel, hudSmall, hudBig, cardStyle;

    void Start()
    {
        tilePreview = CreatePreview("TilePreview", square, 7);
        rangePreview = CreatePreview("RangePreview", circle, -5);
    }

    void Update()
    {
        UpdatePreview();
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.Repaint || e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
            mouseScreen = e.mousePosition;

        UIKit.Begin();
        EnsureStyles();
        if (Session == null)
        {
            DrawDataErrors();
            UIKit.End();
            return;
        }

        bool overlay = pauseMenu || Session.Phase == BattlePhase.Ended;
        GUI.enabled = !overlay;
        DrawTopBar();
        DrawBuildBar();
        if (selectedTower != null) DrawTowerPanel();
        DrawMessage();
        GUI.enabled = true;

        if (Session.Phase == BattlePhase.Ended) DrawResult();
        else if (pauseMenu) DrawPauseMenu();

        Vector2 virtualMouse = mouseScreen / UIKit.Scale;
        hoverOnMap = !overlay && !IsOverUI(virtualMouse);
        UIKit.End();

        HandleInput(e, overlay);
    }

    // ───────── 그리기 ─────────

    void DrawTopBar()
    {
        float w = UIKit.Width;
        GUI.Box(new Rect(0, 0, w, TopBarHeight), GUIContent.none, UIKit.Panel);

        string stageName = string.IsNullOrEmpty(Session.Stage.Name) ? Session.Stage.Id : Session.Stage.Name;
        GUI.Label(new Rect(32, 14, 360, 34), stageName, hudLabel);
        GUI.Label(new Rect(32, 50, 360, 30), $"웨이브 {Session.WaveIndex + 1} / {Session.Stage.Waves.Count}", hudSmall);

        // 중심 체력(가장 중요한 정보: 가운데에 크게)
        float barX = w * 0.5f - 260f;
        GUI.Label(new Rect(barX, 10, 520, 30), $"꿈의 중심  {Session.CoreHp} / {Session.Stage.CoreMaxHp}", hudSmall);
        DrawBar(new Rect(barX, 44, 520, 22), (float)Session.CoreHp / Session.Stage.CoreMaxHp, new Color(1f, 0.75f, 0.35f));

        GUI.Label(new Rect(barX + 560, 18, 220, 50), $"재화 {Session.Coin}", hudBig);

        float rightX = w - 470f;
        if (Session.Phase == BattlePhase.Prepare)
        {
            GUI.Label(new Rect(rightX - 230, 14, 220, 30), $"준비 {Mathf.CeilToInt((float)Session.PrepareRemaining)}초", hudLabel);
            if (GUI.Button(new Rect(rightX - 230, 48, 210, 38), "바로 시작 [Space]", UIKit.ButtonSmall)) Session.StartWaveNow();
        }
        else if (Session.Phase == BattlePhase.Combat)
        {
            GUI.Label(new Rect(rightX - 230, 14, 220, 30), $"남은 적 {Session.AliveCount + Session.RemainingSpawns}", hudLabel);
        }

        SpeedButton(new Rect(rightX, 22, 90, 52), "x1", 1f);
        SpeedButton(new Rect(rightX + 100, 22, 90, 52), "x2", 2f);
        SpeedButton(new Rect(rightX + 200, 22, 90, 52), "x4", 4f);
        if (GUI.Button(new Rect(rightX + 300, 22, 140, 52), "일시정지", UIKit.ButtonSmall)) SetPauseMenu(true);
    }

    void SpeedButton(Rect rect, string text, float speed)
    {
        bool active = Mathf.Approximately(controller.Speed, speed);
        if (GUI.Button(rect, text, active ? UIKit.ButtonSelected : UIKit.ButtonSmall)) controller.SetSpeed(speed);
    }

    void DrawBuildBar()
    {
        float w = UIKit.Width, h = UIKit.Height;
        var towers = Session.Stage.Towers;
        const float cardW = 230f, cardH = 104f, gap = 16f;
        float x = (w - towers.Count * cardW - (towers.Count - 1) * gap) / 2f;
        float y = h - BottomBarHeight + 13f;
        GUI.Box(new Rect(0, h - BottomBarHeight, w, BottomBarHeight), GUIContent.none, UIKit.Panel);

        for (int i = 0; i < towers.Count; i++)
        {
            TowerDef tower = towers[i];
            TowerAsset asset;
            controller.Content.Towers.TryGetValue(tower, out asset);
            bool affordable = Session.Coin >= tower.BuildCost;
            Sprite icon = asset == null ? null : affordable || asset.iconDisabled == null ? asset.icon : asset.iconDisabled;
            string text = $"[{i + 1}] {tower.Name}\n{tower.BuildCost}";
            GUIStyle style = buildSelection == tower ? UIKit.ButtonSelected : cardStyle;
            var rect = new Rect(x + i * (cardW + gap), y, cardW, cardH);
            if (GUI.Button(rect, new GUIContent(text, icon != null ? icon.texture : null), style)) ToggleBuild(tower);
        }

        GUI.Label(new Rect(24, h - BottomBarHeight + 16, 520, 100),
            buildSelection != null
                ? "밝은 칸을 클릭해 건설 · 우클릭/Esc 취소"
                : "타워를 고르고 밝은 칸에 지으세요.\n지은 타워를 클릭하면 강화할 수 있어요.",
            hudSmall);
    }

    void DrawTowerPanel()
    {
        float w = UIKit.Width;
        var rect = new Rect(w - PanelWidth - 24, TopBarHeight + 24, PanelWidth, 330);
        GUI.Box(rect, GUIContent.none, UIKit.Panel);
        TowerLevelDef level = selectedTower.Level;
        float x = rect.x + 24, y = rect.y + 18;
        GUI.Label(new Rect(x, y, PanelWidth - 48, 40), $"{selectedTower.Def.Name}  {selectedTower.LevelIndex + 1}단계", hudLabel);
        y += 52;
        GUI.Label(new Rect(x, y, PanelWidth - 48, 30), $"공격력 {level.Damage:0.#}   공격속도 {BattleMath.AttackInterval(level.AttackSec, Session.Rules):0.##}초", hudSmall);
        y += 34;
        GUI.Label(new Rect(x, y, PanelWidth - 48, 30), $"사거리 {level.Range:0.#}   치명 {level.CritChance * 100:0}% ×{level.CritMult:0.##}", hudSmall);
        y += 34;
        GUI.Label(new Rect(x, y, PanelWidth - 48, 30), $"투자 {selectedTower.TotalSpent}", hudSmall);
        y += 52;

        if (selectedTower.CanUpgrade)
        {
            TowerLevelDef next = selectedTower.Def.Levels[selectedTower.LevelIndex + 1];
            GUI.Label(new Rect(x, y, PanelWidth - 48, 30), $"다음: 공격력 {next.Damage:0.#}, 사거리 {next.Range:0.#}", hudSmall);
            y += 40;
            if (GUI.Button(new Rect(x, y, PanelWidth - 48, 56), $"강화 ({level.UpgradeCost})", UIKit.ButtonSmall))
            {
                CommandError error = Session.TryUpgrade(selectedTower);
                if (error != CommandError.None) ShowMessage(BattleText.Error(error, level.UpgradeCost));
            }
        }
        else
        {
            GUI.Label(new Rect(x, y, PanelWidth - 48, 30), "최대 단계", hudSmall);
        }
    }

    void DrawMessage()
    {
        if (string.IsNullOrEmpty(message) || Time.unscaledTime > messageUntil) return;
        var rect = new Rect(0, UIKit.Height - BottomBarHeight - 70, UIKit.Width, 50);
        UIKit.ShadowLabel(rect, message, new GUIStyle(hudLabel) { alignment = TextAnchor.MiddleCenter });
    }

    void DrawPauseMenu()
    {
        UIKit.DimScreen();
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0, 220, w, 100), "일시정지", UIKit.Heading);
        float x = (w - 420f) / 2f;
        if (UIKit.DrawButton(new Rect(x, 380, 420, 80), "계속하기")) SetPauseMenu(false);
        if (UIKit.DrawButton(new Rect(x, 480, 420, 80), "처음부터 다시")) SceneFlow.RestartStage();
        if (UIKit.DrawButton(new Rect(x, 580, 420, 80), "중도 귀환")) { SetPauseMenu(false); Session.Retreat(); }
        if (UIKit.DrawButton(new Rect(x, 680, 420, 80), "메인 메뉴")) SceneFlow.GoToTitle();
    }

    void DrawResult()
    {
        UIKit.DimScreen(0.7f);
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0, 180, w, 130), BattleText.Result(Session.Result), UIKit.Title);
        GUI.Label(new Rect(0, 320, w, 40), BattleText.Reason(Session.EndReason), new GUIStyle(hudSmall) { alignment = TextAnchor.MiddleCenter });
        GUI.Label(new Rect(0, 370, w, 40),
            $"남은 중심 HP {Session.CoreHp}   ·   전투 시간 {Session.CombatTime:0}초   ·   웨이브 {Session.WaveIndex + 1}/{Session.Stage.Waves.Count}",
            new GUIStyle(hudSmall) { alignment = TextAnchor.MiddleCenter });

        float x = (w - 420f) / 2f, y = 460f;
        if (Session.Result == BattleResult.Success && SceneFlow.HasNextStage)
        {
            if (UIKit.DrawButton(new Rect(x, y, 420, 80), "다음 스테이지")) SceneFlow.StartNextStage();
            y += 100;
        }
        if (UIKit.DrawButton(new Rect(x, y, 420, 80), "다시 도전")) SceneFlow.RestartStage();
        y += 100;
        if (UIKit.DrawButton(new Rect(x, y, 420, 80), "메인 메뉴")) SceneFlow.GoToTitle();
    }

    void DrawDataErrors()
    {
        UIKit.DimScreen(0.85f);
        GUI.Label(new Rect(80, 80, UIKit.Width - 160, 60), "전투 데이터 오류로 시작할 수 없어요", UIKit.Heading);
        string list = controller.Content == null ? "" : string.Join("\n", controller.Content.Errors);
        GUI.Label(new Rect(80, 170, UIKit.Width - 160, UIKit.Height - 300), list, hudSmall);
        if (UIKit.DrawButton(new Rect(80, UIKit.Height - 120, 300, 70), "메인 메뉴", UIKit.ButtonSmall)) SceneFlow.GoToTitle();
    }

    void DrawBar(Rect rect, float t, Color color)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(t), rect.height), Texture2D.whiteTexture);
        GUI.color = old;
    }

    // ───────── 입력 ─────────

    void HandleInput(Event e, bool overlay)
    {
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                if (Session.Phase == BattlePhase.Ended) return;
                if (buildSelection != null || selectedTower != null) ClearSelection();
                else SetPauseMenu(!pauseMenu);
                e.Use();
                return;
            }
            if (overlay) return;
            if (e.keyCode == KeyCode.Space) { Session.StartWaveNow(); e.Use(); }
            else if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            {
                int index = e.keyCode - KeyCode.Alpha1;
                if (index < Session.Stage.Towers.Count) ToggleBuild(Session.Stage.Towers[index]);
                e.Use();
            }
            else if (Debug.isDebugBuild && e.keyCode == KeyCode.F1) { Session.DebugAddCoin(100); e.Use(); }
            else if (Debug.isDebugBuild && e.keyCode == KeyCode.F2) { Session.DebugKillAll(); e.Use(); }
            return;
        }

        if (overlay || e.type != EventType.MouseDown || !hoverOnMap) return;

        if (e.button == 1)
        {
            ClearSelection();
            e.Use();
            return;
        }
        if (e.button != 0) return;

        if (buildSelection != null)
        {
            TowerState built;
            CommandError error = Session.TryBuild(buildSelection, hoverTile.x, hoverTile.y, out built);
            if (error != CommandError.None) ShowMessage(BattleText.Error(error, buildSelection.BuildCost));
        }
        else
        {
            selectedTower = Session.TowerAt(hoverTile.x, hoverTile.y);
        }
        e.Use();
    }

    void ToggleBuild(TowerDef tower)
    {
        buildSelection = buildSelection == tower ? null : tower;
        selectedTower = null;
    }

    void ClearSelection()
    {
        buildSelection = null;
        selectedTower = null;
    }

    void SetPauseMenu(bool open)
    {
        pauseMenu = open;
        controller.SetPaused(open);
    }

    bool IsOverUI(Vector2 virtualMouse)
    {
        if (virtualMouse.y < TopBarHeight || virtualMouse.y > UIKit.Height - BottomBarHeight) return true;
        if (selectedTower != null && virtualMouse.x > UIKit.Width - PanelWidth - 48) return true;
        return false;
    }

    void ShowMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        message = text;
        messageUntil = Time.unscaledTime + 1.8f;
    }

    // ───────── 미리보기 ─────────

    void UpdatePreview()
    {
        if (Session == null || worldCamera == null) return;

        var screen = new Vector3(mouseScreen.x, Screen.height - mouseScreen.y, 0f);
        Vector3 world = worldCamera.ScreenToWorldPoint(screen);
        hoverTile = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));

        bool building = buildSelection != null && hoverOnMap && Session.Phase != BattlePhase.Ended;
        tilePreview.enabled = building;
        if (building)
        {
            bool ok = Session.CanBuild(buildSelection, hoverTile.x, hoverTile.y) == CommandError.None;
            tilePreview.transform.position = new Vector3(hoverTile.x, hoverTile.y, 0f);
            tilePreview.transform.localScale = Vector3.one * 0.95f;
            tilePreview.color = ok ? new Color(0.4f, 1f, 0.6f, 0.45f) : new Color(1f, 0.35f, 0.35f, 0.45f);
        }

        float range = 0f;
        Vector3 center = Vector3.zero;
        if (building)
        {
            range = buildSelection.Levels[0].Range;
            center = tilePreview.transform.position;
        }
        else if (selectedTower != null)
        {
            range = selectedTower.Level.Range;
            center = new Vector3(selectedTower.TileX, selectedTower.TileY, 0f);
        }
        rangePreview.enabled = range > 0f;
        if (range > 0f)
        {
            rangePreview.transform.position = center;
            rangePreview.transform.localScale = Vector3.one * range * 2f;
            rangePreview.color = new Color(0.5f, 0.95f, 0.9f, 0.14f);
        }
    }

    SpriteRenderer CreatePreview(string objectName, Sprite sprite, int order)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    void EnsureStyles()
    {
        if (hudLabel != null) return;
        hudLabel = new GUIStyle(UIKit.Body) { fontSize = 30, fontStyle = FontStyle.Bold };
        hudSmall = new GUIStyle(UIKit.Body) { fontSize = 24, wordWrap = true };
        hudBig = new GUIStyle(UIKit.Body) { fontSize = 38, fontStyle = FontStyle.Bold };
        hudBig.normal.textColor = new Color(1f, 0.85f, 0.45f);
        cardStyle = new GUIStyle(UIKit.ButtonSmall) { imagePosition = ImagePosition.ImageLeft, alignment = TextAnchor.MiddleLeft, fontSize = 26 };
    }
}
