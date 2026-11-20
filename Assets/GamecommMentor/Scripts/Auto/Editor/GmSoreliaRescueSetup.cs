using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmSoreliaRescueSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Sorelia Rescue";
        private const string UndoName = "Set Up Sorelia Rescue";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";
        private const string FisherArt = "Assets/Asset2D_Pixel/NPC/NPC_Nelayan/";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && SceneManager.GetActiveScene().path == Village;

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanInstall()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                GmSoreliaRescue placement = InstallInScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = placement.gameObject;
                Debug.Log("[Daeng Saka 1E.7B R1] Posisi keluarga Nelayan dan Poppo siap. Simpan scene DesaSorelia.", placement);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
            }
        }

        private static GmSoreliaRescue InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            GameObject flow = RequireRoot(scene, "Daeng Saka - Encounter Flow").gameObject;
            Require<GmEncounterFlow>(flow.transform);
            Require<GmMainQuestProgress>(flow.transform);
            var father = RequireChild(world, "Nelayan (Uji)");
            var fatherVisual = RequireChild(father, "Visual");
            var fatherRenderer = Require<SpriteRenderer>(fatherVisual);
            var conversation = Require<GmPixelCrushersConversation>(father);
            var poppo = Require<GmEnemyEncounter>(RequireChild(world, "Poppo (Uji)"));
            var camera = Require<Camera>(RequireChild(world, "Saka - Orbit Camera"));
            var player = Require<CharacterController>(RequireChild(world, "Saka"));
            var layout = new GmWorldSceneLayout(scene, "PortalPoint_StartRespawnDesaSoralia",
                "PortalPoint_GoGoaLembayung", player);
            var start = RequireChild(world, "Spawn - Start");
            var portal = RequireChild(world, "Portal");

            // The before-invasion group in the scene is named NPCPalaces_BeforeInvade.
            var before = RequireRoot(scene, "NPCPalaces_BeforeInvade", "NPCPlaces_BeforeInvade");
            var after = RequireRoot(scene, "NPCPlaces_AfterInvade");
            var parentsMarker = RequireChild(before, "NPCPlaces_Nelayan_BapakIbu");
            var childMarker = RequireChild(before, "NPCPlaces_Nelayan_AnakL");
            var poppoMarker = RequireChild(before, "NPCPlaces_EC_Poppo");
            var safeFatherMarker = RequireChild(after, "NPCPlaces_Nelayan_Bapak");
            var safeMotherMarker = RequireChild(after, "NPCPlaces_Nelayan_Ibu");
            var safeChildMarker = RequireChild(after, "NPCPlaces_Nelayan_Anak");

            var fatherArt = GmDialogueNpcSprites.Prepare(FisherArt +
                "NPC_Nelayan_Ayah/NPC_Nelayan_Ayah_Idle/NPC_Nelayan_Ayah_Idle_Front.png", "Sorelia_Nelayan_Ayah");
            var motherArt = GmDialogueNpcSprites.Prepare(FisherArt +
                "NPC_Nelayan_Ibu/NPC_Nelayan_Ibu_Idle/NPC_Nelayan_Ibu_Idle_Front.png", "Sorelia_Nelayan_Ibu");
            // The original world example already has an idle controller for the son's sprites.
            var childArt = GmDialogueNpcSprites.Prepare(GmDialogueNpcSprites.FisherSource, "Nelayan");
            Material material = fatherRenderer.sharedMaterial;
            Transform family = GetOrCreateChild(world, "Keluarga Nelayan");
            Transform mother = GetOrCreateChild(family, "Ibu Nelayan");
            Transform child = GetOrCreateChild(family, "Anak Nelayan");
            Transform points = GetOrCreateChild(family, "Posisi Keluarga");
            Transform beforePoints = GetOrCreateChild(points, "Saat Diserang");
            Transform afterPoints = GetOrCreateChild(points, "Setelah Ditolong");
            Vector3 faceEntry = -layout.IntoLevel;

            Transform PlacePoint(Transform group, string name, Vector3 position, params Vector3[] avoid)
            {
                Transform point = GetOrCreateChild(group, name);
                Undo.RecordObject(point, UndoName);
                layout.Place(point, position, faceEntry, avoid);
                return point;
            }

            // Ignore the existing NPC's body during grounding so running setup twice is stable.
            var colliders = father.GetComponentsInChildren<Collider>(true)
                .Where(collider => collider.enabled && !collider.isTrigger).ToArray();
            foreach (Collider collider in colliders) collider.enabled = false;
            GmSoreliaRescue.Resident[] residents;
            try
            {
                Undo.RecordObject(poppo.transform, UndoName);
                layout.Place(poppo.transform, poppoMarker.position, faceEntry, start.position, portal.position);
                var fatherBefore = PlacePoint(beforePoints, "Ayah", parentsMarker.position, poppo.transform.position);
                Vector3 side = Vector3.Cross(Vector3.up, layout.IntoLevel);
                if (Vector3.Dot(poppo.transform.position - fatherBefore.position, side) > 0f) side = -side;
                var motherBefore = PlacePoint(beforePoints, "Ibu", fatherBefore.position + side * 2.3f,
                    fatherBefore.position, poppo.transform.position);
                var childBefore = PlacePoint(beforePoints, "Anak", childMarker.position, start.position);
                var fatherAfter = PlacePoint(afterPoints, "Ayah", safeFatherMarker.position, portal.position);
                var motherAfter = PlacePoint(afterPoints, "Ibu", safeMotherMarker.position, fatherAfter.position);
                var childAfter = PlacePoint(afterPoints, "Anak", safeChildMarker.position,
                    fatherAfter.position, motherAfter.position);

                PlaceActor(father, fatherBefore);
                PlaceActor(mother, motherBefore);
                PlaceActor(child, childBefore);
                residents = new[]
                {
                    new GmSoreliaRescue.Resident(father, fatherBefore, fatherAfter),
                    new GmSoreliaRescue.Resident(mother, motherBefore, motherAfter),
                    new GmSoreliaRescue.Resident(child, childBefore, childAfter)
                };
            }
            finally
            {
                foreach (Collider collider in colliders) collider.enabled = true;
                Physics.SyncTransforms();
            }

            SetVisual(fatherVisual, fatherArt, material, camera);
            SetVisual(GetOrCreateChild(mother, "Visual"), motherArt, material, camera);
            SetVisual(GetOrCreateChild(child, "Visual"), childArt, material, camera);
            Undo.RecordObject(conversation, UndoName);
            conversation.SetPresentation("Nelayan", "Bicara");
            PrefabUtility.RecordPrefabInstancePropertyModifications(conversation);

            var rescue = GetOrAdd<GmSoreliaRescue>(flow.transform);
            Undo.RecordObject(rescue, UndoName);
            rescue.Configure(poppo, residents);
            return rescue;
        }

        private static void PlaceActor(Transform actor, Transform point)
        {
            Undo.RecordObject(actor, UndoName);
            actor.SetPositionAndRotation(point.position, point.rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
        }

        private static void SetVisual(Transform visual, GmDialogueNpcSprites.Prepared art, Material material, Camera camera)
        {
            var renderer = GetOrAdd<SpriteRenderer>(visual);
            var animator = GetOrAdd<Animator>(visual);
            var billboard = GetOrAdd<GmSpriteBillboard>(visual);
            Undo.RecordObject(renderer, UndoName);
            Undo.RecordObject(animator, UndoName);
            Undo.RecordObject(billboard, UndoName);
            renderer.sprite = art.First;
            if (material != null) renderer.sharedMaterial = material;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            animator.runtimeAnimatorController = art.Controller;
            animator.applyRootMotion = false;
            billboard.SetCamera(camera);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(billboard);
        }

        private static Transform RequireRoot(Scene scene, params string[] names)
        {
            var root = scene.GetRootGameObjects().SingleOrDefault(candidate => names.Contains(candidate.name));
            if (root == null) throw new InvalidOperationException($"Objek '{names[0]}' belum ada di {scene.name}.");
            return root.transform;
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) throw new InvalidOperationException($"Titik '{name}' belum ada di '{parent.name}'.");
            return child;
        }

        private static T Require<T>(Transform target) where T : Component
        {
            if (target.TryGetComponent(out T component)) return component;
            throw new InvalidOperationException($"'{target.name}' harus punya {typeof(T).Name}.");
        }

        private static T GetOrAdd<T>(Transform target) where T : Component
        {
            if (target.TryGetComponent(out T component)) return component;

            component = Undo.AddComponent<T>(target.gameObject);
            if (component == null)
                throw new InvalidOperationException($"Gagal menambahkan {typeof(T).Name} ke '{target.name}'.");
            return component;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            var child = new GameObject(name);
            SceneManager.MoveGameObjectToScene(child, parent.gameObject.scene);
            child.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child, UndoName);
            return child.transform;
        }
    }
}
