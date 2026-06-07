using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build script referenced by CI pipeline via -executeMethod.
/// Builds Windows 64-bit standalone.
/// V0-EXCEPTION: will be extended for Android (V2+) and other platforms.
/// </summary>
public static class BuildScript
{
    public static void PerformWin64Build()
    {
        string[] scenes = {
            "Assets/Scenes/TestGrid.unity"
        };

        string buildPath = "Build/Win64/DungeonVR.exe";

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            targetGroup = BuildTargetGroup.Standalone,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build succeeded: {summary.totalSize} bytes at {buildPath}");
        }
        else
        {
            Debug.LogError($"[BuildScript] Build failed: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
