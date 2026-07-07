using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-time helper: produces a sideload-only test APK. Mono scripting
/// backend (much faster to build than IL2CPP) and a placeholder package
/// identifier - fine for testing on your own device, but IL2CPP + a real
/// identifier will be needed before any Play Store submission (that's M7).
/// Safe to delete after use. Run via: Unity.exe -batchmode -executeMethod BuildAndroidTest.Run
/// </summary>
public static class BuildAndroidTest
{
    private const string OutputPath = "Builds/Android/BadMovieClues-test.apk";

    [MenuItem("Bad Movie Clues/Build Android Test APK")]
    public static void Run()
    {
        PlayerSettings.productName = "Bad Movie Clues";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.badmovieclues.game");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
        // Mono on Android only supports the 32-bit ARMv7 architecture (ARM64
        // requires IL2CPP) - every real phone still runs ARMv7 binaries fine,
        // this just wouldn't pass Play Store's 64-bit requirement, which
        // doesn't matter for a sideloaded test build.
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

        var buildDir = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/_Project/Scenes/Bootstrap.unity" },
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
