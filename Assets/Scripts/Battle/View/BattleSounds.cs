using Akmong.Battle;

/// <summary>전투 효과음: 전투 코어 이벤트에 소리를 붙인다(타워 공격은 종류별, 적 맞음·처치, 중심 피해, 짓기·강화, 웨이브 시작, 승리·패배).</summary>
public static class BattleSounds
{
    public static void Attach(BattleSession session)
    {
        session.TowerFired += hit =>
        {
            if (hit.HitIndex == 0) SoundFx.Play(TowerSound(hit.Tower.Def.Id), 0.8f, 0.06f, 0.05f);
            SoundFx.Play(hit.Killed ? "kill" : "hit", hit.Killed ? 0.8f : 0.45f, 0.1f, hit.Killed ? 0.05f : 0.07f);
        };
        session.EnemyReachedCore += (enemy, damage) => SoundFx.Play("core_hit", 1f, 0.03f, 0.25f);
        session.TowerBuilt += tower => SoundFx.Play("build", 0.9f);
        session.TowerUpgraded += tower => SoundFx.Play("upgrade", 0.9f);
        session.WaveStarted += wave => SoundFx.Play("wave_start", 0.9f, 0f);
        session.Ended += (result, reason) =>
        {
            if (result == BattleResult.Success) SoundFx.Play("victory", 1f, 0f);
            else if (result == BattleResult.Failure) SoundFx.Play("defeat", 1f, 0f);
        };
    }

    static string TowerSound(string towerId)
    {
        switch (towerId)
        {
            case "TW_LAMP": return "tw_lamp";
            case "TW_SCISSORS": return "tw_scissors";
            case "TW_SEWING": return "tw_sewing";
            case "TW_TAPE": return "tw_tape";
            case "TW_PIN": return "tw_pin";
            case "TW_TAGBOW": return "tw_tagbow";
            default: return "tw_pin";
        }
    }
}
