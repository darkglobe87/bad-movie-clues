using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Produces a sideload-only test APK. IL2CPP + ARM64 - required for modern
/// phones (recent Snapdragon chips dropped 32-bit/ARMv7 support entirely,
/// so Mono's ARMv7-only output won't even install). A placeholder package
/// identifier is used here; a real one + a Release build will be needed
/// before any Play Store submission (that's M7).
/// Run via: Unity.exe -batchmode -executeMethod BuildAndroidTest.Run
/// </summary>
public static class BuildAndroidTest
{
    private const string OutputPath = "Builds/Android/BadMovieClues-test.apk";

    [MenuItem("Bad Movie Clues/Build Android Test APK")]
    public static void Run()
    {
        PlayerSettings.productName = "Bad Movie Clues";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.badmovieclues.game");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

        var buildDir = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        // Build every scene registered in Build Settings (Splash -> MainMenu
        // -> Gameplay as of M10), not a single hardcoded scene - this used
        // to point at "Bootstrap.unity", which was renamed to Gameplay.unity
        // back in M8 and hadn't existed for several milestones.
        var scenePaths = new string[EditorBuildSettings.scenes.Length];
        for (var i = 0; i < scenePaths.Length; i++)
            scenePaths[i] = EditorBuildSettings.scenes[i].path;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });

        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildAndroidTest] PASS: build succeeded, size={summary.totalSize} bytes, output={OutputPath}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[BuildAndroidTest] FAIL: result={summary.result}, totalErrors={summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }
}
