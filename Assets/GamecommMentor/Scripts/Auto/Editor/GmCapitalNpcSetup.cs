using System;
using System.Linq;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmCapitalNpcSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Ibu Kota NPCs";
        private const string UndoName = "Set Up Ibu Kota NPCs";
        private const string PuaMenu = "GamecommMentor/Daeng Saka/Set Up Pua Near Arrival";
        private const string PuaUndo = "Set Up Pua Near Arrival";
        private const string Capital = "Assets/GamecommMentor/Scenes_GamecommMentor/IbuKota.unity";
        private const string PuaObject = "Pemandu Kerajaan (Uji)";
        private const string PuaArrivalAim = "Arah Kamera - Kedatangan Pua";
        private const float PuaForward = 3f;
        private const float PuaRight = 1.15f;
        private const float PuaArrivalYaw = 10f;
        private const string NpcPrefab = "Assets/GamecommMentor/Prefabs/Core/Dialogue/NPC_Pedagang.prefab";
        private const string NpcArt = "Assets/Asset2D_Pixel/NPC/";
        private const string PuaSource = NpcArt + "NPC_Commoner/NPC_Commoner_Ayah/NPC_Commoner_Ayah_Norm/" +
            "NPC_Commoner_Ayah_Norm_Idle/NPC_Commoner_Ayah_Norm_Idle_Front.png";
        private const string SmithSource = NpcArt + "NPC_Blacksmith/NPC_Blacksmith_Idle/NPC_Blacksmith_Idle_Front.png";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && SceneManager.GetActiveScene().path == Capital;

        [MenuItem(PuaMenu, true)]
        private static bool CanSetUpPuaNearArrival() => CanInstall();

        [MenuItem(Menu)]
        private static void SetUp() => RunSetup(UndoName, InstallInScene,
            "[Daeng Saka 1E.21B] Pua’, Merchant, dan Penempa Besi siap. Simpan scene IbuKota.");

        [MenuItem(PuaMenu)]
        private static void SetUpPuaNearArrival() => RunSetup(PuaUndo, PlaceExistingPua,
            "[Daeng Saka 1E.21B] Pua’ siap menyambut Saka di dekat spawn. Simpan scene IbuKota.");

        private static void RunSetup(string undoName, Func<Scene, Transform> apply, string message)
        {
            if (!CanInstall()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            try
            {
                Transform pua = apply(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = pua.gameObject;
                Debug.Log(message, pua);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        private static Transform PlaceExistingPua(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform pua = RequireChild(world, PuaObject);
            Transform spawn = RequireChild(world, "Spawn - Start");
            var player = Require<CharacterController>(RequireChild(world, "Saka"));
            PlacePua(GmCapitalRouteSetup.CreateLayout(scene, player), world, pua, spawn, PuaUndo);
            return pua;
        }

        private static Transform InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform flow = RequireRoot(scene, "Daeng Saka - Encounter Flow");
            Transform places = RequireRoot(scene, "NPC_Places");
            Transform merchantPoint = RequireChild(places, "NPCPlace_Merchant");
            Transform smithPoint = RequireChild(places, "NPCPlace_PenempaBesi");
            Transform pua = RequireChild(world, PuaObject);
            Transform spawn = RequireChild(world, "Spawn - Start");
            Transform portal = RequireChild(world, "Portal");
            var camera = Require<Camera>(RequireChild(world, "Saka - Orbit Camera"));
            var player = Require<CharacterController>(RequireChild(world, "Saka"));
            var journal = Require<GmQuestJournal>(RequireChild(world, "Quest Encounter Hint"));
            var sequence = Require<GmPixelCrushersMainQuestDialogue>(flow);
            Require<GmMainQuestProgress>(flow);
            Require<GmEncounterFlow>(flow);
            var layout = GmCapitalRouteSetup.CreateLayout(scene, player);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefab);
            if (prefab == null) throw new InvalidOperationException("Prefab NPC belum ada: " + NpcPrefab);
            var puaArt = GmDialogueNpcSprites.Prepare(PuaSource, "IbuKota_Pua");
            var merchantArt = GmDialogueNpcSprites.Prepare(GmDialogueNpcSprites.MerchantSource, "Pedagang");
            var smithArt = GmDialogueNpcSprites.Prepare(SmithSource, "IbuKota_Penempa");
            Transform merchant = GetOrCreateNpc(world, "Merchant", prefab);
            Transform smith = GetOrCreateNpc(world, "Penempa Besi", prefab);

            Vector3 faceEntry = -layout.IntoLevel;
            PlaceNpc(layout, merchant, merchantPoint.position, faceEntry, UndoName, spawn.position, pua.position, portal.position);
            PlaceNpc(layout, smith, smithPoint.position, faceEntry, UndoName, spawn.position, merchant.position, portal.position);
            PlacePua(layout, world, pua, spawn, UndoName);
            SetNpc(pua, "Pua’", puaArt, camera, sequence);
            SetNpc(merchant, "Merchant", merchantArt, camera, sequence);
            SetNpc(smith, "Penempa Besi", smithArt, camera, sequence);

            Undo.RecordObject(sequence, UndoName);
            sequence.SetSpeakers(
                new GmPixelCrushersMainQuestDialogue.StepSpeaker(GmMainQuestStep.PersiapanSebelumBertarung, pua),
                new GmPixelCrushersMainQuestDialogue.StepSpeaker(GmMainQuestStep.KunjungiMerchant, merchant),
                new GmPixelCrushersMainQuestDialogue.StepSpeaker(GmMainQuestStep.KunjungiPenempaBesi, smith));
            Undo.RecordObject(journal, UndoName);
            journal.SetAreaLandmarks(BoundsForRoute(spawn, pua, merchant, smith, portal),
                new GmQuestLandmark(GmQuestCatalog.Guide, "Pua’", "Persiapan sebelum berangkat.", pua),
                new GmQuestLandmark(GmQuestCatalog.Merchant, "Merchant", "Perbekalan dan ramuan.", merchant),
                new GmQuestLandmark(GmQuestCatalog.Blacksmith, "Penempa Besi", "Perlengkapan untuk perjalanan.", smith));
            return pua;
        }

        private static Transform GetOrCreateNpc(Transform parent, string name, GameObject prefab)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            var npc = PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene) as GameObject;
            if (npc == null) throw new InvalidOperationException("Gagal memasang NPC " + name + ".");
            npc.transform.SetParent(parent, false);
            npc.name = name;
            PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
            Undo.RegisterCreatedObjectUndo(npc, UndoName);
            return npc.transform;
        }

        private static void PlacePua(GmWorldSceneLayout layout, Transform world, Transform pua, Transform spawn,
            string undoName)
        {
            Vector3 forward = ConfigurePuaArrivalView(world.gameObject.scene, spawn, undoName);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 near = spawn.position + forward * PuaForward + right * PuaRight;
            Vector3[] reserved = new[] { spawn, world.Find("Merchant"), world.Find("Penempa Besi"), world.Find("Portal") }
                .Where(point => point != null).Select(point => point.position).ToArray();
            PlaceNpc(layout, pua, near, spawn.position - near, undoName, reserved);
        }

        private static Vector3 ConfigurePuaArrivalView(Scene scene, Transform spawn, string undoName)
        {
            Vector3 spawnForward = Vector3.ProjectOnPlane(spawn.forward, Vector3.up).normalized;
            var route = RequireRoot(scene, "Daeng Saka - Encounter Flow").GetComponent<GmWorldScene>();
            if (route == null) return spawnForward;

            using var data = new SerializedObject(route);
            var entries = data.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("id").stringValue != GmWorldScene.DefaultEntry ||
                    entry.FindPropertyRelative("point").objectReferenceValue != spawn) continue;

                var lookAt = entry.FindPropertyRelative("cameraLookAt");
                var aim = lookAt.objectReferenceValue as Transform;
                if (aim == null)
                {
                    aim = spawn.Find(PuaArrivalAim);
                    if (aim == null)
                    {
                        var marker = new GameObject(PuaArrivalAim);
                        SceneManager.MoveGameObjectToScene(marker, scene);
                        aim = marker.transform;
                        aim.SetParent(spawn, false);
                        // The previous camera ray passes through the beringin behind the spawn.
                        aim.position = spawn.position + Quaternion.Euler(0f, PuaArrivalYaw, 0f) * spawnForward * 4f;
                        Undo.RegisterCreatedObjectUndo(marker, undoName);
                    }
                    lookAt.objectReferenceValue = aim;
                    data.ApplyModifiedProperties();
                }

                Vector3 direction = Vector3.ProjectOnPlane(aim.position - spawn.position, Vector3.up);
                return direction.sqrMagnitude > .0001f ? direction.normalized : spawnForward;
            }
            return spawnForward;
        }

        private static void PlaceNpc(GmWorldSceneLayout layout, Transform npc, Vector3 near, Vector3 forward,
            string undoName, params Vector3[] avoid)
        {
            var body = Require<CapsuleCollider>(npc);
            bool wasEnabled = body.enabled;
            body.enabled = false;
            try
            {
                Undo.RecordObject(npc, undoName);
                layout.Place(npc, near, forward, avoid);
            }
            finally
            {
                body.enabled = wasEnabled;
                Physics.SyncTransforms();
            }
        }

        private static void SetNpc(Transform npc, string name, GmDialogueNpcSprites.Prepared art,
            Camera camera, GmPixelCrushersMainQuestDialogue sequence)
        {
            var interaction = Require<GmPixelCrushersConversation>(npc);
            var actor = Require<DialogueActor>(npc);
            Transform visual = RequireChild(npc, "Visual");
            var renderer = Require<SpriteRenderer>(visual);
            var animator = Require<Animator>(visual);
            var billboard = Require<GmSpriteBillboard>(visual);
            var components = new Component[] { interaction, actor, renderer, animator, billboard };
            foreach (var component in components) Undo.RecordObject(component, UndoName);
            interaction.SetPresentation(name, "Bicara");
            interaction.SetMainQuestDialogue(sequence);
            actor.actor = name;
            renderer.sprite = art.First;
            animator.runtimeAnimatorController = art.Controller;
            billboard.SetCamera(camera);
            foreach (var component in components) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        private static Rect BoundsForRoute(Transform start, params Transform[] stops)
        {
            const float margin = 4f;
            Vector3 origin = start.position;
            float minX = origin.x, maxX = origin.x, minZ = origin.z, maxZ = origin.z;
            foreach (Transform stop in stops)
            {
                Vector3 position = stop.position;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minZ = Mathf.Min(minZ, position.z);
                maxZ = Mathf.Max(maxZ, position.z);
            }
            return new Rect(minX - margin, minZ - margin, maxX - minX + 2f * margin, maxZ - minZ + 2f * margin);
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
