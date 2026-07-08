using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BadMovieClues.EditorTools
{
    /// <summary>
    /// Builds Addressables content before every player build, local or
    /// cloud (Unity Build Automation runs the same underlying build
    /// pipeline, so this fires automatically there too - no extra manual
    /// step needed in the Build Automation UI).
    ///
    /// Editor Play mode never needed this (it reads assets directly, no
    /// built bundles required), which is exactly why the gap went
    /// unnoticed through every prior milestone's verification: clue
    /// images loaded fine in every Editor-based smoke test, but a real
    /// device build has nothing to load from without this step -
    /// Addressables.LoadAssetAsync returns null for a key that's
    /// registered but never actually packed into a bundle. Root cause of
    /// "some levels charge coins but show no image" on-device.
    /// </summary>
    public class AddressablesBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[AddressablesBuildPreprocessor] Building Addressables content...");
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
                Debug.LogError($"[AddressablesBuildPreprocessor] Addressables content build failed: {result.Error}");
            else
                Debug.Log($"[AddressablesBuildPreprocessor] Addressables content built OK, duration={result.Duration}s.");
        }
    }
}
