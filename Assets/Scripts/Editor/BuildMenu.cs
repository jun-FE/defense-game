using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 메뉴: Defense > Windows 빌드 / macOS 빌드. 결과물은 Builds/ 아래에 생긴다.
/// Steam 업로드 스크립트는 Builds/Windows 를 올린다.
/// 맥에서 만든 Windows 빌드는 Mono 백엔드를 쓴다(IL2CPP Windows 빌드는 Windows PC에서만 가능).
/// </summary>
public static class BuildMenu
{
    [MenuItem("Defense/Windows 빌드")]
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/DefenseGame.exe");

    [MenuItem("Defense/macOS 빌드")]
    public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Builds/macOS/DefenseGame.app");

    static void Build(BuildTarget target, string outputPath)
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Defense] 빌드 성공: {outputPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
            EditorUtility.RevealInFinder(outputPath);
        }
        else
        {
            Debug.LogError($"[Defense] 빌드 실패: {report.summary.result} (해당 플랫폼 빌드 모듈이 설치돼 있는지 Unity Hub에서 확인하세요)");
        }
    }
}
