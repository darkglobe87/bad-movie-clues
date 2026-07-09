using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BadMovieClues.Editor
{
    [InitializeOnLoad]
    public static class SceneDiagnostic
    {
        static SceneDiagnostic()
        {
            EditorApplication.delayCall += RunDiagnostic;
        }

        [MenuItem("Bad Movie Clues/Run Scene Diagnostic")]
        public static void RunDiagnostic()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[SceneDiagnostic] Active Scene Name: '{activeScene.name}' | Path: '{activeScene.path}'");
            
            var roots = activeScene.GetRootGameObjects();
            Debug.Log($"[SceneDiagnostic] Root GameObjects Count: {roots.Length}");
            foreach (var root in roots)
            {
                Debug.Log($"[SceneDiagnostic] - Root: '{root.name}' | ActiveSelf: {root.activeSelf}");
                PrintChildren(root.transform, "  ");
            }
        }

        private static void PrintChildren(Transform trans, string indent)
        {
            for (int i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                Debug.Log($"[SceneDiagnostic] {indent}- Child: '{child.name}' | ActiveSelf: {child.gameObject.activeSelf}");
                PrintChildren(child, indent + "  ");
            }
        }
    }
}
