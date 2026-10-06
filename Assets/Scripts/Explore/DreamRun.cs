using Akmong.Battle;

/// <summary>
/// 지금 진행 중인 꿈 탐색(탐색형 디펜스) 한 판의 상태. 탐색 화면(Explore)이 칸을 밝히고 결정을 모으면,
/// 전투 화면(Battle)이 여기 남은 맵으로 몬스터 경로(밝힌 칸의 최단 경로)와 시작 몽결정을 정한다.
/// 프로토타입 규칙: 탐색 제한 없음. 출현 지점까지 길이 이어지면 언제든 전투를 시작할 수 있다.
/// </summary>
public static class DreamRun
{
    /// <summary>탐색 중이거나 탐색을 마친 맵(밝힌 칸 포함). 없으면 칸 맵 판이 아님.</summary>
    public static GridMap Map { get; private set; }
    /// <summary>원본 맵(다시 도전할 때 처음 상태로 되돌리기 위해).</summary>
    public static GridMap Original { get; private set; }
    /// <summary>탐색에서 모은 몽결정.</summary>
    public static int Collected { get; set; }
    /// <summary>맵 에디터의 "테스트 플레이"로 들어왔는지(끝나면 에디터로 돌아갈 수 있게).</summary>
    public static bool TestMode { get; private set; }

    public static bool Active => Map != null;

    /// <summary>새 판 시작. original은 건드리지 않고 복사해서 쓴다.</summary>
    public static void Begin(GridMap original, bool testMode)
    {
        Original = original.Clone();
        Map = original.Clone();
        Collected = 0;
        TestMode = testMode;
    }

    /// <summary>같은 맵으로 처음부터 다시(다시 도전).</summary>
    public static void Restart()
    {
        if (Original == null) return;
        Map = Original.Clone();
        Collected = 0;
    }

    public static void Clear()
    {
        Map = null;
        Original = null;
        Collected = 0;
        TestMode = false;
    }
}
