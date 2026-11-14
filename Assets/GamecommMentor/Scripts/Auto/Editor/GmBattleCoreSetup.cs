using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmBattleCoreSetup
    {
        [MenuItem("GamecommMentor/Daeng Saka/Install Battle Core")]
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            try
            {
                Scene exploration = SceneManager.GetActiveScene();
                GmEncounterFlow[] flows = exploration.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GmEncounterFlow>(true)).ToArray();
                if (flows.Length != 1)
                    throw new InvalidOperationException("Buka scene eksplorasi yang lulus tes encounter 1A.3B.");

                GmEncounterFlow flow = flows[0];
                string path = flow.BattleScenePath;
                if (string.IsNullOrWhiteSpace(path) || path == exploration.path || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new InvalidOperationException("Encounter Flow perlu menunjuk ke scene battle yang sudah tersimpan.");

                Scene battle = SceneManager.GetSceneByPath(path);
                bool loadedBefore = battle.IsValid() && battle.isLoaded;
                if (loadedBefore && battle.isDirty &&
                    (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { battle }) || battle.isDirty)) return;

                bool installed = false;
                bool closed = false;
                try
                {
                    if (!loadedBefore) battle = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    if (!battle.IsValid() || !battle.isLoaded)
                        throw new InvalidOperationException("Scene battle tidak berhasil dibuka untuk setup.");
                    InstallInBattleScene(battle);
                    installed = true;
                }
                finally
                {
                    if (battle.IsValid() && battle.isLoaded && (!loadedBefore || installed))
                        closed = EditorSceneManager.CloseScene(battle, true);
                }

                if (!closed)
                    throw new InvalidOperationException("Battle Core tersimpan. Tutup scene battle sebelum Play dari eksplorasi.");
                Debug.Log($"[Daeng Saka 1B.1A] Battle Core siap. Play dari {exploration.name} dan sentuh musuh.", flow);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Daeng Saka Battle Core", "Setup berhenti. Periksa pesan pertama di Console.", "OK");
            }
        }

        private static void InstallInBattleScene(Scene battle)
        {
            GameObject[] roots = battle.GetRootGameObjects();
            GmEncounterBattleRoot root = roots.Length == 1 && !roots[0].activeSelf
                ? roots[0].GetComponent<GmEncounterBattleRoot>() : null;
            if (root == null)
                throw new InvalidOperationException("Scene battle perlu satu GmEncounterBattleRoot yang awalnya nonaktif.");
            if (root.GetComponent<GmBattleController>() != null) return;

            GmEncounterTestPanel prototype = root.GetComponent<GmEncounterTestPanel>();
            if (prototype == null)
                throw new InvalidOperationException("Scene battle perlu panel hasil dari setup encounter 1A.3B.");

            using var panelData = new SerializedObject(prototype);
            Button retreat = panelData.FindProperty("retreatButton")?.objectReferenceValue as Button;
            Button victory = panelData.FindProperty("victoryButton")?.objectReferenceValue as Button;
            Transform card = retreat != null ? retreat.transform.parent : null;
            Canvas canvas = retreat != null ? retreat.GetComponentInParent<Canvas>(true) : null;
            if (card == null || canvas == null || card == canvas.transform || !card.IsChildOf(canvas.transform) ||
                !canvas.transform.IsChildOf(root.transform) || victory == null || !victory.transform.IsChildOf(card))
                throw new InvalidOperationException("Panel encounter 1A.3B perlu tombol Mundur dan Uji Menang di dalam kartu yang sama.");

            bool prototypeEnabled = prototype.enabled;
            bool cardActive = card.gameObject.activeSelf;
            GmBattleController controller = null;
            try
            {
                controller = root.gameObject.AddComponent<GmBattleController>();
                controller.SetCanvas(canvas);
                prototype.enabled = false;
                card.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(battle);
                if (!EditorSceneManager.SaveScene(battle))
                    throw new InvalidOperationException("Scene battle gagal disimpan.");
            }
            catch
            {
                if (controller != null) UnityEngine.Object.DestroyImmediate(controller);
                prototype.enabled = prototypeEnabled;
                card.gameObject.SetActive(cardActive);
                throw;
            }
        }
    }
}
