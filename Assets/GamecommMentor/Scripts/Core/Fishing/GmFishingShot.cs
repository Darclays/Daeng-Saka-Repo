using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    // One session borrows the scene camera. The existing brain retains its own yaw and pitch.
    internal sealed class GmFishingShot
    {
        private const float ShotFov = 37f;
        private static readonly float[] AlternateAngles = { 5f, 27f, -12f, -25f };
        private readonly Camera camera;
        private readonly Transform player;
        private readonly MeshCollider water;
        private readonly GmOrbitFollowCamera orbit;
        private readonly GmFixedFollowCamera fixedFollow;
        private bool active, orbitWasOn, fixedWasOn, wasOrtho;
        private float previousFov;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        internal Vector3 Stand { get; }
        internal Vector3 Float { get; }
        internal Camera Camera => camera;

        internal GmFishingShot(Camera camera, Transform player, Transform bank, Transform floatTarget)
        {
            this.camera = camera;
            this.player = player;
            orbit = camera.GetComponent<GmOrbitFollowCamera>();
            fixedFollow = camera.GetComponent<GmFixedFollowCamera>();
            Transform level = null;
            foreach (GameObject root in bank.gameObject.scene.GetRootGameObjects())
                if (root.name == "LevelDesign") { level = root.transform; break; }
            TerrainCollider floor = level != null ? level.GetComponentInChildren<TerrainCollider>() : null;
            water = level != null ? level.Find("Water")?.GetComponent<MeshCollider>() : null;

            Vector3 offset = Vector3.ProjectOnPlane(floatTarget.position - bank.position, Vector3.up);
            Vector3 direction = offset.normalized;
            Stand = bank.position;
            Float = floatTarget.position;
            // Use the verified dry crest if it still has room for Saka.
            if (Surface(floor, bank.position + direction * .8f, out RaycastHit crest) &&
                crest.normal.y >= .83f && crest.point.y >= Float.y + .12f &&
                Mathf.Abs(crest.point.y - bank.position.y) < .60f && CanStand(crest.point, floor))
                Stand = crest.point + Vector3.up * .04f;

            // Extend only the original straight 2.8 m placement on the flat Sorelia water.
            Vector3 oldOffset = water != null ?
                Vector3.ProjectOnPlane(water.bounds.center - bank.position, Vector3.up).normalized * 2.8f : Vector3.zero;
            if (water != null && (offset - oldOffset).sqrMagnitude < .025f * .025f &&
                Mathf.Abs(Float.y - water.bounds.center.y) < .025f &&
                Surface(water, Float + direction * .4f, out RaycastHit surface) &&
                Surface(floor, surface.point, out RaycastHit bed) && surface.point.y - bed.point.y >= .18f)
                Float = surface.point;
        }

        private static bool Surface(Collider source, Vector3 xz, out RaycastHit hit)
        {
            hit = default;
            if (source == null || !source.enabled) return false;
            Bounds bounds = source.bounds;
            var start = new Vector3(xz.x, bounds.max.y + 2f, xz.z);
            return source.Raycast(new Ray(start, Vector3.down), out hit, bounds.size.y + 4f);
        }

        private bool CanStand(Vector3 feet, TerrainCollider ground)
        {
            foreach (Collider obstacle in Physics.OverlapCapsule(feet + Vector3.up * .36f,
                feet + Vector3.up * 1.45f, .32f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (obstacle != ground && obstacle != water && obstacle.gameObject.scene == player.gameObject.scene &&
                    !obstacle.transform.IsChildOf(player)) return false;
            return true;
        }

        internal void Enter()
        {
            if (active) return;
            previousPosition = camera.transform.position;
            previousRotation = camera.transform.rotation;
            previousFov = camera.fieldOfView;
            wasOrtho = camera.orthographic;
            orbitWasOn = orbit != null && orbit.enabled;
            fixedWasOn = fixedFollow != null && fixedFollow.enabled;
            active = true;
            if (orbitWasOn) orbit.enabled = false;
            if (fixedWasOn) fixedFollow.enabled = false;

            Vector3 feet = player.position;
            Vector3 forward = Vector3.ProjectOnPlane(Float - feet, Vector3.up);
            if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
            forward.Normalize();
            // If a new prop crosses the approved angle, allow another fixed view from the water.
            Vector3 towardsCamera = Quaternion.AngleAxis(15f, Vector3.up) * forward;
            Vector3 cameraPosition = Position(feet, towardsCamera);
            if (!ClearView(cameraPosition, feet))
            {
                foreach (float angle in AlternateAngles)
                {
                    Vector3 testDirection = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                    Vector3 testPosition = Position(feet, testDirection);
                    if (!ClearView(testPosition, feet)) continue;
                    towardsCamera = testDirection;
                    cameraPosition = testPosition;
                    break;
                }
            }
            Quaternion rotation = Quaternion.LookRotation(-towardsCamera, Vector3.up) *
                Quaternion.Euler(20.9f, 9.1f, 0f);
            camera.orthographic = false;
            camera.fieldOfView = ShotFov;
            camera.transform.SetPositionAndRotation(cameraPosition, rotation);
        }

        private static Vector3 Position(Vector3 feet, Vector3 towardsCamera) =>
            feet + towardsCamera * 9.1f + Vector3.up * 3.1f;

        private bool ClearView(Vector3 position, Vector3 feet) =>
            ClearRay(position, feet + Vector3.up * .18f) && ClearRay(position, feet + Vector3.up * 1.45f) &&
            ClearRay(position, Float + Vector3.up * .09f);

        private bool ClearRay(Vector3 origin, Vector3 target)
        {
            Vector3 delta = target - origin;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider != water && hit.collider.gameObject.scene == player.gameObject.scene &&
                    !hit.collider.transform.IsChildOf(player)) return false;
            return true;
        }

        internal void Restore()
        {
            if (!active) return;
            active = false;
            if (camera != null)
            {
                camera.orthographic = wasOrtho;
                camera.fieldOfView = previousFov;
                camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
            }
            if (orbitWasOn && orbit != null) orbit.enabled = true;
            if (fixedWasOn && fixedFollow != null) fixedFollow.enabled = true;
        }
    }
}
