using System.Collections.Generic;
using System.Linq;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmMainQuestSetup
    {
        private const string Menu = "Set Up Main Quest Playtest";
        private const string CaveTestId = "main-quest-cave-test";

        [MenuItem("GamecommMentor/Daeng Saka/" + Menu)]
        private static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path))
            {
                Debug.LogError("[Daeng Saka 1C.2B] Buka scene playtest 1C.1B yang sudah tersimpan.");
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            GmEncounterFlow[] flows = ActiveComponents<GmEncounterFlow>(roots).ToArray();
            GmSakaMotor[] players = ActiveComponents<GmSakaMotor>(roots).ToArray();
            GmPixelCrushersConversation[] guides = ActiveComponents<GmPixelCrushersConversation>(roots)
                .Where(npc => npc.ConversationTitle == GmDialogueExampleData.DemoOpeningConversation).ToArray();
            if (flows.Length != 1 || players.Length != 1 || guides.Length != 1 ||
                SceneUtility.GetBuildIndexByScenePath(flows[0].BattleScenePath) < 0)
            {
                Debug.LogError("[Daeng Saka 1C.2B] Gunakan scene yang lulus Set Up Demo Progression Playtest dari 1C.1B.");
                return;
            }

            GmEnemyEncounter[] encounters = ActiveComponents<GmEnemyEncounter>(roots).ToArray();
            GmEnemyEncounter sorelia = FindEncounter(encounters, "common-poppo");
            GmEnemyEncounter pakande = FindEncounter(encounters, "boss-nenek-pakande");
            GmEnemyEncounter[] caveMarkers = roots.SelectMany(root => root.GetComponentsInChildren<GmEnemyEncounter>(true))
                .Where(enemy => enemy.EncounterId == CaveTestId).ToArray();
            if (caveMarkers.Length > 1 || (caveMarkers.Length == 1 && !caveMarkers[0].isActiveAndEnabled))
            {
                Debug.LogError("[Daeng Saka 1C.2B] Sisakan satu marker Musuh Gua (Uji) yang aktif.");
                return;
            }
            GmEnemyEncounter cave = caveMarkers.FirstOrDefault() ?? FindEncounter(encounters, "enemy-test-01");
            if (sorelia == null || !sorelia.UsePoppoBattle || pakande == null || pakande.BossBattleProfile == null)
            {
                Debug.LogError("[Daeng Saka 1C.2B] Tambahkan Poppo dan Pakande lewat menu playtest masing-masing.");
                return;
            }

            DialogueSystemController[] managers = ActiveComponents<DialogueSystemController>(roots).ToArray();
            DialogueSystemController manager = managers.Length == 1 ? managers[0] : null;
            GmPixelCrushersDialogueBridge bridge = manager != null ? manager.GetComponent<GmPixelCrushersDialogueBridge>() : null;
            DialogueDatabase database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(GmDialogueExampleData.DatabasePath);
            if (bridge == null || !bridge.isActiveAndEnabled || database == null ||
                (manager.initialDatabase != database && bridge.AdditionalDatabase != database))
            {
                Debug.LogError("[Daeng Saka 1C.2B] Aktifkan Dialogue Manager dan database dialog contoh dari 1C.1B.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(Menu);
            GmEncounterFlow flow = flows[0];
            if (cave == null) cave = CreateCaveMarker(sorelia, players[0], flow);

            Undo.RecordObject(database, Menu);
            int previousCount = database.conversations.Count;
            GmPixelCrushersMainQuestDialogue.StepDialogue[] dialogues = GmMainQuestDialogueData.EnsureConversations(database);
            if (database.conversations.Count != previousCount)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }

            GmMainQuestProgress quest = flow.GetComponent<GmMainQuestProgress>();
            if (quest == null) quest = Undo.AddComponent<GmMainQuestProgress>(flow.gameObject);
            GmPixelCrushersMainQuestDialogue sequence = flow.GetComponent<GmPixelCrushersMainQuestDialogue>();
            if (sequence == null) sequence = Undo.AddComponent<GmPixelCrushersMainQuestDialogue>(flow.gameObject);
            GmPixelCrushersConversation guide = guides[0];
            Undo.RecordObjects(new Object[] { quest, sequence, guide }, Menu);
            quest.Configure(sorelia, cave, pakande);
            sequence.Configure(bridge, dialogues);
            guide.SetMainQuestDialogue(sequence);
            guide.SetPresentation("Main Quest (Uji)", "Bicara");
            PrefabUtility.RecordPrefabInstancePropertyModifications(quest);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sequence);
            PrefabUtility.RecordPrefabInstancePropertyModifications(guide);
            SetUpEncounterHint(roots, quest, players[0].GetComponent<GmControlGate>());
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = guide.gameObject;
            Debug.Log($"[Daeng Saka 1C.2B-R1] Simpan scene. Tujuan battle akan menandai musuh aktif, termasuk {cave.gameObject.name} di gua.", guide);
        }

        private static void SetUpEncounterHint(GameObject[] roots, GmMainQuestProgress quest, GmControlGate controls)
        {
            GmMainQuestEncounterHint hint = roots.SelectMany(root => root.GetComponentsInChildren<GmMainQuestEncounterHint>(true))
                .FirstOrDefault(component => component.transform.parent == null);
            if (hint == null)
            {
                var root = new GameObject("Main Quest Encounter Hint", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(root, Menu);
                hint = Undo.AddComponent<GmMainQuestEncounterHint>(root);
            }

            Undo.RecordObject(hint, Menu);
            hint.Configure(quest, controls);
            PrefabUtility.RecordPrefabInstancePropertyModifications(hint);
        }

        private static GmEnemyEncounter FindEncounter(GmEnemyEncounter[] encounters, string id)
        {
            GmEnemyEncounter[] matches = encounters.Where(enemy => enemy.EncounterId == id).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static GmEnemyEncounter CreateCaveMarker(GmEnemyEncounter sorelia, GmSakaMotor player, GmEncounterFlow flow)
        {
            Vector3 offset = Vector3.ProjectOnPlane(player.transform.position - sorelia.transform.position, Vector3.up);
            if (offset.sqrMagnitude < 0.01f) offset = Vector3.left;
            Vector3 position = player.transform.position + offset.normalized * 3.2f;
            if (Physics.Raycast(position + Vector3.up * 4f, Vector3.down, out RaycastHit ground, 8f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) position.y = ground.point.y;

            GameObject marker = Object.Instantiate(sorelia.gameObject, position, sorelia.transform.rotation);
            Undo.RegisterCreatedObjectUndo(marker, Menu);
            Undo.RecordObject(marker, Menu);
            marker.name = "Musuh Gua (Uji)";
            GmEnemyEncounter encounter = marker.GetComponent<GmEnemyEncounter>();
            Undo.RecordObject(encounter, Menu);
            encounter.Configure(flow, CaveTestId, "Musuh Gua (Uji)", poppoBattle: true);
            return encounter;
        }

        private static IEnumerable<T> ActiveComponents<T>(GameObject[] roots) where T : Behaviour =>
            roots.SelectMany(root => root.GetComponentsInChildren<T>(false)).Where(component => component.isActiveAndEnabled);
    }
}
