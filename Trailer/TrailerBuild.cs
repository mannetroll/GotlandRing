using UnityEditor;
using UnityEditor.Build.Reporting;

public static class TrailerBuild
{
    public static void Build()
    {
        EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", "ARM64");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Gotland.unity" },
            locationPathName = "Build/Trailer/GotlandRing-Trailer.app",
            target = BuildTarget.StandaloneOSX, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Trailer capture build failed: " + report.summary.result);
    }
}
