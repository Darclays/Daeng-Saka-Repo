using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmCapitalRouteSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Ibu Kota Entry and Portal";
        private const string UndoName = "Set Up Ibu Kota Entry and Portal";
        private const string Capital = "Assets/GamecommMentor/Scenes_GamecommMentor/IbuKota.unity";
        private const string StartMarker = "PortalPoint_GoDesaSoralia";
        private const string ExitMarker = "PortalPoint_StartRespawnPointIbuKota";

        // Use the same safe footprints the old portal and spawn used on these approaches.
        internal const float StartInset = .9f;
        internal const float ExitInset = 1.8f;

        internal static GmWorldSceneLayout CreateLayout(Scene scene, CharacterController body) =>
            new GmWorldSceneLayout(scene, StartMarker, ExitMarker, body, "GroundKota", "Ground Kerajaan");

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => CanInstall(Capital);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanSetUp()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                Transform start = InstallInScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = start.gameObject;
                Debug.Log("[Daeng Saka 1E.16B R2] Titik mulai Ibu Kota dan portal Sorelia siap. Simpan IbuKota.", start);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        private static Transform InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Require<GmWorldScene>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            Transform player = RequireChild(world, "Saka");
            Transform start = RequireChild(world, "Spawn - Start");
            Transform portal = RequireChild(world, "Portal");
            Require<GmWorldPortal>(portal);
            var orbit = Require<GmOrbitFollowCamera>(RequireChild(world, "Saka - Orbit Camera"));
            var body = Require<CharacterController>(player);
            var layout = CreateLayout(scene, body);
            layout.HidePlaceholders();

            bool wasEnabled = body.enabled;
            body.enabled = false;
            try
            {
                Vector3 inward = layout.IntoLevel;
                Undo.RecordObject(start, UndoName);
                layout.Place(start, layout.StartGuide.position + inward * StartInset, inward);
                Undo.RecordObject(portal, UndoName);
                layout.Place(portal, layout.PortalGuide.position - inward * ExitInset, -inward, start.position);
                Undo.RecordObject(player, UndoName);
                player.SetPositionAndRotation(start.position, start.rotation * Quaternion.Euler(0f, 180f, 0f));
                PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            }
            finally
            {
                body.enabled = wasEnabled;
                Physics.SyncTransforms();
            }

            Undo.RecordObject(orbit, UndoName);
            Undo.RecordObject(orbit.transform, UndoName);
            orbit.SetStartingHeading(start.eulerAngles.y);
            orbit.SnapToTarget();
            PrefabUtility.RecordPrefabInstancePropertyModifications(orbit);
            PrefabUtility.RecordPrefabInstancePropertyModifications(orbit.transform);
            return start;
        }
    }
}
