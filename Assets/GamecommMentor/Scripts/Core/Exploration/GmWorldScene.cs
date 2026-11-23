using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    public sealed class GmWorldScene : MonoBehaviour
    {
        public const string DefaultEntry = "Start";
        public const string CaveReturnEntry = "FromGoaLembayung";
        public const string VillageReturnEntry = "FromDesaSorelia";
        private const float DefaultArrivalPitch = 18f;

        [Serializable]
        public struct Entry
        {
            public string id;
            public Transform point;
            [Tooltip("Optional camera aim point on arrival. Only its horizontal direction is used.")]
            public Transform cameraLookAt;
            [Tooltip("Downward camera angle on arrival. Use 0 for the demo default.")]
            [Range(0f, 60f)] public float cameraPitch;

            public Entry(string id, Transform point)
            {
                this.id = id;
                this.point = point;
                cameraLookAt = null;
                cameraPitch = 0f;
            }
        }

        private sealed class Transfer
        {
            public readonly string scenePath;
            public readonly string entryId;
            public bool placed;

            public Transfer(string scenePath, string entryId)
            {
                this.scenePath = scenePath;
                this.entryId = entryId;
            }
        }

        [SerializeField] private GmSakaMotor player;
        [SerializeField] private GmOrbitFollowCamera orbitCamera;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        private static Transfer pendingTransfer;
        private string arrivalEntryId = DefaultEntry;

        public void Configure(GmSakaMotor hero, GmOrbitFollowCamera camera, params Entry[] arrivalPoints)
        {
            player = hero;
            orbitCamera = camera;
            Entry[] configured = arrivalPoints == null ? Array.Empty<Entry>() : (Entry[])arrivalPoints.Clone();
            // The world setup can supply fresh spawn references without replacing authored camera settings.
            for (int i = 0; i < configured.Length; i++)
            {
                Entry old = FindEntry(configured[i].id);
                if (configured[i].cameraLookAt == null)
                    configured[i].cameraLookAt = old.cameraLookAt;
                if (configured[i].cameraPitch <= 0f)
                    configured[i].cameraPitch = old.cameraPitch;
            }
            entries = configured;
        }

        public void SetEntry(Entry entry)
        {
            entries ??= Array.Empty<Entry>();
            int index = Array.FindIndex(entries, current => current.id == entry.id);
            if (index < 0)
            {
                index = entries.Length;
                Array.Resize(ref entries, index + 1);
            }
            else
            {
                Entry old = entries[index];
                if (entry.cameraLookAt == null) entry.cameraLookAt = old.cameraLookAt;
                if (entry.cameraPitch <= 0f) entry.cameraPitch = old.cameraPitch;
            }
            entries[index] = entry;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetTransfer() => pendingTransfer = null;

        private void Awake()
        {
            Transfer transfer = pendingTransfer;
            bool fromPortal = transfer != null && transfer.scenePath == gameObject.scene.path;
            string entryId = fromPortal ? transfer.entryId : DefaultEntry;
            Entry entry = FindEntry(entryId);
            if (entry.point == null && fromPortal)
            {
                Debug.LogError($"[Daeng Saka] Titik masuk '{entryId}' tidak ada di {gameObject.scene.name}.", this);
                entry = FindEntry(DefaultEntry);
            }
            Transform point = entry.point;
            if (point == null || player == null || orbitCamera == null)
            {
                Debug.LogError("[Daeng Saka] Lengkapi titik masuk, Saka dan kamera di GmWorldScene.", this);
                return;
            }

            arrivalEntryId = entry.id;
            PlacePlayer(entry, point.position);
            if (fromPortal)
            {
                transfer.placed = true;
                GmInteractor interactor = player.GetComponent<GmInteractor>();
                if (interactor != null) interactor.SuppressInteraction();
            }
        }

        private void PlacePlayer(Entry entry, Vector3 position)
        {
            player.Teleport(position);
            player.transform.rotation = EntryRotation(entry);
            float downwardAngle = entry.cameraPitch > 0f ? entry.cameraPitch : DefaultArrivalPitch;
            orbitCamera.SetArrivalView(ArrivalHeading(entry), downwardAngle);
        }

        internal static bool TryUnstuck(GmInteractor requester, UnityEngine.Object pause)
        {
            if (requester == null || !requester.isActiveAndEnabled || pendingTransfer != null ||
                GmDemoScenes.IsLoading || requester.gameObject.scene != SceneManager.GetActiveScene() ||
                !requester.ControlGate.IsBlockedOnlyBy(pause)) return false;

            foreach (GameObject root in requester.gameObject.scene.GetRootGameObjects())
                if (root != null && root.TryGetComponent(out GmWorldScene world) && world.isActiveAndEnabled &&
                    world.player != null && world.player.gameObject == requester.gameObject)
                    return world.RecoverPlayer(requester);
            return false;
        }

        private bool RecoverPlayer(GmInteractor requester)
        {
            var body = player.GetComponent<CharacterController>();
            if (body == null || !body.enabled || orbitCamera == null) return false;

            Physics.SyncTransforms();
            Entry entry = FindEntry(arrivalEntryId);
            if (!TryClearLanding(entry, body, out Vector3 position))
            {
                if (entry.id == DefaultEntry) return false;
                entry = FindEntry(DefaultEntry);
                if (!TryClearLanding(entry, body, out position)) return false;
            }

            PlacePlayer(entry, position);
            orbitCamera.SnapToTarget();
            player.RefreshArrivalFacing();
            requester.SuppressInteraction();
            return true;
        }

        private bool TryClearLanding(Entry entry, CharacterController body, out Vector3 position)
        {
            position = default;
            if (entry.point == null || entry.point.gameObject.scene != gameObject.scene) return false;

            Vector3 scale = body.transform.lossyScale;
            float radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(radius, body.height * Mathf.Abs(scale.y) * .5f);
            Vector3 center = EntryRotation(entry) * Vector3.Scale(body.center, scale);
            float minNormalY = Mathf.Cos(Mathf.Min(body.slopeLimit, 75f) * Mathf.Deg2Rad);
            Vector3 marker = entry.point.position;
            float bestGround = float.NegativeInfinity;

            foreach (var hit in Physics.RaycastAll(marker + Vector3.up * .85f, Vector3.down, 2.35f, ~0,
                QueryTriggerInteraction.Ignore))
            {
                if (BlocksLanding(hit.collider, body) && hit.normal.y >= minNormalY)
                    bestGround = Mathf.Max(bestGround, hit.point.y);
            }
            if (float.IsNegativeInfinity(bestGround)) return false;
            position = new Vector3(marker.x, bestGround + halfHeight - center.y + .045f, marker.z);
            Vector3 axis = Vector3.up * (halfHeight - radius);
            return HasRoomToStand(position + center, axis, radius, body);
        }

        private bool HasRoomToStand(Vector3 center, Vector3 axis, float radius, CharacterController body)
        {
            foreach (var collider in Physics.OverlapCapsule(center - axis, center + axis, radius, ~0,
                QueryTriggerInteraction.Collide))
                if (BlocksLanding(collider, body) && (!collider.isTrigger ||
                    collider.GetComponentInParent<GmEnemyEncounter>() != null)) return false;
            return true;
        }

        private bool BlocksLanding(Collider collider, CharacterController body) =>
            collider != null && collider.gameObject.scene == gameObject.scene &&
            !collider.transform.IsChildOf(body.transform) &&
            !Physics.GetIgnoreLayerCollision(body.gameObject.layer, collider.gameObject.layer);

        private static Quaternion EntryRotation(Entry entry) => entry.point.rotation * Quaternion.Euler(0f, 180f, 0f);

        private Entry FindEntry(string id)
        {
            foreach (Entry entry in entries)
                if (entry.id == id) return entry;
            return default;
        }

        private static float ArrivalHeading(Entry entry)
        {
            if (entry.cameraLookAt == null) return entry.point.eulerAngles.y;

            Vector3 direction = Vector3.ProjectOnPlane(entry.cameraLookAt.position - entry.point.position, Vector3.up);
            return direction.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                : entry.point.eulerAngles.y;
        }

        internal bool TryTravel(string scenePath, string entryId, GmInteractor requester, GmWorldPortal portal)
        {
            if (!isActiveAndEnabled || pendingTransfer != null || player == null || requester == null ||
                requester.gameObject != player.gameObject || !requester.ControlGate.CanExplore) return false;

            int buildIndex = string.IsNullOrWhiteSpace(scenePath) ? -1 : SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (buildIndex < 0 || scenePath == gameObject.scene.path || string.IsNullOrWhiteSpace(entryId))
            {
                Debug.LogError($"[Daeng Saka] Portal '{portal.name}' perlu scene tujuan dan titik masuk yang terdaftar.", portal);
                return false;
            }

            var transfer = new Transfer(scenePath, entryId);
            GmControlGate gate = requester.ControlGate;
            pendingTransfer = transfer;
            gate.Block(portal);
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
                if (operation != null)
                {
                    operation.completed += _ => FinishTravel(transfer, gate, portal);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, portal);
            }
            FinishTravel(transfer, gate, portal);
            return false;
        }

        private static void FinishTravel(Transfer transfer, GmControlGate gate, GmWorldPortal portal)
        {
            if (pendingTransfer != transfer) return;
            pendingTransfer = null;
            if (gate != null) gate.Unblock(portal);
            if (!transfer.placed)
                Debug.LogError($"[Daeng Saka] Portal gagal menempatkan Saka di '{transfer.scenePath}'.");
        }
    }
}
