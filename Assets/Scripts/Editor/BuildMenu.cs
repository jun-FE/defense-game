using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>메뉴: Defense > Windows 빌드. 결과물은 Builds/Windows 에 생성된다(Steam 업로드 폴더).</summary>
public static class BuildMenu
{
    const string OutputPath = "Builds/Windows/DefenseGame.exe";

    [MenuItem("Defense/Windows 빌드")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Defense] 빌드 성공: {OutputPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
            EditorUtility.RevealInFinder(OutputPath);
        }
        else
        {
            Debug.LogError($"[Defense] 빌드 실패: {report.summary.result}");
        }
    }
}
