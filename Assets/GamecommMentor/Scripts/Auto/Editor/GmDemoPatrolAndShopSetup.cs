using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmDemoPatrolAndShopSetup
    {
        private const string Scenes = "Assets/GamecommMentor/Scenes_GamecommMentor/";
        private const string Capital = Scenes + "IbuKota.unity";
        private const string Cave = Scenes + "GoaLembanyung.unity";
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Demo Patrol and Shops";
        private const string UndoName = "Set Up Demo Patrol and Shops";
        private const string Roster = "Assets/GamecommMentor/Data/PatroliPoppoGoa.asset";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && (SceneManager.GetActiveScene().path == Capital ||
                SceneManager.GetActiveScene().path == Cave);

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
                Transform selection = scene.path == Capital ? InstallShops(scene) : InstallPatrol(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = selection.gameObject;
                Debug.Log($"[Daeng Saka 1E.26B] {scene.name} siap. Simpan scene.", selection);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        internal static Transform InstallShops(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform merchant = RequireChild(world, "Merchant");
            Transform smith = RequireChild(world, "Penempa Besi");
            Transform shops = RequireChild(RequireRoot(scene, "LevelDesain"), "Tenant");
            Transform stall = RequireChild(shops, "3D_Ibukota_Tenant_1 (2)");
            Transform forge = RequireChild(shops, "Asset3D_IbuKota_Blacksmith");
            Transform anvil = RequireChild(forge, "Asset3D_IbuKota_Blacksmith_Anvil");
            Transform table = RequireChild(forge, "Asset3D_IbuKota_Blacksmith_MejaBS");

            ResizeMerchantStall(stall, shops);
            SetShopReach(merchant, 3.05f, stall);
            SetShopReach(smith, 3.45f, anvil, table);
            Physics.SyncTransforms();
            return merchant;
        }

        private static void ResizeMerchantStall(Transform stall, Transform shops)
        {
            MeshRenderer[] renderers = stall.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Mesh kedai Merchant belum siap.");

            const float canopyHeight = 4.12f;
            float height = renderers.Max(renderer => renderer.bounds.max.y) - stall.position.y;
            if (height <= canopyHeight + .015f) return;
            float ratio = canopyHeight / height;
            Vector3 origin = stall.position;
            Undo.RecordObject(stall, UndoName);
            stall.localScale *= ratio;
            PrefabUtility.RecordPrefabInstancePropertyModifications(stall);

            // The dishes are siblings of this stall in IbuKota. Their offsets follow its floor pivot.
            for (int index = 0; index < shops.childCount; index++)
            {
                Transform dish = shops.GetChild(index);
                if ((!dish.name.StartsWith("3D_Ibukota_Tenant_Kue", StringComparison.Ordinal) &&
                     !dish.name.StartsWith("3D_Ibukota_Tenant_MasakCrafter", StringComparison.Ordinal)) ||
                    Vector3.ProjectOnPlane(dish.position - origin, Vector3.up).sqrMagnitude > 2.45f * 2.45f) continue;
                Undo.RecordObject(dish, UndoName);
                dish.position = origin + (dish.position - origin) * ratio;
                dish.localScale *= ratio;
                PrefabUtility.RecordPrefabInstancePropertyModifications(dish);
            }
        }

        private static void SetShopReach(Transform npc, float radius, params Transform[] counters)
        {
            Require<GmPixelCrushersConversation>(npc);
            Transform child = GetOrCreate(npc, "Jangkauan Layanan", Vector3.zero);
            var sensor = Ensure<SphereCollider>(child);
            var reach = Ensure<GmShopInteractionReach>(npc);
            Undo.RecordObject(sensor, UndoName);
            sensor.enabled = true;
            sensor.isTrigger = true;
            sensor.center = Vector3.up * .8f;
            sensor.radius = radius;
            Undo.RecordObject(reach, UndoName);
            reach.Configure(sensor, counters);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sensor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(reach);
        }

        internal static Transform InstallPatrol(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform flow = RequireRoot(scene, "Daeng Saka - Encounter Flow");
            var intro = Require<GmGoaArrivalIntro>(RequireChild(world, "Cutscene - Masuk Goa Lembayung"));
            Transform patrolRoot = RequireChild(world, "Poppo - Mulut Goa");
            Transform visual = RequireChild(patrolRoot, "Visual");
            var hero = Require<GmSakaMotor>(RequireChild(world, "Saka"));
            var terrain = Require<TerrainCollider>(RequireChild(RequireRoot(scene, "LevelDesign"), "Terrain"));
            Transform places = RequireRoot(scene, "NPCPlaces");
            Transform mouth = RequireChild(places, "EC_Poppo_MulutGoa");
            Vector3 end = mouth.position + new Vector3(3.8f, 0, 1.16f);
            var near = GetOrCreate(places, "Patroli Poppo - A", mouth.position);
            var far = GetOrCreate(places, "Patroli Poppo - B", end);
            var roster = AssetDatabase.LoadAssetAtPath<GmEncounterRoster>(Roster);
            if (roster == null || !roster.HasEnemies)
                throw new InvalidOperationException("PatroliPoppoGoa.asset belum tersedia di GamecommMentor/Data.");
            Vector3 direction = Vector3.ProjectOnPlane(far.position - near.position, Vector3.up);
            if (direction.sqrMagnitude < .25f)
                throw new InvalidOperationException("Pisahkan marker Patroli Poppo - A dan B.");

            Physics.SyncTransforms();
            // Validate the stretch, including the space between the two authored endpoints.
            for (int sample = 0; sample <= 16; sample++)
                if (!GmCavePatrol.TryGround(terrain, Vector3.Lerp(near.position, far.position, sample / 16f), out _))
                    throw new InvalidOperationException("Jalur Poppo menyentuh penghalang atau lereng. Cek marker Patroli Poppo - A dan B.");
            GmCavePatrol.TryGround(terrain, near.position, out Vector3 home);
            Undo.RecordObject(patrolRoot, UndoName);
            patrolRoot.SetPositionAndRotation(home, Quaternion.LookRotation(direction, Vector3.up));

            var encounter = Ensure<GmEnemyEncounter>(patrolRoot);
            var trigger = Ensure<SphereCollider>(patrolRoot);
            var body = Ensure<Rigidbody>(patrolRoot);
            Undo.RecordObject(encounter, UndoName);
            encounter.Configure(Require<GmEncounterFlow>(flow), "world-cave-patrol-poppo", "Poppo Patroli");
            encounter.SetRoster(roster);
            encounter.SetStartWhenReadyWhileTouching(true);
            encounter.SetRememberVictory(true);
            Undo.RecordObject(body, UndoName);
            body.isKinematic = true;
            body.useGravity = false;
            Undo.RecordObject(trigger, UndoName);
            trigger.center = Vector3.up * .8f;
            trigger.radius = .72f;
            trigger.isTrigger = true;
            trigger.enabled = false;

            var alert = MakeWarning(visual);
            var patrol = Ensure<GmCavePatrol>(patrolRoot);
            Undo.RecordObject(patrol, UndoName);
            patrol.Configure(hero, Require<GmMainQuestProgress>(flow), terrain, near, far, visual, alert);
            Undo.RecordObject(intro, UndoName);
            intro.SetMouthPatrol(patrol);
            return patrolRoot;
        }

        private static GameObject MakeWarning(Transform visual)
        {
            const string name = "Poppo - Waspada";
            Transform existing = visual.Find(name);
            if (existing != null) return existing.gameObject;
            GameObject warning = GmCavePatrol.MakeWarning(visual, name, 1.34f);
            Undo.RegisterCreatedObjectUndo(warning, UndoName);
            return warning;
        }

        private static Transform GetOrCreate(Transform parent, string name, Vector3 position)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            var child = new GameObject(name);
            SceneManager.MoveGameObjectToScene(child, parent.gameObject.scene);
            child.transform.SetParent(parent, false);
            if (position != Vector3.zero) child.transform.position = position;
            Undo.RegisterCreatedObjectUndo(child, UndoName);
            return child.transform;
        }

        private static T Ensure<T>(Transform owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            if (component == null) component = Undo.AddComponent<T>(owner.gameObject);
            if (component == null) throw new InvalidOperationException($"Gagal menambahkan {typeof(T).Name} pada '{owner.name}'.");
            return component;
        }
    }
}
