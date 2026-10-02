using System.Collections.Generic;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 전투 UI(IMGUI, 1080p 기준 좌표). 배치는 전투 UI 시안(ArtSource/Battle_UI/전투UI_시안.png)을 따른다.
/// - 왼쪽 위: 스테이지 이름, 웨이브, (준비 중) 바로 시작 / 가운데 위: 악몽 침식도
/// - 오른쪽 위: 꿈의 불빛 HP, 몽결정(전투 재화), 일시정지, 배속
/// - 아래: 타워 카드 5칸(병정인형·실타래·스탠드·오르골·드림캐처), 왼쪽 이안, 오른쪽 도하(스킬 자리)
/// 아이콘은 시안에서 잘라 낸 임시 그림(Assets/Art/Battle/UI/mock_*)이다. 정식 UI 아트가 오면 교체한다.
/// 입력은 명령으로만 전달한다: 건설 / 강화 / 웨이브 시작 / 속도 / 일시정지 / 귀환.
/// 조작: 타워 카드(또는 1~5) → 밝은 칸 클릭으로 건설, 우클릭·Esc 취소.
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
    [Tooltip("메인 UI 조각(Assets/Art/Main/UI)과 전투 UI 임시 아이콘(Assets/Art/Battle/UI)")]
    public Sprite[] uiSprites = new Sprite[0];
    [Tooltip("Assets/Art/Main/UI/ui_borders.json")]
    public TextAsset uiBorders;

    /// <summary>타워 카드 5칸. 데이터에 없는 타워는 "준비 중"으로 잠가 보여 준다(타워 역할 기획서 순서).</summary>
    static readonly Slot[] Slots =
    {
        new Slot("TW_SOLDIER", "병정인형", "mock_tower_soldier", "접근한 적을 붙잡아 다른 타워가 칠 시간을 법니다."),
        new Slot("TW_THREAD", "실타래", "mock_tower_thread", "바늘과 실이 적을 꿰뚫고 다음 적으로 이어집니다."),
        new Slot("TW_LAMP", "스탠드", "mock_tower_stand", "빛으로 한 적을 집중 공격합니다."),
        new Slot("TW_MUSICBOX", "오르골", "mock_tower_musicbox", "음파로 주변 적들의 발을 느리게 합니다."),
        new Slot("TW_DREAMCATCHER", "드림캐처", "mock_tower_dreamcatcher", "위험한 적을 붙잡고 받는 피해를 늘립니다."),
    };

    class Slot
    {
        public readonly string Id, Name, Icon, Description;
        public Slot(string id, string name, string icon, string description) { Id = id; Name = name; Icon = icon; Description = description; }
    }

    const float PanelWidth = 380f;
    const float CardW = 150f, CardH = 186f, CardGap = 12f;

    BattleSession Session => controller.Session;

    UISkin skin;
    TowerDef[] slotTowers;
    TowerDef buildSelection;
    TowerState selectedTower;
    bool pauseMenu;
    string message;
    float messageUntil;
    Vector2 mouseScreen;       // IMGUI 좌표(왼쪽 위 원점, 픽셀)
    Vector2Int hoverTile;
    bool hoverOnMap;
    int hoverSlot = -1;
    readonly List<Rect> uiRects = new List<Rect>();

    SpriteRenderer tilePreview;
    SpriteRenderer rangePreview;
    GUIStyle hudSmall;

    void Start()
    {
        tilePreview = CreatePreview("TilePreview", square, 7);
        rangePreview = CreatePreview("RangePreview", circle, -5);
        skin = new UISkin(uiSprites, uiBorders);
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
        if (slotTowers == null) MapSlots();

        uiRects.Clear();
        Vector2 virtualMouse = mouseScreen / UIKit.Scale;
        bool overlay = pauseMenu || Session.Phase == BattlePhase.Ended;
        GUI.enabled = !overlay;
        DrawStagePlate();
        DrawErosion();
        DrawStatus();
        DrawIan();
        DrawDoha();
        DrawTowerCards(virtualMouse);
        if (selectedTower != null) DrawTowerPanel();
        DrawMessage();
        GUI.enabled = true;

        if (Session.Phase == BattlePhase.Ended) DrawResult();
        else if (pauseMenu) DrawPauseMenu();

        hoverOnMap = !overlay && !IsOverUI(virtualMouse);
        UIKit.End();

        HandleInput(e, overlay);
    }

    void MapSlots()
    {
        slotTowers = new TowerDef[Slots.Length];
        for (int i = 0; i < Slots.Length; i++)
        {
            Slot slot = Slots[i];
            slotTowers[i] = Session.Stage.Towers.Find(t => t != null && (t.Id == slot.Id || t.Name == slot.Name));
        }
    }

    /// <summary>UI 영역으로 기록한다(그 위에서는 맵 클릭이 먹히지 않는다).</summary>
    Rect Ui(Rect r)
    {
        uiRects.Add(r);
        return r;
    }

    // ───────── 위 ─────────

    void DrawStagePlate()
    {
        Rect plate = Ui(new Rect(24, 16, 470, 76));
        skin.Frame(plate, "panel_indigo");
        skin.Icon(new Rect(plate.x + 18, plate.y + 14, 48, 48), "icon_moon");
        int number = Mathf.Max(0, SceneFlow.CurrentStageIndex) + 1;
        string stageName = string.IsNullOrEmpty(Session.Stage.Name) ? Session.Stage.Id : Session.Stage.Name;
        GUI.Label(new Rect(plate.x + 78, plate.y, plate.width - 90, plate.height), $"{number:00} · {stageName}", skin.Text(32, UISkin.Light, TextAnchor.MiddleLeft, true));

        Rect wave = Ui(new Rect(60, 96, 300, 44));
        skin.Frame(wave, "bar_indigo");
        string remaining = Session.Phase == BattlePhase.Combat ? $"   ·   남은 적 {Session.AliveCount + Session.RemainingSpawns}" : "";
        GUI.Label(wave, $"웨이브 {Session.WaveIndex + 1} / {Session.Stage.Waves.Count}{remaining}", skin.Text(22, UISkin.Light, TextAnchor.MiddleCenter));

        if (Session.Phase == BattlePhase.Prepare)
        {
            Rect start = Ui(new Rect(60, 148, 300, 56));
            if (GUI.Button(start, $"바로 시작 [Space] · {Mathf.CeilToInt((float)Session.PrepareRemaining)}초", skin.Button(22))) Session.StartWaveNow();
        }
    }

    /// <summary>악몽 침식도(시스템 기획서). 침식 규칙은 2주차에 연결하고 지금은 0으로 둔다.</summary>
    void DrawErosion()
    {
        float w = UIKit.Width;
        Rect r = Ui(new Rect(w / 2f - 380f, 12, 760, 104));
        skin.Frame(r, "panel_indigo");
        float erosion = 0f;
        skin.Icon(new Rect(r.x + 22, r.y + 12, 36, 36), "icon_moon");
        GUI.Label(new Rect(r.x + 66, r.y + 8, 300, 44), "악몽 침식도", skin.Text(26, UISkin.Light, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(r.xMax - 240, r.y + 8, 216, 44), $"<size=34><b>{erosion:0}</b></size> / 100", skin.Text(22, UISkin.Light, TextAnchor.MiddleRight));
        var bar = new Rect(r.x + 24, r.y + 54, r.width - 48, 22);
        skin.Gauge(bar, erosion / 100f);
        int[] marks = { 30, 60, 90 };
        foreach (int mark in marks)
        {
            float x = bar.x + bar.width * mark / 100f;
            GUI.Label(new Rect(x - 30, bar.yMax, 60, 26), mark.ToString(), skin.Text(16, UISkin.Muted, TextAnchor.MiddleCenter));
        }
        int next = 30;
        foreach (int mark in marks) if (erosion < mark) { next = mark; break; }
        GUI.Label(new Rect(r.xMax - 200, bar.yMax, 176, 26), $"다음 임계: {next}", skin.Text(16, UISkin.Gold, TextAnchor.MiddleRight));
    }

    void DrawStatus()
    {
        float w = UIKit.Width;
        Rect hp = Ui(new Rect(w - 574, 16, 236, 80));
        skin.Frame(hp, "panel_indigo");
        skin.Icon(new Rect(hp.x + 12, hp.y + 12, 52, 56), "mock_icon_hp");
        GUI.Label(new Rect(hp.x + 70, hp.y + 8, hp.width - 80, 30), "꿈의 불빛 HP", skin.Text(18, UISkin.Gold, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(hp.x + 70, hp.y + 36, hp.width - 80, 36), $"{Session.CoreHp} / {Session.Stage.CoreMaxHp}", skin.Text(26, UISkin.Light, TextAnchor.MiddleCenter, true));

        Rect coin = Ui(new Rect(w - 328, 16, 180, 80));
        skin.Frame(coin, "panel_indigo");
        skin.Icon(new Rect(coin.x + 12, coin.y + 12, 46, 56), "mock_icon_crystal");
        GUI.Label(new Rect(coin.x + 62, coin.y + 8, coin.width - 70, 30), "몽결정", skin.Text(18, UISkin.Light, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(coin.x + 62, coin.y + 36, coin.width - 70, 36), Session.Coin.ToString(), skin.Text(28, UISkin.Light, TextAnchor.MiddleCenter, true));

        Rect pause = Ui(new Rect(w - 136, 16, 80, 80));
        if (GUI.Button(pause, "<b>II</b>", skin.Button(30))) SetPauseMenu(true);

        // 배속: 시안에는 없지만 테스트에 필요해서 작게 둔다.
        float x = w - 328;
        string[] labels = { "x1", "x2", "x4" };
        float[] speeds = { 1f, 2f, 4f };
        for (int i = 0; i < 3; i++)
        {
            Rect r = Ui(new Rect(x + i * 62, 104, 56, 40));
            bool active = Mathf.Approximately(controller.Speed, speeds[i]);
            if (active)
            {
                GUI.Button(r, labels[i], skin.Button(18));
            }
            else
            {
                if (GUI.Button(r, GUIContent.none, skin.FrameStyle("chip_tag"))) controller.SetSpeed(speeds[i]);
                GUI.Label(r, labels[i], skin.Text(18, UISkin.Muted, TextAnchor.MiddleCenter));
            }
        }
    }

    // ───────── 아래 ─────────

    void DrawIan()
    {
        float h = UIKit.Height;
        Rect r = Ui(new Rect(20, h - 218, 214, 200));
        skin.Frame(r, "panel_indigo");
        skin.Icon(new Rect(r.x + 10, r.y + 10, r.width - 20, 130), "mock_ian");
        var label = new Rect(r.x + 10, r.yMax - 54, r.width - 20, 44);
        skin.Frame(label, "bar_indigo");
        GUI.Label(label, "이안 · 건설 지휘", skin.Text(20, UISkin.Light, TextAnchor.MiddleCenter, true));
    }

    /// <summary>도하 스킬 자리(3주차: 필드 지원). 지금은 모양만 보여 준다.</summary>
    void DrawDoha()
    {
        float w = UIKit.Width, h = UIKit.Height;
        Rect r = Ui(new Rect(w - 404, h - 208, 384, 190));
        skin.Frame(r, "panel_indigo");
        skin.Icon(new Rect(r.x + 18, r.y + 12, 30, 30), "icon_moon");
        GUI.Label(new Rect(r.x + 54, r.y + 8, 200, 38), "도하", skin.Text(24, UISkin.Light, TextAnchor.MiddleLeft, true));
        skin.Icon(new Rect(r.x + 14, r.y + 50, 120, 128), "mock_doha");

        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.55f);
        string[] icons = { "mock_skill_q", "mock_skill_e" };
        string[] names = { "Q · 타워 강화", "E · 악몽 약화" };
        for (int i = 0; i < 2; i++)
        {
            var slot = new Rect(r.x + 150 + i * 116, r.y + 52, 96, 80);
            skin.Icon(slot, icons[i]);
            GUI.Label(new Rect(slot.x - 12, slot.yMax + 2, slot.width + 24, 26), names[i], skin.Text(16, UISkin.Light, TextAnchor.MiddleCenter));
        }
        GUI.color = old;
        GUI.Label(new Rect(r.x + 140, r.yMax - 34, r.width - 150, 26), "스킬은 준비 중 · 우클릭 이동", skin.Text(15, UISkin.Muted, TextAnchor.MiddleCenter));
    }

    void DrawTowerCards(Vector2 virtualMouse)
    {
        float w = UIKit.Width, h = UIKit.Height;
        float total = Slots.Length * CardW + (Slots.Length - 1) * CardGap;
        float x0 = (w - total) / 2f, y = h - CardH - 18f;
        Rect back = Ui(new Rect(x0 - 20, y - 12, total + 40, CardH + 24));
        skin.Frame(back, "panel_indigo");

        hoverSlot = -1;
        for (int i = 0; i < Slots.Length; i++)
        {
            Slot slot = Slots[i];
            TowerDef tower = slotTowers[i];
            var r = new Rect(x0 + i * (CardW + CardGap), y, CardW, CardH);
            bool selected = tower != null && buildSelection == tower;
            bool affordable = tower != null && Session.Coin >= tower.BuildCost;
            if (r.Contains(virtualMouse)) hoverSlot = i;

            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (tower != null) ToggleBuild(tower);
                else ShowMessage($"{slot.Name}는 아직 준비 중이에요");
            }
            skin.Frame(r, selected || (hoverSlot == i && tower != null) ? "card_dark_hover" : "card_dark");

            Color old = GUI.color;
            if (tower == null) GUI.color = new Color(0.6f, 0.6f, 0.7f, 0.5f);
            else if (!affordable) GUI.color = new Color(1f, 1f, 1f, 0.55f);
            skin.Icon(new Rect(r.x + 14, r.y + 12, r.width - 28, 100), slot.Icon);
            GUI.Label(new Rect(r.x + 8, r.y + 10, 30, 30), (i + 1).ToString(), skin.Text(18, UISkin.Light, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(r.x, r.y + 114, r.width, 32), slot.Name, skin.Text(21, UISkin.Light, TextAnchor.MiddleCenter, true));
            if (tower != null)
            {
                skin.Icon(new Rect(r.x + 36, r.y + 148, 26, 30), "mock_icon_crystal");
                GUI.Label(new Rect(r.x + 64, r.y + 146, 60, 34), tower.BuildCost.ToString(),
                    skin.Text(22, affordable ? UISkin.Light : new Color(1f, 0.55f, 0.55f), TextAnchor.MiddleLeft, true));
            }
            else
            {
                GUI.Label(new Rect(r.x, r.y + 146, r.width, 34), "준비 중", skin.Text(18, UISkin.Muted, TextAnchor.MiddleCenter));
            }
            GUI.color = old;
        }

        if (hoverSlot >= 0) DrawCardTooltip(hoverSlot, x0 + hoverSlot * (CardW + CardGap) + CardW / 2f, y - 22f);
    }

    void DrawCardTooltip(int index, float centerX, float bottom)
    {
        Slot slot = Slots[index];
        TowerDef tower = slotTowers[index];
        var r = new Rect(centerX - 190, bottom - 110, 380, 104);
        skin.Frame(r, "panel_indigo");
        GUI.Label(new Rect(r.x + 18, r.y + 8, r.width - 36, 32), slot.Name, skin.Text(22, UISkin.Light, TextAnchor.MiddleLeft, true));
        GUI.Label(new Rect(r.x + 18, r.y + 38, r.width - 36, 30), slot.Description, skin.Text(17, UISkin.Light, TextAnchor.MiddleLeft, false, true));
        string cost = tower != null ? $"설치 비용: 몽결정 {tower.BuildCost}" : "아직 만들어지지 않은 타워예요";
        GUI.Label(new Rect(r.x + 18, r.y + 68, r.width - 36, 30), cost, skin.Text(17, UISkin.Gold, TextAnchor.MiddleLeft));
    }

    void DrawTowerPanel()
    {
        float w = UIKit.Width;
        TowerLevelDef level = selectedTower.Level;
        bool blocks = selectedTower.Blocks;
        Rect rect = Ui(new Rect(w - PanelWidth - 24, 160, PanelWidth, blocks ? 420 : 350));
        skin.Frame(rect, "panel_indigo");
        float x = rect.x + 26, y = rect.y + 20, width = PanelWidth - 52;
        GUI.Label(new Rect(x, y, width, 40), $"{selectedTower.Def.Name}  {selectedTower.LevelIndex + 1}단계", skin.Text(28, UISkin.Light, TextAnchor.MiddleLeft, true));
        y += 50;
        GUI.Label(new Rect(x, y, width, 30), $"공격력 {level.Damage:0.#}   공격속도 {BattleMath.AttackInterval(level.AttackSec, Session.Rules):0.##}초", hudSmall);
        y += 34;
        GUI.Label(new Rect(x, y, width, 30), $"사거리 {level.Range:0.#}   치명 {level.CritChance * 100:0}% ×{level.CritMult:0.##}", hudSmall);
        y += 34;
        if (blocks)
        {
            string push = level.Knockback > 0f ? " · 밀어내기" : "";
            GUI.Label(new Rect(x, y, width, 30), $"저지 {level.BlockCount}명 · {level.BlockSec:0.#}초{push}", hudSmall);
            y += 34;
        }
        GUI.Label(new Rect(x, y, width, 30), $"투자 몽결정 {selectedTower.TotalSpent}", hudSmall);
        y += 48;

        if (selectedTower.CanUpgrade)
        {
            TowerLevelDef next = selectedTower.Def.Levels[selectedTower.LevelIndex + 1];
            string nextText = blocks
                ? $"다음: 공격력 {next.Damage:0.#}\n저지 {next.BlockCount}명 · {next.BlockSec:0.#}초{(next.Knockback > level.Knockback ? " · 밀어내기" : "")}"
                : $"다음: 공격력 {next.Damage:0.#}, 사거리 {next.Range:0.#}";
            float nextH = blocks ? 62f : 30f;
            GUI.Label(new Rect(x, y, width, nextH), nextText, hudSmall);
            y += nextH + 12f;
            if (GUI.Button(new Rect(x, y, width, 60), $"강화 (몽결정 {level.UpgradeCost})", skin.Button(24)))
            {
                CommandError error = Session.TryUpgrade(selectedTower);
                if (error != CommandError.None) ShowMessage(BattleText.Error(error, level.UpgradeCost));
            }
        }
        else
        {
            GUI.Label(new Rect(x, y, width, 30), "최대 단계", skin.Text(22, UISkin.Gold));
        }
    }

    void DrawMessage()
    {
        if (string.IsNullOrEmpty(message) || Time.unscaledTime > messageUntil) return;
        var rect = new Rect(0, UIKit.Height - CardH - 150, UIKit.Width, 50);
        UIKit.ShadowLabel(rect, message, skin.Text(28, UISkin.Gold, TextAnchor.MiddleCenter, true));
    }

    // ───────── 겹쳐 뜨는 화면 ─────────

    void DrawPauseMenu()
    {
        UIKit.DimScreen();
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0, 220, w, 100), "일시정지", UIKit.Heading);
        float x = (w - 420f) / 2f;
        if (GUI.Button(new Rect(x, 380, 420, 80), "계속하기", skin.Button(28))) SetPauseMenu(false);
        if (GUI.Button(new Rect(x, 480, 420, 80), "처음부터 다시", skin.Button(28))) SceneFlow.RestartStage();
        if (GUI.Button(new Rect(x, 580, 420, 80), "중도 귀환", skin.Button(28))) { SetPauseMenu(false); Session.Retreat(); }
        if (GUI.Button(new Rect(x, 680, 420, 80), "수선소로", skin.Button(28))) SceneFlow.GoToMain();
    }

    void DrawResult()
    {
        UIKit.DimScreen(0.7f);
        float w = UIKit.Width;
        UIKit.ShadowLabel(new Rect(0, 180, w, 130), BattleText.Result(Session.Result), UIKit.Title);
        GUI.Label(new Rect(0, 320, w, 40), BattleText.Reason(Session.EndReason), new GUIStyle(hudSmall) { alignment = TextAnchor.MiddleCenter });
        GUI.Label(new Rect(0, 370, w, 40),
            $"남은 꿈의 불빛 {Session.CoreHp}   ·   전투 시간 {Session.CombatTime:0}초   ·   웨이브 {Session.WaveIndex + 1}/{Session.Stage.Waves.Count}",
            new GUIStyle(hudSmall) { alignment = TextAnchor.MiddleCenter });

        float x = (w - 420f) / 2f, y = 460f;
        if (Session.Result == BattleResult.Success && SceneFlow.HasNextStage)
        {
            if (GUI.Button(new Rect(x, y, 420, 80), "다음 스테이지", skin.Button(28))) SceneFlow.StartNextStage();
            y += 100;
        }
        if (GUI.Button(new Rect(x, y, 420, 80), "다시 도전", skin.Button(28))) SceneFlow.RestartStage();
        y += 100;
        if (GUI.Button(new Rect(x, y, 420, 80), "수선소로", skin.Button(28))) SceneFlow.GoToMain();
    }

    void DrawDataErrors()
    {
        UIKit.DimScreen(0.85f);
        GUI.Label(new Rect(80, 80, UIKit.Width - 160, 60), "전투 데이터 오류로 시작할 수 없어요", UIKit.Heading);
        string list = controller.Content == null ? "" : string.Join("\n", controller.Content.Errors);
        GUI.Label(new Rect(80, 170, UIKit.Width - 160, UIKit.Height - 300), list, hudSmall);
        if (UIKit.DrawButton(new Rect(80, UIKit.Height - 120, 300, 70), "수선소로", UIKit.ButtonSmall)) SceneFlow.GoToMain();
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
                if (slotTowers != null && index < slotTowers.Length)
                {
                    if (slotTowers[index] != null) ToggleBuild(slotTowers[index]);
                    else ShowMessage($"{Slots[index].Name}는 아직 준비 중이에요");
                }
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
        foreach (Rect r in uiRects)
            if (r.Contains(virtualMouse)) return true;
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

        bool choosing = buildSelection != null && Session.Phase != BattlePhase.Ended;
        if (view != null && view.mapView != null) view.mapView.ShowBuildTiles(choosing);

        bool building = choosing && hoverOnMap;
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
        if (hudSmall != null) return;
        hudSmall = new GUIStyle(UIKit.Body) { fontSize = 22, wordWrap = true };
        hudSmall.normal.textColor = UISkin.Light;
    }
}
