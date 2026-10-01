using Akmong.Battle;

/// <summary>전투 화면에 보여줄 한국어 문구. 거부 사유는 즉시 안내한다(시스템 기획서 28번).</summary>
public static class BattleText
{
    public static string Error(CommandError error, int cost = 0)
    {
        switch (error)
        {
            case CommandError.BattleOver: return "전투가 끝났어요";
            case CommandError.UnknownTower: return "이 스테이지에서는 지을 수 없는 타워예요";
            case CommandError.OutsideBuildZone: return "여기에는 지을 수 없어요 (밝은 칸에만 건설 가능)";
            case CommandError.Occupied: return "이미 타워가 있어요";
            case CommandError.NotEnoughCoin: return $"재화가 부족해요 (필요 {cost})";
            case CommandError.MaxLevel: return "최대 단계예요";
            default: return null;
        }
    }

    public static string Result(BattleResult result)
    {
        switch (result)
        {
            case BattleResult.Success: return "꿈을 지켜냈다";
            case BattleResult.Failure: return "꿈이 무너졌다";
            case BattleResult.Retreat: return "수선소로 돌아왔다";
            default: return "";
        }
    }

    public static string Reason(EndReason reason)
    {
        switch (reason)
        {
            case EndReason.AllWavesCleared: return "모든 웨이브를 막아냈어요.";
            case EndReason.CoreDestroyed: return "꿈의 중심이 무너졌어요. 어느 방향에서 새어 들어왔는지 떠올려 보세요.";
            case EndReason.Retreat: return "의뢰는 진행 중으로 남아 있어요. 준비를 바꿔 다시 들어갈 수 있어요.";
            default: return "";
        }
    }
}
