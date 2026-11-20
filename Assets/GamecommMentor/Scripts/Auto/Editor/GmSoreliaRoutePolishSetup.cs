using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmSoreliaRoutePolishSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Sorelia Route Polish";
        private const string UndoName = "Set Up Sorelia Route Polish";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";
        private const string Art = "Assets/Asset3D/Asset3D_GuaLembayung/";
        private const string Rocks = Art + "3D_GuaLembayung_StylizedRock/Prefabs/";
        private const string Camp = Art + "3D_GuaLembayung_Camp/StickTogether/";
        private const string GroupName = "Dekor Jalur Sorelia";

        [MenuItem(Menu, true)]
        private static bool CanPolish() => CanInstall(Village);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanPolish()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                Transform group = Install(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = group.gameObject;
                Debug.Log($"[Daeng Saka 1E.13B] Dekor Jalur Sorelia berisi {group.childCount} properti. Simpan scene.", group);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
            }
        }

        private static Transform Install(Scene scene)
        {
            Transform level = RequireRoot(scene, "LevelDesign");
            var terrain = Require<TerrainCollider>(RequireChild(level, "Terrain"));
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform portal = RequireChild(world, "Portal");
            Transform caveSpawn = RequireChild(world, "Spawn - Dari Goa");
            Transform villageSpawn = RequireChild(world, "Spawn - Start");
            Transform poppo = RequireChild(world, "Poppo (Uji)");
            Transform fishing = RequireChild(world, "Fishing Spot");
            Transform family = RequireChild(world, "Keluarga Nelayan");
            Transform familyPoints = RequireChild(family, "Posisi Keluarga");
            Transform after = RequireChild(familyPoints, "Setelah Ditolong");
            Transform fisher = RequireChild(after, "Ayah");
            Vector3 caveAxis = AlongGround(portal.position, caveSpawn.position);
            Vector3 riverAxis = AlongGround(fisher.position, fishing.position);
            var protectedPoints = new List<Vector3>
            {
                portal.position, caveSpawn.position, villageSpawn.position, poppo.position, fishing.position,
                RequireChild(world, "Nelayan (Uji)").position
            };
            foreach (string stage in new[] { "Saat Diserang", "Setelah Ditolong" })
                AddChildPositions(RequireChild(familyPoints, stage), protectedPoints);
            Transform villagers = world.Find("Warga Sorelia Setelah Ditolong");
            if (villagers != null) AddChildPositions(villagers, protectedPoints);

            var passages = new[]
            {
                (Start: portal.position, End: caveSpawn.position + caveAxis * 7f),
                (Start: poppo.position, End: fisher.position),
                (Start: fisher.position, End: fishing.position)
            };

            Transform group = level.Find(GroupName);
            if (group == null)
            {
                var root = new GameObject(GroupName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetParent(level, false);
                Undo.RegisterCreatedObjectUndo(root, UndoName);
                group = root.transform;
            }

            void Add(string name, string path, Vector3 anchor, Vector3 forward, float along, float side,
                float yaw, float width, float height)
            {
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                Vector3 point = anchor + forward * along + right * side;
                Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, yaw, 0f);
                Vector3 outward = right * Mathf.Sign(side) * 0.35f;
                AddProp(group, name, path, rotation, width, height, terrain, protectedPoints, passages,
                    point, point + outward + forward * 0.65f, point + outward - forward * 0.65f);
            }

            Add("Batu jalur goa 1", Rocks + "SM_Rocks_03.prefab", portal.position, caveAxis, 4.3f, 3.1f, 19f, 1.7f, 0.85f);
            Add("Batu jalur goa 2", Rocks + "SM_Rocks_06.prefab", portal.position, caveAxis, 4.9f, -3.35f, -32f, 1.25f, 0.72f);
            Add("Batang kayu dekat Nelayan", Camp + "Environment/TreeLog/Prefabs/P_TreeLog.prefab",
                fisher.position, riverAxis, -3f, -1.7f, 90f, 2.05f, 0.6f);
            Add("Semak dekat Nelayan", Camp + "Foliage/Hazel/Prefabs/P_Hazel.prefab",
                fisher.position, riverAxis, -3.55f, -3.8f, -18f, 0.85f, 1f);
            return group;
        }

        private static void AddProp(Transform group, string name, string path, Quaternion rotation,
            float width, float height, TerrainCollider terrain, List<Vector3> protectedPoints,
            (Vector3 Start, Vector3 End)[] passages, params Vector3[] positions)
        {
            if (group.Find(name) != null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Daeng Saka] Aset dekor belum terbaca di {path}.", group);
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab, group.gameObject.scene) as GameObject;
            if (instance == null) throw new InvalidOperationException($"Gagal membuat properti '{name}'.");
            bool installed = false;
            try
            {
                Transform prop = instance.transform;
                prop.SetParent(group, false);
                prop.SetPositionAndRotation(positions[0], rotation);
                MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>();
                MeshFilter[] meshes = instance.GetComponentsInChildren<MeshFilter>()
                    .Where(filter => filter.sharedMesh != null).ToArray();
                if (renderers.Length == 0 || meshes.Length == 0)
                {
                    Debug.LogWarning($"[Daeng Saka] Mesh dekor '{name}' belum terbaca di {path}.", group);
                    return;
                }
                Vector3 size = Measure(renderers).size;
                float span = Mathf.Max(size.x, size.z);
                if (span < 0.001f || size.y < 0.001f)
                {
                    Debug.LogWarning($"[Daeng Saka] Batas visual '{name}' belum terbaca di {path}.", group);
                    return;
                }
                float scale = Mathf.Min(width / span, height / size.y);
                prop.localScale *= scale;

                foreach (Vector3 position in positions)
                {
                    var ray = new Ray(position + Vector3.up * 4f, Vector3.down);
                    if (!terrain.Raycast(ray, out RaycastHit ground, 12f) || ground.normal.y < 0.87f) continue;

                    Bounds unplaced = Measure(renderers);
                    var bottomCenter = new Vector3(unplaced.center.x, unplaced.min.y + 0.035f, unplaced.center.z);
                    prop.position += ground.point - bottomCenter;
                    Bounds bounds = Measure(renderers);
                    if (!HasClearance(instance, bounds, terrain, protectedPoints, passages)) continue;

                    if (!instance.GetComponentsInChildren<Collider>().Any(collider => collider.enabled && !collider.isTrigger))
                        foreach (MeshFilter filter in meshes)
                            filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;

                    instance.name = name;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                    Undo.RegisterCreatedObjectUndo(instance, UndoName);
                    installed = true;
                    return;
                }

                Debug.LogWarning($"[Daeng Saka] '{name}' dilewati karena ruang di tepi jalur tidak cukup.", group);
            }
            finally
            {
                if (!installed) UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static bool HasClearance(GameObject prop, Bounds bounds, TerrainCollider terrain,
            List<Vector3> protectedPoints, (Vector3 Start, Vector3 End)[] passages)
        {
            Vector3 center = Flat(bounds.center);
            float radius = Flat(bounds.extents).magnitude;
            if (protectedPoints.Any(point => (Flat(point) - center).magnitude < radius + 1.5f)) return false;
            if (passages.Any(passage => DistanceToPassage(center, passage.Start, passage.End) < radius + 1.25f)) return false;

            Physics.SyncTransforms();
            return !Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(collider =>
                    collider != terrain && collider.gameObject.scene == prop.scene &&
                    collider.transform != prop.transform && !collider.transform.IsChildOf(prop.transform));
        }

        private static float DistanceToPassage(Vector3 center, Vector3 from, Vector3 to)
        {
            Vector3 start = Flat(from);
            Vector3 axis = Flat(to) - start;
            if (axis.sqrMagnitude < 0.001f) return (center - start).magnitude;
            float progress = Mathf.Clamp01(Vector3.Dot(center - start, axis) / axis.sqrMagnitude);
            return (center - (start + axis * progress)).magnitude;
        }

        private static Bounds Measure(MeshRenderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void AddChildPositions(Transform parent, List<Vector3> positions)
        {
            for (int i = 0; i < parent.childCount; i++) positions.Add(parent.GetChild(i).position);
        }

        private static Vector3 AlongGround(Vector3 from, Vector3 to)
        {
            Vector3 direction = Flat(to - from);
            return direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
        }

        private static Vector3 Flat(Vector3 position) => new Vector3(position.x, 0f, position.z);
    }
}
