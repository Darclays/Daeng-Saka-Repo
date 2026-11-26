using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Launching
{
    internal static class LaunchingSceneSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Launching/Create or Update Launching Scene";
        private const string ScenePath = "Assets/GamecommMentor/Launching/DaengSakaLaunching.unity";
        private const string MusicPath = "Assets/GamecommMentor/Audio/BGM/BGM_MainMenuKerajaan.mp3";
        private const string PixelMaterialPath = "Assets/GamecommMentor/Launching/Rendering/PixelAtlas.mat";

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanSetUp()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || SceneManager.sceneCount != 1)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            var ceremony = scene.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<LaunchingCeremony>(true)).FirstOrDefault(found => found != null);
            if (ceremony == null)
                ceremony = new GameObject("Daeng Saka Launching").AddComponent<LaunchingCeremony>();
            ceremony.UseDefaults(AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath),
                AssetDatabase.LoadAssetAtPath<Material>(PixelMaterialPath));
            ceremony.EnsureSetup();
            EditorUtility.SetDirty(ceremony);
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath))
                Selection.activeGameObject = ceremony.gameObject;
        }
    }
}
