using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal sealed class GmWorldSceneLayout
    {
        private readonly Scene scene;
        private readonly Collider[] floors;
        private readonly float clearanceRadius;
        private readonly float clearanceHeight;
        private readonly float minFloorNormalY;

        public Transform StartGuide { get; }
        public Transform PortalGuide { get; }
        public Vector3 IntoLevel { get; }

        public GmWorldSceneLayout(Scene scene, string startName, string portalName, CharacterController body)
        {
            this.scene = scene;
            Transform[] transforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            StartGuide = FindGuide(transforms, startName);
            PortalGuide = startName == portalName ? StartGuide : FindGuide(transforms, portalName);
            floors = transforms.SelectMany(t => t.GetComponents<Collider>())
                .Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger &&
                    (c is TerrainCollider || (c is MeshCollider && (c.name == "Plane" || c.name == "Plane (1)"))))
                .ToArray();
            if (floors.Length == 0)
                throw new InvalidOperationException($"Ground collider tidak ditemukan di {scene.path}.");
            clearanceRadius = body.radius + body.skinWidth + 0.1f;
            clearanceHeight = Mathf.Max(body.height + 0.15f, 2f * clearanceRadius);
            minFloorNormalY = Mathf.Cos(Mathf.Min(body.slopeLimit, 40f) * Mathf.Deg2Rad);

            Vector3 toward = StartGuide == PortalGuide ? floors[0].bounds.center : PortalGuide.position;
            IntoLevel = Vector3.ProjectOnPlane(toward - StartGuide.position, Vector3.up).normalized;
            if (IntoLevel.sqrMagnitude < 0.01f) IntoLevel = Vector3.right;
        }

        private Transform FindGuide(IEnumerable<Transform> transforms, string name)
        {
            Transform[] matches = transforms.Where(t => t.name == name && t.parent != null &&
                t.parent.name == "PortalPoint").ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"'{name}' harus ada satu kali di bawah PortalPoint pada {scene.path}.");
            return matches[0];
        }

        public void HidePlaceholders()
        {
            DisablePlaceholder(StartGuide);
            if (PortalGuide != StartGuide) DisablePlaceholder(PortalGuide);

            if (StartGuide != PortalGuide) return;
            Transform level = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "LevelDesign")?.transform;
            Transform caveCapsule = level != null ? level.Find("Capsule") : null;
            Vector3 oldPosition = new Vector3(48.12f, 0.738f, 47.3f);
            if (caveCapsule != null && (caveCapsule.localPosition - oldPosition).sqrMagnitude < 0.001f &&
                caveCapsule.childCount == 0 && caveCapsule.GetComponent<CapsuleCollider>() != null &&
                caveCapsule.GetComponents<Component>().Length == 4)
                DisablePlaceholder(caveCapsule);
        }

        private static void DisablePlaceholder(Transform guide)
        {
            CapsuleCollider capsule = guide.GetComponent<CapsuleCollider>();
            if (capsule == null) return;
            Undo.RecordObject(capsule, "Hide portal capsule");
            capsule.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(capsule);
            MeshRenderer renderer = guide.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            Undo.RecordObject(renderer, "Hide portal capsule");
            renderer.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        public void Place(Transform target, Vector3 near, Vector3 forward, params Vector3[] avoid)
        {
            Vector3 feet = FindGroundedPosition(target.name, near, avoid);
            target.SetPositionAndRotation(feet, Quaternion.LookRotation(forward, Vector3.up));
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private Vector3 FindGroundedPosition(string label, Vector3 near, Vector3[] avoid)
        {
            Physics.SyncTransforms();
            if (TryPosition(near, avoid, out Vector3 feet)) return feet;
            for (float radius = 0.75f; radius <= 6f; radius += 0.75f)
            {
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * Mathf.PI / 6f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (TryPosition(near + offset, avoid, out feet)) return feet;
                }
            }
            throw new InvalidOperationException($"Tidak ada titik yang bisa dipijak untuk '{label}' di sekitar {near} ({scene.name}).");
        }

        private bool TryPosition(Vector3 sample, Vector3[] avoid, out Vector3 feet)
        {
            feet = default;
            foreach (Vector3 reserved in avoid)
                if (Vector3.ProjectOnPlane(reserved - sample, Vector3.up).sqrMagnitude < 2.2f * 2.2f)
                    return false;

            var ray = new Ray(sample + Vector3.up * 4f, Vector3.down);
            float groundY = float.NegativeInfinity;
            foreach (Collider floor in floors)
                if (floor.Raycast(ray, out RaycastHit hit, 12f) && hit.normal.y >= minFloorNormalY)
                    groundY = Mathf.Max(groundY, hit.point.y);
            if (float.IsNegativeInfinity(groundY)) return false;

            feet = new Vector3(sample.x, groundY + 0.04f, sample.z);
            Vector3 bottom = feet + Vector3.up * clearanceRadius;
            Vector3 top = feet + Vector3.up * (clearanceHeight - clearanceRadius);
            return !Physics.OverlapCapsule(bottom, top, clearanceRadius, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore).Any(c => c.gameObject.scene == scene && !floors.Contains(c));
        }
    }
}
