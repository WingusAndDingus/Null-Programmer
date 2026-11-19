using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NullProgrammer.Showcase.Editor
{
    internal static class RemoveLegacyDebugApp
    {
        private const string Menu = "Tools/Null Programmer/Remove Old Debug App";
        private const string OldScript = "Assets/Scripts/Debug/LiveStatsDebugPanel.cs";
        private static readonly HashSet<string> OldPrefabs = new HashSet<string>
        {
            "Assets/Prefabs/DebugPanel Icon.prefab",
            "Assets/Prefabs/DebugPanel.prefab"
        };

        [MenuItem(Menu, true)]
        private static bool CanRemove() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(Menu)]
        private static void Remove()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string[] scenes = FindAssets("t:Scene")
                .Where(path => AssetDatabase.GetDependencies(path, false).Any(OldPrefabs.Contains))
                .ToArray();
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            int removed = 0;
            try
            {
                foreach (string path in scenes)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    GameObject[] instances = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                        .Select(child => child.gameObject)
                        .Where(obj => PrefabUtility.GetNearestPrefabInstanceRoot(obj) == obj &&
                            OldPrefabs.Contains(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj)))
                        .ToArray();

                    foreach (GameObject instance in instances)
                    {
                        if (instance == null) continue;
                        Object.DestroyImmediate(instance);
                        removed++;
                    }

                    if (instances.Length > 0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        if (!EditorSceneManager.SaveScene(scene))
                        {
                            Debug.LogError("[Showcase] Could not save " + path + ". The old prefab assets were kept.");
                            return;
                        }
                    }
                }
            }
            finally
            {
                if (scenes.Length > 0 && previousSetup.Any(item => item.isLoaded && !string.IsNullOrEmpty(item.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            foreach (string asset in scenes.Concat(FindAssets("t:Prefab")).Where(path => !OldPrefabs.Contains(path)))
            {
                if (!AssetDatabase.GetDependencies(asset, false).Any(path => path == OldScript || OldPrefabs.Contains(path)))
                    continue;

                Debug.LogWarning("[Showcase] Old debug assets still referenced by " + asset + ". Remove that reference and run the cleanup again.");
                return;
            }

            foreach (string path in OldPrefabs)
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
            if (AssetDatabase.LoadMainAssetAtPath(OldScript) != null) AssetDatabase.DeleteAsset(OldScript);
            Debug.Log($"[Showcase] Removed the old debug app and {removed} scene instance(s). F12 opens Showcase Controls.");
        }

        private static IEnumerable<string> FindAssets(string filter)
        {
            return AssetDatabase.FindAssets(filter, new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath);
        }
    }
}
