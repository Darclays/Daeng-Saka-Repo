using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmWindowsBaselineBuild
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Build/Windows Baseline";
        private const BuildTarget Target = BuildTarget.StandaloneWindows64;

        [MenuItem(Menu, true)]
        private static bool CanBuild()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
                && !EditorApplication.isUpdating && !BuildPipeline.isBuildingPlayer;
        }

        [MenuItem(Menu)]
        private static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, Target))
            {
                Debug.LogError("[Daeng Saka] Unity Editor ini belum mendukung build Windows 64-bit. Cek modul Windows untuk versi Editor ini di Unity Hub.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string project = Directory.GetParent(Application.dataPath).FullName;
            try
            {
                if (!GmMainMenuSetup.HasTitleScene)
                    throw new IOException("Jalankan GamecommMentor > Daeng Saka > Set Up Main Menu sebelum build.");
                string[] scenes = GmMainMenuSetup.ScenePaths(includeTitle: true);
                foreach (string scene in scenes)
                    CheckScene(project, scene);

                string root = Path.Combine(project, "Builds", "WindowsBaseline");
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                string directory = Path.Combine(root, stamp);
                int attempt = 1;
                while (Directory.Exists(directory) || File.Exists(directory))
                {
                    attempt++;
                    directory = Path.Combine(root, stamp + "-" + attempt.ToString(CultureInfo.InvariantCulture));
                }

                Directory.CreateDirectory(directory);
                string player = Path.Combine(directory, "DaengSaka.exe");
                Debug.Log("[Daeng Saka] Memulai build Windows dari Main Menu. Tujuan: " + player);
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(new BuildPlayerOptions
                {
                    target = Target,
                    targetGroup = BuildTargetGroup.Standalone,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    scenes = scenes,
                    locationPathName = player,
                    options = BuildOptions.ShowBuiltPlayer
                });
            }
            catch (IOException exception)
            {
                Debug.LogError("[Daeng Saka] " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError("[Daeng Saka] Akses file ditolak. " + exception.Message);
            }
        }

        private static void CheckScene(string project, string scene)
        {
            string file = Path.Combine(project, scene);
            if (!File.Exists(file))
                throw new FileNotFoundException("Scene demo belum ada di proyek: " + scene);

            // Only read the signature. A valid Unity scene may be YAML or binary.
            const string lfsHeader = "version https://git-lfs.github.com/spec/v1";
            using (FileStream stream = File.OpenRead(file))
            {
                int index = 0;
                while (index < lfsHeader.Length && stream.ReadByte() == lfsHeader[index])
                    index++;
                if (index == lfsHeader.Length)
                    throw new IOException("Scene masih berupa pointer Git LFS: " + scene
                        + ". Jalankan git lfs pull di folder proyek, tunggu Unity selesai mengimpor, lalu ulangi build.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                throw new IOException("Unity belum bisa membaca scene: " + scene + ". Periksa hasil impor di Console.");
        }
    }
}
