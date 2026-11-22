using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmCityArchitectureSetup
    {
        private const string UndoName = "Set Up Ibu Kota Colliders";
        private const float StairStepOffset = .38f;

        internal static void Install(Scene scene, Transform world, CharacterController player, GmOrbitFollowCamera camera)
        {
            Transform level = RequireRoot(scene, "LevelDesain");
            Transform houses = RequireChild(level, "Rumah");
            Transform palace = RequireChild(level, "LingkunganKerajaan");
            Transform tenants = RequireChild(level, "Tenant");
            Transform rocks = level.Find("RockLock");
            // Use the sculpted steps and terrace floors, and preserve the gaps under gates.
            MeshFilter[] architecture = level.GetComponentsInChildren<MeshFilter>()
                .Where(part => part.sharedMesh != null && part.GetComponent<MeshRenderer>() != null &&
                    part.GetComponentInParent<Rigidbody>() == null &&
                    IsArchitecture(part, level, houses, palace, tenants, rocks))
                .ToArray();
            if (architecture.Length == 0)
                throw new InvalidOperationException("Mesh bangunan Ibu Kota belum ditemukan di LevelDesain.");

            int mask = 0;
            foreach (MeshFilter part in architecture)
            {
                EnsureSolidSurface(part);
                mask |= 1 << part.gameObject.layer;
            }
            Transform court = level.Find("Ground Kerajaan");
            if (court != null && court.TryGetComponent(out Collider floor))
                AddPalaceLanding(palace, floor);
            Undo.RecordObject(camera, UndoName);
            camera.IncludeObstructionLayers(mask);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);

            // The palace flight has risers around .33 m. A slightly larger offset lets the capsule climb them.
            Undo.RecordObject(player, UndoName);
            player.stepOffset = Mathf.Max(player.stepOffset, Mathf.Min(StairStepOffset, player.height * .3f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);

            Transform jamu = RequireChild(RequireChild(world, "Warga Ibu Kota"), "Tukang Jamu");
            Transform stall = tenants.GetComponentsInChildren<Transform>()
                .Where(part => part.parent == tenants && IsStall(part))
                .OrderBy(part => (part.position - jamu.position).sqrMagnitude)
                .FirstOrDefault();
            if (stall != null)
                EnableCounterInteraction(jamu, stall);

            Physics.SyncTransforms();
            Debug.Log($"[Daeng Saka] {architecture.Length} bagian arsitektur dan batu Ibu Kota siap untuk Saka dan kamera orbit.", level);
        }

        private static bool IsArchitecture(MeshFilter mesh, Transform level, Transform houses, Transform palace,
            Transform tenants, Transform rocks)
        {
            Transform part = mesh.transform;
            return part.IsChildOf(houses) || part.IsChildOf(palace) ||
                IsShopStructure(part, tenants) || FindChildOf(part, level)?.name == "3D_IbuKota_Kerajaan_Patung" ||
                (rocks != null && part.IsChildOf(rocks) &&
                    (part.name.StartsWith("SM_Rocks_", StringComparison.Ordinal) ||
                     mesh.sharedMesh.name.StartsWith("SM_Rocks_", StringComparison.Ordinal)));
        }

        private static bool IsShopStructure(Transform part, Transform tenants)
        {
            if (!part.IsChildOf(tenants) || part == tenants) return false;
            Transform shop = FindChildOf(part, tenants);
            return IsStall(shop) ||
                shop.name.StartsWith("Asset3D_IbuKota_Blacksmith", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStall(Transform part) =>
            part.name.StartsWith("3D_Ibukota_Tenant_1", StringComparison.OrdinalIgnoreCase);

        private static Transform FindChildOf(Transform part, Transform parent)
        {
            if (part == parent || !part.IsChildOf(parent)) return null;
            while (part.parent != parent) part = part.parent;
            return part;
        }

        private static void EnsureSolidSurface(MeshFilter part)
        {
            var meshes = part.GetComponents<MeshCollider>();
            if (meshes.Any(c => c.enabled && !c.isTrigger && !c.convex && c.sharedMesh == part.sharedMesh)) return;
            var surface = meshes.FirstOrDefault(c => !c.isTrigger && c.sharedMesh == part.sharedMesh);
            if (surface == null) surface = AddComponent<MeshCollider>(part.transform);
            Undo.RecordObject(surface, UndoName);
            surface.convex = false;
            surface.sharedMesh = part.sharedMesh;
            surface.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
        }

        private static void AddPalaceLanding(Transform palace, Collider court)
        {
            Transform stairs = palace.Find("3D_IbuKota_Kerajaan_Tangga");
            if (stairs == null || court == null) return;
            MeshRenderer[] renderers = stairs.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return;
            Bounds flight = renderers[0].bounds;
            foreach (MeshRenderer part in renderers.Skip(1)) flight.Encapsulate(part.bounds);
            const string surfaceName = "Sambungan ke Pelataran";
            float height = flight.max.y - court.bounds.max.y;
            if (height <= StairStepOffset)
            {
                Transform previous = stairs.Find(surfaceName);
                if (previous != null && previous.gameObject.activeSelf)
                {
                    Undo.RecordObject(previous.gameObject, UndoName);
                    previous.gameObject.SetActive(false);
                }
                return;
            }

            Vector3 across = Vector3.ProjectOnPlane(stairs.forward, Vector3.up).normalized;
            if (Vector3.Dot(across, court.bounds.center - flight.center) < 0f) across = -across;
            float span = Mathf.Abs(across.x) * flight.extents.x + Mathf.Abs(across.z) * flight.extents.z;
            Vector3 top = flight.center + across * (span - .12f);
            top.y = flight.max.y + .012f;
            Vector3 bottom = top + across * Mathf.Max(1f, 1.75f * height);
            bottom.y = court.bounds.max.y + .012f;
            const float thickness = .12f;
            Quaternion angle = Quaternion.LookRotation(bottom - top, Vector3.up);
            Transform landing = stairs.Find(surfaceName);
            if (landing == null)
            {
                var host = new GameObject(surfaceName);
                SceneManager.MoveGameObjectToScene(host, stairs.gameObject.scene);
                landing = host.transform;
                // Keep the new collider at unit world scale even on scaled FBX instances.
                landing.SetParent(stairs, true);
                Undo.RegisterCreatedObjectUndo(host, UndoName);
            }
            Undo.RecordObject(landing.gameObject, UndoName);
            landing.gameObject.SetActive(true);
            Undo.RecordObject(landing, UndoName);
            landing.SetPositionAndRotation((top + bottom) * .5f - angle * Vector3.up * (thickness * .5f), angle);
            var surface = GetOrAdd<BoxCollider>(landing);
            Undo.RecordObject(surface, UndoName);
            surface.enabled = true;
            surface.isTrigger = false;
            surface.center = Vector3.zero;
            surface.size = new Vector3(6f, thickness, (bottom - top).magnitude + .035f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(landing);
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
        }

        private static void EnableCounterInteraction(Transform jamu, Transform stall)
        {
            Transform point = jamu.Find("Jangkauan Kedai");
            if (point == null)
            {
                var host = new GameObject("Jangkauan Kedai");
                SceneManager.MoveGameObjectToScene(host, jamu.gameObject.scene);
                point = host.transform;
                point.SetParent(jamu, false);
                Undo.RegisterCreatedObjectUndo(host, UndoName);
            }
            var sensor = GetOrAdd<SphereCollider>(point);
            Undo.RecordObject(sensor, UndoName);
            sensor.enabled = true;
            sensor.isTrigger = true;
            sensor.center = Vector3.up * .8f;
            sensor.radius = 2.85f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sensor);

            var reach = GetOrAdd<GmShopInteractionReach>(jamu);
            Undo.RecordObject(reach, UndoName);
            reach.Configure(sensor, stall);
            PrefabUtility.RecordPrefabInstancePropertyModifications(reach);
        }
    }
}
