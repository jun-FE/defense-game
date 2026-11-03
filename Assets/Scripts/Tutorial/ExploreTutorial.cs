using UnityEngine;

/// <summary>
/// 프롤로그 · 꿈속 방 탐색 안내(ArtSource/Prologue/프롤로그_화면구성.xlsx 01~03, 06). ExploreUI가 튜토리얼 중일 때 붙인다.
/// 처음 들어올 때:
///   01 검은 화면에 "이안 | 또 이 꿈인가" → 02 방이 서서히 드러남, 툴팁 "W A S D 이동"
///   → 03 툴팁 "몽결정에 가까이 가면 자동으로 수집됩니다." 얻을 때마다 "✦ 몽결정 획득 ✦"
///   → 툴팁 "보랏빛 소용돌이까지 빛의 길을 이어 주세요." → 길이 이어지면 어두워지며 전투(04)로.
/// 전투를 이기고 돌아왔을 때(문 모드): 툴팁 "몽쇄가 걸린 문으로 가 보세요." → 문 칸에 닿으면 조작을 멈추고
///   어두워지며 문·깨어남 장면(Stories/prologue_wake, 06~08) → 수선소.
/// 안내 툴팁은 다음 안내가 나올 때까지 그대로 둔다(한 번 누르거나 첫 행동만으로 숨기지 않음).
/// </summary>
public class ExploreTutorial : MonoBehaviour
{
    public ExploreUI explore;

    const float FadeSec = 1.1f;
    const float NoticeSec = 1.8f;

    readonly TutorialSteps steps = new TutorialSteps();
    float black = 1f;          // 화면을 덮는 검은색(1 = 완전히 검음)
    bool leaving;
    System.Action afterFade;
    int noticedPickups;
    float noticeAt = -10f;

    void Start()
    {
        explore.Locked = true;
        if (DreamRun.DoorMode)
        {
            steps.Guide("몽쇄가 걸린 문으로 가 보세요.", () => explore.ReachedDoor, () => explore.Locked = false);
            steps.Start();
            return;
        }
        steps
            .Say("이안", "또 이 꿈인가")
            .Guide("이동", () => explore.Opened >= 4 || explore.CrystalPickups > 0, () => explore.Locked = false, "W", "A", "S", "D")
            .Guide("몽결정에 가까이 가면 자동으로 수집됩니다.", () => explore.CrystalPickups > 0)
            .Guide("보랏빛 소용돌이까지 빛의 길을 이어 주세요.", () => explore.Connected);
        steps.Start();
    }

    void Update()
    {
        if (explore == null) return;
        steps.Update();
        float dt = Time.unscaledDeltaTime;

        // 처음 대사(검은 화면)가 끝나면 방이 서서히 드러난다. 떠날 때는 다시 어두워진다.
        bool dark = leaving || (!DreamRun.DoorMode && steps.InDialog);
        black = Mathf.MoveTowards(black, dark ? 1f : 0f, dt / FadeSec);
        if (leaving && black >= 1f && afterFade != null)
        {
            System.Action go = afterFade;
            afterFade = null;
            go();
            return;
        }

        if (explore.CrystalPickups > noticedPickups)
        {
            noticedPickups = explore.CrystalPickups;
            noticeAt = Time.unscaledTime;
        }

        if (leaving) return;
        if (DreamRun.DoorMode && explore.ReachedDoor) Leave(() => SceneFlow.FinishTutorial());
        else if (!DreamRun.DoorMode && explore.Connected && !steps.InDialog) Leave(SceneFlow.FinishExploration);
    }

    void Leave(System.Action next)
    {
        leaving = true;
        explore.Locked = true;
        afterFade = next;
    }

    void OnGUI()
    {
        if (explore == null) return;
        GUI.depth = -10; // 탐색 화면 위에
        UIKit.Begin();
        float w = UIKit.Width, h = UIKit.Height;

        float since = Time.unscaledTime - noticeAt;
        if (since < NoticeSec)
        {
            float a = Mathf.Min(1f, since / 0.2f) * Mathf.Clamp01((NoticeSec - since) / 0.5f);
            DreamUI.Notice(new Rect(0f, h * 0.18f - since * 12f, w, 50f), "✦ 몽결정 획득 ✦", a);
        }

        if (black > 0f)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, black);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = old;
        }
        if (!leaving && steps.Draw(w, h)) SceneFlow.FinishTutorial(showStory: false);
        UIKit.End();
    }
}
