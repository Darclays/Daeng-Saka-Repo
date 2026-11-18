using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmFishingSpotSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Install Fishing Spot";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";

        [MenuItem(Menu, true)]
        private static bool CanInstall() => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

        [MenuItem(Menu)]
        private static void Install()
        {
            if (!CanInstall()) return;
            Scene scene = SceneManager.GetSceneByPath(Village);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (!openedHere && scene.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Sorelia Fishing Spot");
            try
            {
                if (openedHere) scene = EditorSceneManager.OpenScene(Village, OpenSceneMode.Additive);
                InstallInScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Gagal menyimpan " + Village);
                Undo.CollapseUndoOperations(group);
                Debug.Log("[Daeng Saka 1C.7B] Spot dan tujuan peta terpasang di tepi sungai Desa Sorelia.");
            }
            catch (Exception error)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(error);
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void InstallInScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            Transform world = roots.SingleOrDefault(r => r.name == "Daeng Saka - World Gameplay")?.transform;
            GmQuestJournal journal = world != null ? world.GetComponentInChildren<GmQuestJournal>(true) : null;
            if (journal == null || world.GetComponentInChildren<GmFishingQuestDialogue>(true) == null)
                throw new InvalidOperationException("Pasang jurnal dan dialog Nelayan dengan Install Quest Journal dahulu.");
            Transform level = roots.SingleOrDefault(r => r.name == "LevelDesign")?.transform;
            MeshCollider water = level != null ? level.Find("Water")?.GetComponent<MeshCollider>() : null;
            TerrainCollider ground = level != null ? level.GetComponentInChildren<TerrainCollider>(true) : null;
            if (water == null || !water.enabled || ground == null || !ground.enabled)
                throw new InvalidOperationException("Water dan Terrain memerlukan collider aktif di Desa Sorelia.");
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(GmFishingSpot.ArtPath);
            if (atlas == null || atlas.width != 1024 || atlas.height != 468)
                throw new InvalidOperationException("Atlas SakaFishing berukuran 1024 × 468 perlu tersedia di " + GmFishingSpot.ArtPath);
            Camera camera = world.Find("Saka - Orbit Camera")?.GetComponent<Camera>();
            Transform stand = world.Find("Fishing Spot");
            Transform cast = stand != null ? stand.Find("Float Target") : null;
            Physics.SyncTransforms();
            if (stand == null)
            {
                if (!FindBank(water, ground, out Vector3 bank, out Vector3 landing))
                    throw new InvalidOperationException("Tepian yang lapang tidak ditemukan. Tempatkan Fishing Spot dan anak Float Target di bawah World Gameplay, lalu jalankan installer lagi.");
                stand = MakeChild(scene, world, "Fishing Spot", bank);
                cast = MakeChild(scene, stand, "Float Target", landing);
            }
            else if (cast == null)
            {
                if (!TryLanding(water, ground, stand.position, out Vector3 landing))
                    throw new InvalidOperationException("Tambahkan anak Float Target pada Fishing Spot yang sudah ada, lalu tempatkan di permukaan air.");
                cast = MakeChild(scene, stand, "Float Target", landing);
            }
            GmFishingSpot spot = stand.GetComponent<GmFishingSpot>();
            if (spot == null) spot = Undo.AddComponent<GmFishingSpot>(stand.gameObject);
            if (!stand.GetComponents<SphereCollider>().Any(c => c.isTrigger && c.enabled))
            {
                var trigger = Undo.AddComponent<SphereCollider>(stand.gameObject);
                trigger.isTrigger = true;
                trigger.center = Vector3.up * .8f;
                trigger.radius = .62f;
                EditorUtility.SetDirty(trigger);
            }
            Undo.RecordObject(spot, "Configure Fishing Spot");
            spot.Configure(journal, cast, atlas, camera);
            EditorUtility.SetDirty(spot);
            SetMapPoint(journal, stand);
        }

        private static Transform MakeChild(Scene scene, Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go.transform;
        }

        private static bool FindBank(MeshCollider water, TerrainCollider ground, out Vector3 bank, out Vector3 landing)
        {
            // Water-local coordinates on the accessible southern shore of the authored terrain.
            Vector3 preferred = water.transform.TransformPoint(new Vector3(-2.965f, 0f, -5.156f));
            bank = landing = default;
            for (int ring = 0; ring <= 7; ring++)
            {
                int samples = ring == 0 ? 1 : 16;
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * (2f * Mathf.PI / samples);
                    Vector3 p = preferred + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (.55f * ring);
                    if (!Surface(ground, p, out RaycastHit floor) || floor.normal.y < .83f ||
                        !TryLanding(water, ground, floor.point, out landing)) continue;
                    float rise = floor.point.y - landing.y;
                    if (rise < .12f || rise > 1.6f) continue;
                    Vector3 approach = floor.point + Vector3.ProjectOnPlane(floor.point - landing, Vector3.up).normalized;
                    if (!Surface(ground, approach, out RaycastHit back) || back.normal.y < .77f ||
                        Mathf.Abs(back.point.y - floor.point.y) > .70f ||
                        !HasClearance(floor.point, ground, water) || !HasClearance(back.point, ground, water)) continue;
                    bank = floor.point + Vector3.up * .04f;
                    return true;
                }
            }
            return false;
        }

        private static bool TryLanding(MeshCollider water, TerrainCollider ground, Vector3 bank, out Vector3 landing)
        {
            Vector3 forward = Vector3.ProjectOnPlane(water.bounds.center - bank, Vector3.up).normalized;
            foreach (float angle in new[] { 0f, 22f, -22f, 45f, -45f })
            {
                Vector3 p = bank + Quaternion.Euler(0f, angle, 0f) * forward * 2.8f;
                if (Surface(water, p, out RaycastHit surface) && Surface(ground, p, out RaycastHit bed) &&
                    surface.point.y - bed.point.y >= .18f)
                {
                    landing = surface.point;
                    return true;
                }
            }
            landing = default;
            return false;
        }

        private static bool Surface(Collider collider, Vector3 point, out RaycastHit hit)
        {
            Bounds bounds = collider.bounds;
            var ray = new Ray(new Vector3(point.x, bounds.max.y + 2f, point.z), Vector3.down);
            return collider.Raycast(ray, out hit, bounds.size.y + 4f);
        }

        private static bool HasClearance(Vector3 feet, TerrainCollider ground, MeshCollider water) =>
            !Physics.OverlapCapsule(feet + Vector3.up * .36f, feet + Vector3.up * 1.45f, .32f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(c =>
                c != ground && c != water && c.gameObject.scene == ground.gameObject.scene);

        private static void SetMapPoint(GmQuestJournal journal, Transform stand)
        {
            using var data = new SerializedObject(journal);
            SerializedProperty list = data.FindProperty("landmarks");
            SerializedProperty entry = null;
            for (int i = 0; i < list.arraySize; i++)
            {
                var row = list.GetArrayElementAtIndex(i);
                if (row.FindPropertyRelative("id").stringValue == GmQuestCatalog.FishingSpot) { entry = row; break; }
            }
            if (entry == null)
            {
                int index = list.arraySize++;
                entry = list.GetArrayElementAtIndex(index);
            }
            entry.FindPropertyRelative("id").stringValue = GmQuestCatalog.FishingSpot;
            entry.FindPropertyRelative("point").objectReferenceValue = stand;
            entry.FindPropertyRelative("label").stringValue = "Spot mancing";
            entry.FindPropertyRelative("description").stringValue = "Tepian sungai tempat Saka melempar kail.";
            data.ApplyModifiedProperties();
        }
    }
}
