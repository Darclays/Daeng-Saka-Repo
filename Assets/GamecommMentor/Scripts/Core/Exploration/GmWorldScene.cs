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
        private static void ResetTransfer() => pendingTransfer = null;

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

            player.Teleport(point.position);
            player.transform.rotation = point.rotation * Quaternion.Euler(0f, 180f, 0f);
            float downwardAngle = entry.cameraPitch > 0f ? entry.cameraPitch : DefaultArrivalPitch;
            orbitCamera.SetArrivalView(ArrivalHeading(entry), downwardAngle);
            if (fromPortal)
            {
                transfer.placed = true;
                GmInteractor interactor = player.GetComponent<GmInteractor>();
                if (interactor != null) interactor.SuppressInteraction();
            }
        }

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
