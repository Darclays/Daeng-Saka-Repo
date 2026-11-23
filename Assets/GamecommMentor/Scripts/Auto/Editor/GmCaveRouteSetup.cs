using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmCaveRouteSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Goa Lembayung Route";
        private const string UndoName = "Set Up Goa Lembayung Route";
        private const string Cave = "Assets/GamecommMentor/Scenes_GamecommMentor/GoaLembanyung.unity";
        private const string EntryMarker = "PortalPoint_StartRespawnGoaLembayung";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && SceneManager.GetActiveScene().path == Cave;

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanInstall()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                Transform boss = InstallInScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = boss.gameObject;
                Debug.Log("[Daeng Saka 1E.16B R1] Posisi Poppo dan Nenek Pakande mengikuti NPCPlaces. Simpan scene GoaLembanyung.", boss);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        internal static Transform InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform flow = RequireRoot(scene, "Daeng Saka - Encounter Flow");
            var progress = Require<GmMainQuestProgress>(flow);
            var dialogue = Require<GmPixelCrushersMainQuestDialogue>(flow);
            Transform guide = world.Find(GmVisualPolishSetup.CaveMeetingPoint) ??
                RequireChild(world, GmVisualPolishSetup.CaveTestGuide);
            Transform guard = RequireChild(world, "Musuh Goa");
            Transform boss = RequireChild(world, "Nenek Pakande");
            var encounter = Require<GmEnemyEncounter>(boss);
            if (progress.PakandeEncounter != encounter || encounter.BossBattleProfile == null)
                throw new InvalidOperationException("Hubungkan Nenek Pakande ke quest dan profil battle sebelum memasang rute.");
            if (!Require<SphereCollider>(boss).isTrigger)
                throw new InvalidOperationException("Sphere Collider Nenek Pakande harus memakai Is Trigger.");

            Transform places = RequireRoot(scene, "NPCPlaces");
            Transform poppoMarker = RequireChild(places, "EC_Poppo");
            Transform marker = RequireChild(places, "EB_NenekPkdn");
            Transform spawn = RequireChild(world, "Spawn - Start");
            Transform portal = RequireChild(world, "Portal");
            var body = Require<CharacterController>(RequireChild(world, "Saka"));
            var layout = new GmWorldSceneLayout(scene, EntryMarker, EntryMarker, body);
            Vector3 towardEntry = Vector3.ProjectOnPlane(spawn.position - poppoMarker.position, Vector3.up).normalized;
            if (towardEntry.sqrMagnitude < .01f) towardEntry = -layout.IntoLevel;
            Undo.RecordObject(guard, UndoName);
            layout.Place(guard, poppoMarker.position, towardEntry, spawn.position, portal.position, guide.position);

            Vector3 facing = Vector3.ProjectOnPlane(guard.position - marker.position, Vector3.up).normalized;
            if (facing.sqrMagnitude < 0.01f) facing = -layout.IntoLevel;

            Undo.RecordObject(boss, UndoName);
            layout.Place(boss, marker.position, facing, spawn.position, portal.position, guard.position);
            if (!boss.TryGetComponent(out GmPixelCrushersConversation conversation))
                conversation = Undo.AddComponent<GmPixelCrushersConversation>(boss.gameObject);
            if (conversation == null)
                throw new InvalidOperationException("Gagal memasang interaksi dialog Nenek Pakande.");

            Undo.RecordObject(conversation, UndoName);
            conversation.SetPresentation("Nenek Pakande", "Bicara");
            conversation.SetMainQuestDialogue(dialogue);
            PrefabUtility.RecordPrefabInstancePropertyModifications(conversation);

            Undo.RecordObject(encounter, UndoName);
            encounter.SetStartWhenReadyWhileTouching(true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(encounter);

            Undo.RecordObject(dialogue, UndoName);
            dialogue.SetSpeakers(
                new GmPixelCrushersMainQuestDialogue.StepSpeaker(GmMainQuestStep.MasukGuaLembayung, guide),
                new GmPixelCrushersMainQuestDialogue.StepSpeaker(GmMainQuestStep.SelidikiGua, guide),
                new GmPixelCrushersMainQuestDialogue.StepSpeaker(GmMainQuestStep.TemuiPakande, boss),
                new GmPixelCrushersMainQuestDialogue.StepSpeaker(GmMainQuestStep.LawanPakande, guide));
            return boss;
        }

        private static Transform RequireRoot(Scene scene, string name)
        {
            var root = scene.GetRootGameObjects().SingleOrDefault(candidate => candidate.name == name);
            if (root == null) throw new InvalidOperationException($"'{name}' belum ada di {scene.name}.");
            return root.transform;
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) throw new InvalidOperationException($"'{name}' belum ada di bawah '{parent.name}'.");
            return child;
        }

        private static T Require<T>(Transform owner) where T : Component =>
            owner.TryGetComponent(out T component) ? component :
                throw new InvalidOperationException($"'{owner.name}' memerlukan {typeof(T).Name}.");
    }
}
