using UnityEngine;

/// <summary>
/// 첫 수선 튜토리얼 · 탐색 화면 안내(기획서 5장 "탐색과 몽결정 수집").
/// 이동 → 몽결정 → 출현 지점까지 길 잇기 → 도하 경고 → 길과 방어의 관계 → 전투 준비.
/// ExploreUI가 튜토리얼 중일 때 붙인다. 화면 위 가운데에 안내 상자를 띄운다.
/// </summary>
public class ExploreTutorial : MonoBehaviour
{
    public ExploreUI explore;

    readonly TutorialSteps steps = new TutorialSteps();

    void Start()
    {
        steps
            .Guide("탐색", "WASD·방향키(또는 바로 옆 칸 클릭)로 이안을 움직여요. 걸어간 칸은 어둠이 걷히고, 이번 꿈에서는 다시 어두워지지 않아요.",
                () => explore.Opened >= 3)
            .Guide("몽결정", "보랏빛·하늘빛 결정이 보이는 칸을 밝히면 몽결정을 얻어요. 몽결정은 전투에서 수선 도구를 짓고 강화하는 재화예요.",
                () => DreamRun.Collected > 0 || explore.Opened >= 25)
            .Guide("길 잇기", "보라색 출현 지점까지 밝힌 길을 이어 주세요. 막힌 암흑(붉은 가시)은 지나갈 수 없어요.",
                () => explore.Connected)
            .Say("도하", "이안. 저쪽에서 오고 있어.")
            .Guide("길과 방어", "악몽은 밝힌 칸 중 가장 짧은 길을 따라 와요. 길을 더 넓히면 몽결정을 더 얻지만, 악몽이 오는 길도 바뀌어요. 준비되면 아래 '전투 준비'를 누르세요.");
        steps.Start();
    }

    void Update() => steps.Update();

    void OnGUI()
    {
        GUI.depth = -10; // 탐색 화면 위에
        UIKit.Begin();
        if (steps.Draw(UIKit.Width)) SceneFlow.FinishTutorial(showStory: false);
        UIKit.End();
    }
}
