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

        [Serializable]
        public struct Entry
        {
            public string id;
            public Transform point;

            public Entry(string id, Transform point)
            {
                this.id = id;
                this.point = point;
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
            entries = arrivalPoints;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransfer() => pendingTransfer = null;

        private void Awake()
        {
            Transfer transfer = pendingTransfer;
            bool fromPortal = transfer != null && transfer.scenePath == gameObject.scene.path;
            string entryId = fromPortal ? transfer.entryId : DefaultEntry;
            Transform point = FindEntry(entryId);
            if (point == null && fromPortal)
            {
                Debug.LogError($"[Daeng Saka] Titik masuk '{entryId}' tidak ada di {gameObject.scene.name}.", this);
                point = FindEntry(DefaultEntry);
            }
            if (point == null || player == null || orbitCamera == null)
            {
                Debug.LogError("[Daeng Saka] Lengkapi titik masuk, Saka dan kamera di GmWorldScene.", this);
                return;
            }

            player.Teleport(point.position);
            player.transform.rotation = point.rotation * Quaternion.Euler(0f, 180f, 0f);
            orbitCamera.SetStartingHeading(point.eulerAngles.y);
            if (fromPortal)
            {
                transfer.placed = true;
                GmInteractor interactor = player.GetComponent<GmInteractor>();
                if (interactor != null) interactor.SuppressInteraction();
            }
        }

        private Transform FindEntry(string id)
        {
            foreach (Entry entry in entries)
                if (entry.id == id) return entry.point;
            return null;
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
