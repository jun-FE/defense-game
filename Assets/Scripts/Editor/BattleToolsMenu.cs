using System.Collections.Generic;
using Akmong.Battle;
using UnityEditor;
using UnityEngine;

/// <summary>전투 개발 도구 메뉴.</summary>
public static class BattleToolsMenu
{
    [MenuItem("Defense/전투 검산 테스트 실행")]
    static void RunSelfTest()
    {
        int failed = 0;
        foreach (BattleSelfTest.Case c in BattleSelfTest.RunAll())
        {
            string line = $"{(c.Passed ? "통과" : "실패")}  {c.Name}\n{c.Detail}";
            if (c.Passed) Debug.Log(line);
            else { Debug.LogError(line); failed++; }
        }
        if (failed == 0) Debug.Log("[전투 검산] 모든 검사 통과");
        else Debug.LogError($"[전투 검산] 실패 {failed}건");
    }

    [MenuItem("Defense/전투 데이터 검사")]
    static void ValidateAllStages()
    {
        string[] guids = AssetDatabase.FindAssets("t:StageAsset");
        GameRulesAsset rules = null;
        string[] ruleGuids = AssetDatabase.FindAssets("t:GameRulesAsset");
        if (ruleGuids.Length > 0) rules = AssetDatabase.LoadAssetAtPath<GameRulesAsset>(AssetDatabase.GUIDToAssetPath(ruleGuids[0]));

        // 테이블별 ID 중복(서로 다른 에셋이 같은 ID를 쓰는 경우)
        var seen = new Dictionary<string, string>();
        int problems = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:DefinitionAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<DefinitionAsset>(path);
            if (asset == null) continue;
            string key = asset.GetType().Name + ":" + asset.id;
            string other;
            if (seen.TryGetValue(key, out other))
            {
                Debug.LogError($"[전투 데이터] {asset.GetType().Name} ID '{asset.id}' 중복: {other}, {path}", asset);
                problems++;
            }
            else seen[key] = path;
        }

        foreach (string guid in guids)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageAsset>(AssetDatabase.GUIDToAssetPath(guid));
            BattleContent content = BattleContentBuilder.Build(stage, rules);
            foreach (string error in content.Errors)
            {
                Debug.LogError($"[전투 데이터] {stage.name}: {error}", stage);
                problems++;
            }
        }
        if (problems == 0) Debug.Log($"[전투 데이터] Stage {guids.Length}개 검사 통과");
    }
}
