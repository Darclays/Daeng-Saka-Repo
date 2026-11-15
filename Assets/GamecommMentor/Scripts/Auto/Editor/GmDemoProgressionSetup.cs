using System.Collections.Generic;
using System.Linq;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmDemoProgressionSetup
    {
        private const string SetupName = "Set Up Demo Progression Playtest";

        [MenuItem("GamecommMentor/Daeng Saka/" + SetupName)]
        private static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("[Daeng Saka] Buka scene eksplorasi playtest yang sudah tersimpan.");
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            GmEncounterFlow[] flows = ActiveComponents<GmEncounterFlow>(roots).ToArray();
            if (flows.Length != 1 || SceneUtility.GetBuildIndexByScenePath(flows[0].BattleScenePath) < 0)
            {
                Debug.LogError("[Daeng Saka] Buka scene eksplorasi yang dipakai untuk tes battle 1B.5B.");
                return;
            }

            GmEnemyEncounter[] encounters = ActiveComponents<GmEnemyEncounter>(roots).ToArray();
            GmEnemyEncounter[] poppo = encounters.Where(enemy => enemy.EncounterId == "common-poppo" &&
                enemy.UsePoppoBattle && enemy.BossBattleProfile == null).ToArray();
            GmEnemyEncounter[] pakande = encounters.Where(enemy => enemy.EncounterId == "boss-nenek-pakande" &&
                enemy.BossBattleProfile != null).ToArray();
            if (poppo.Length != 1 || pakande.Length != 1)
            {
                Debug.LogError("[Daeng Saka] Scene harus berisi satu Poppo dan satu Pakande dari menu Add Poppo Playtest dan Add Nenek Pakande Playtest.");
                return;
            }

            GmPixelCrushersConversation[] guides = ActiveComponents<GmPixelCrushersConversation>(roots)
                .Where(npc => npc.ConversationTitle == GmDialogueExampleData.NelayanConversation ||
                    npc.ConversationTitle == GmDialogueExampleData.DemoOpeningConversation).ToArray();
            DialogueSystemController[] managers = ActiveComponents<DialogueSystemController>(roots).ToArray();
            if (guides.Length != 1 || managers.Length != 1 || ActiveComponents<GmInteractor>(roots).Count() != 1)
            {
                Debug.LogError("[Daeng Saka] Siapkan satu Nelayan contoh, satu Dialogue Manager, dan interaksi Saka. Gunakan Create NPC Dialogue Test Scene jika setup 1A.2B belum ada di scene ini.");
                return;
            }

            DialogueDatabase database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(GmDialogueExampleData.DatabasePath);
            DialogueSystemController manager = managers[0];
            GmPixelCrushersDialogueBridge bridge = manager.GetComponent<GmPixelCrushersDialogueBridge>();
            Conversation original = database != null ? database.GetConversation(GmDialogueExampleData.NelayanConversation) : null;
            if (original == null || bridge == null || !bridge.isActiveAndEnabled ||
                (manager.initialDatabase != database && bridge.AdditionalDatabase != database))
            {
                Debug.LogError("[Daeng Saka] Pasang database dialog contoh 1A.2B pada Dialogue Manager sebelum menyiapkan playtest ini.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(SetupName);
            if (database.GetConversation(GmDialogueExampleData.DemoOpeningConversation) == null ||
                database.GetConversation(GmDialogueExampleData.DemoAfterPoppoConversation) == null ||
                database.GetConversation(GmDialogueExampleData.DemoAfterPakandeConversation) == null)
            {
                Undo.RecordObject(database, SetupName);
                GmDialogueExampleData.AddDemoConversations(database, original);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }

            GmPixelCrushersConversation guide = guides[0];
            GmDemoProgress progress = guide.GetComponent<GmDemoProgress>();
            if (progress == null) progress = Undo.AddComponent<GmDemoProgress>(guide.gameObject);
            Undo.RecordObjects(new Object[] { guide, progress }, SetupName);
            progress.Configure(poppo[0], pakande[0]);
            guide.SetConversation(GmDialogueExampleData.DemoOpeningConversation);
            guide.SetDemoConversations(progress, GmDialogueExampleData.DemoAfterPoppoConversation,
                GmDialogueExampleData.DemoAfterPakandeConversation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(guide);
            PrefabUtility.RecordPrefabInstancePropertyModifications(progress);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = guide.gameObject;
            Debug.Log("[Daeng Saka 1C.1B] Simpan scene, lalu bicara dengan Nelayan sebelum dan sesudah setiap battle.", guide);
        }

        private static IEnumerable<T> ActiveComponents<T>(GameObject[] roots) where T : Behaviour =>
            roots.SelectMany(root => root.GetComponentsInChildren<T>(false)).Where(component => component.isActiveAndEnabled);
    }
}
