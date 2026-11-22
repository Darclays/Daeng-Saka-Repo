using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmMainMenuSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Main Menu";

        internal static bool HasTitleScene => AssetDatabase.LoadAssetAtPath<SceneAsset>(GmDemoScenes.Title) != null;

        internal static string[] ScenePaths(bool includeTitle)
        {
            string[] gameplay = { GmDemoScenes.Capital, GmDemoScenes.Village, GmDemoScenes.Cave, GmDemoScenes.Battle };
            return includeTitle ? new[] { GmDemoScenes.Title }.Concat(gameplay).ToArray() : gameplay;
        }

        [MenuItem(Menu, true)]
        private static bool CanSetUp() =>
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        [MenuItem(Menu)]
        private static void SetUp()
        {
            try
            {
                foreach (string path in ScenePaths(includeTitle: false))
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                        throw new InvalidOperationException($"Scene demo belum diimpor: {path}");
                EnsureTitleScene();
                RegisterScenes(includeTitle: true);
                Debug.Log("[Daeng Saka] MainMenu siap. Buka scene MainMenu untuk menguji layar judul.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void EnsureTitleScene()
        {
            if (HasTitleScene) return;
            if (File.Exists(GmDemoScenes.Title))
                throw new IOException("MainMenu.unity sudah ada dan belum dapat diimpor oleh Unity.");

            Scene previous = SceneManager.GetActiveScene();
            Scene created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var root = new GameObject("Daeng Saka - Layar Judul");
                SceneManager.MoveGameObjectToScene(root, created);
                root.AddComponent<GmTitleMenu>();
                if (!EditorSceneManager.SaveScene(created, GmDemoScenes.Title))
                    throw new IOException("Unity gagal menyimpan scene MainMenu.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(created, removeScene: true);
            }
        }

        internal static void RegisterScenes(bool includeTitle)
        {
            string[] required = ScenePaths(includeTitle);
            var existing = EditorBuildSettings.scenes.Where(scene => !required.Contains(scene.path)).ToArray();
            EditorBuildSettings.scenes = required.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(existing).ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
