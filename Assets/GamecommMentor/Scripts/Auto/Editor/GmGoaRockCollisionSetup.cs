using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GamecommMentor.DaengSaka.Editor
{
    internal sealed class GmGoaRockCollisionSetup
    {
        private const string UndoName = "Set Up Goa Rock Colliders";
        private const float EyeRadius = .20f;
        private readonly HashSet<Collider> obstacles;

        private GmGoaRockCollisionSetup(HashSet<Collider> obstacles) => this.obstacles = obstacles;

        internal static GmGoaRockCollisionSetup Install(Transform cave, GmOrbitFollowCamera orbit)
        {
            int added = 0;
            MeshFilter[] rocks = cave.GetComponentsInChildren<MeshFilter>()
                .Where(filter => filter.sharedMesh != null &&
                    (filter.name.StartsWith("SM_Rocks_", StringComparison.Ordinal) ||
                     filter.sharedMesh.name.StartsWith("SM_Rocks_", StringComparison.Ordinal)))
                .ToArray();
            if (rocks.Length == 0)
                throw new InvalidOperationException("Mesh SM_Rocks tidak ditemukan di LevelDesign/Goa.");

            foreach (MeshFilter rock in rocks)
            {
                Collider[] attached = rock.GetComponents<Collider>();
                if (attached.Any(IsSolid)) continue;

                var collider = attached.OfType<MeshCollider>().FirstOrDefault(value => !value.isTrigger);
                if (collider == null)
                {
                    collider = Undo.AddComponent<MeshCollider>(rock.gameObject);
                    if (collider == null)
                        throw new InvalidOperationException($"Gagal memasang Mesh Collider di '{rock.name}'.");
                    added++;
                }
                Undo.RecordObject(collider, UndoName);
                collider.convex = false;
                collider.sharedMesh = rock.sharedMesh;
                collider.enabled = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }

            var obstacles = new HashSet<Collider>(cave.GetComponentsInChildren<Collider>().Where(IsSolid));
            int obstacleLayers = 0;
            foreach (Collider obstacle in obstacles) obstacleLayers |= 1 << obstacle.gameObject.layer;
            using var settings = new SerializedObject(orbit);
            SerializedProperty layers = settings.FindProperty("obstructionLayers");
            int mask = layers.intValue | obstacleLayers;
            if (layers.intValue != mask)
            {
                Undo.RecordObject(orbit, UndoName);
                layers.intValue = mask;
                settings.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(orbit);
            }

            Physics.SyncTransforms();
            Debug.Log($"[Daeng Saka 1E.17B R1] Goa memakai {rocks.Length} mesh batu. Collider baru: {added}.", cave);
            return new GmGoaRockCollisionSetup(obstacles);
        }

        private static bool IsSolid(Collider collider) => collider.enabled && !collider.isTrigger &&
            (collider is not MeshCollider mesh || mesh.sharedMesh != null);

        internal bool CanPlaceActor(Vector3 feet, Vector3 chamber, float radius, float height)
        {
            Vector3 bottom = feet + Vector3.up * radius;
            Vector3 top = feet + Vector3.up * Mathf.Max(radius, height - radius);
            if (Overlaps(bottom, top, radius)) return false;

            // Check the space from inside the chamber as well. A capsule fully inside a rock
            // can miss every surface of its concave mesh.
            Vector3 chest = feet + Vector3.up * (height * .55f);
            return HasClearSweep(chamber + Vector3.up * 1.05f, chest, .13f);
        }

        internal bool HasCameraSpace(Vector3 eye) => !Overlaps(eye, eye, EyeRadius);

        internal bool IsCameraClear(Vector3 eye, Vector3 focus, Vector3 boss) =>
            HasCameraSpace(eye) && HasClearSweep(focus, eye, EyeRadius) &&
            HasClearSweep(boss + Vector3.up * 1.10f, eye, .045f);

        internal void RequireClearCameraMove(Vector3 from, Vector3 to, string label)
        {
            if (!HasClearSweep(from, to, EyeRadius))
                throw new InvalidOperationException($"Gerak kamera '{label}' memotong batu. Cek marker awal dan akhir.");
        }

        internal Vector3 FindCameraEye(Vector3 preferred, Vector3 focus, Vector3 boss, string label)
        {
            Vector3 inward = Vector3.ProjectOnPlane(focus - preferred, Vector3.up).normalized;
            // Keep the angle and height close to the authored pose when a rock was nudged.
            for (int step = 0; step <= 6; step++)
            {
                Vector3 eye = preferred + inward * (.23f * step);
                if (IsCameraClear(eye, focus, boss)) return eye;
            }
            throw new InvalidOperationException($"Shot '{label}' terhalang batu. Geser marker kamera ke ruang terbuka.");
        }

        private bool Overlaps(Vector3 bottom, Vector3 top, float radius) =>
            Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore).Any(obstacles.Contains);

        private bool HasClearSweep(Vector3 start, Vector3 end, float radius)
        {
            Vector3 span = end - start;
            float length = span.magnitude;
            if (length <= .002f) return true;
            Vector3 direction = span / length;
            return !Hits(start, radius, direction, length) && !Hits(end, radius, -direction, length);
        }

        private bool Hits(Vector3 origin, float radius, Vector3 direction, float distance) =>
            Physics.SphereCastAll(origin, radius, direction, distance, ~0, QueryTriggerInteraction.Ignore)
                .Any(hit => obstacles.Contains(hit.collider));
    }
}
