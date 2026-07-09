using UnityEditor;
using UnityEngine;

public class BuildGame {
    public static void DoBuild() {
        var options = new BuildPlayerOptions {
            scenes = new[] { "Assets/_Project/Scenes/Splash.unity", "Assets/_Project/Scenes/MainMenu.unity", "Assets/_Project/Scenes/Gameplay.unity" },
            locationPathName = "Builds/Win/Game.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildPipeline.BuildPlayer(options);
        EditorApplication.Exit(0);
    }
}
