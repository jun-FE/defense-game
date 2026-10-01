// Unity 없이 전투 코어 검산 테스트를 실행한다(CI·서버용).
// 실행: bash Tools/CoreTests/run.sh   (Mono 또는 .NET SDK 필요)
using System;
using Akmong.Battle;

static class RunCoreTests
{
    static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        int failed = 0;
        foreach (BattleSelfTest.Case c in BattleSelfTest.RunAll())
        {
            Console.WriteLine($"{(c.Passed ? "PASS" : "FAIL")}  {c.Name}\n      {c.Detail}");
            if (!c.Passed) failed++;
        }
        Console.WriteLine(failed == 0 ? "\n모든 검사 통과" : $"\n실패 {failed}건");
        return failed == 0 ? 0 : 1;
    }
}
