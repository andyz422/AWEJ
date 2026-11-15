using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds a Windows development player for the automated co-op test (NetAutoTest) into Builds/NetTest.
// Batch: -executeMethod NetTestBuild.BuildBatch
public static class NetTestBuild
{
    public const string Output = "Builds/NetTest/AWEJ.exe";

    [MenuItem("AWEJ/Build Co-op Test Player")]
    public static void Build()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            // Skip stale entries (e.g. StoreScene, which no longer exists) instead of failing the build.
            if (s.enabled && System.IO.File.Exists(s.path))
            {
                scenes.Add(s.path);
            }
            else if (s.enabled)
            {
                Debug.LogWarning("[NetTestBuild] Skipping missing scene in Build Settings: " + s.path);
            }
        }
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = Output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
        });
        Debug.Log("[NetTestBuild] " + report.summary.result + " -> " + Output + " (" + report.summary.totalErrors + " errors)");
    }

    public static void BuildBatch()
    {
        Build();
        EditorApplication.Exit(0);
    }
}
