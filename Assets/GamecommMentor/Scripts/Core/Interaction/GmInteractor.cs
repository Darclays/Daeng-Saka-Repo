using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(0)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmSakaMotor), typeof(GmControlGate), typeof(GmInteractInput))]
    public sealed class GmInteractor : MonoBehaviour
    {
        [SerializeField] private Camera movementCamera;
        [SerializeField, Min(0.1f)] private float range = 1.8f;
        [SerializeField, Range(-1f, 1f)] private float minimumFacingDot = -0.1f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private LayerMask obstructionLayers = ~0;
        private readonly Collider[] overlaps = new Collider[64];
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly HashSet<GmInteractable> considered = new HashSet<GmInteractable>();
        private GmSakaMotor motor;
        private GmControlGate gate;
        private GmInteractInput input;
        private float nextScanTime;
        private float inputAllowedTime;
        private int inputAllowedFrame;

        public GmInteractable CurrentTarget { get; private set; }
        public GmControlGate ControlGate => gate != null ? gate : (gate = GetComponent<GmControlGate>());
        public void SetCamera(Camera viewCamera) => movementCamera = viewCamera;

        private void Awake()
        {
            motor = GetComponent<GmSakaMotor>();
            gate = GetComponent<GmControlGate>();
            input = GetComponent<GmInteractInput>();
            if (movementCamera == null) movementCamera = Camera.main;
        }

        private void Start()
        {
            GmGamepadInput.ConfigureUi(EventSystem.current);
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                foreach (GmQuestJournal journal in root.GetComponentsInChildren<GmQuestJournal>())
                    if (journal.isActiveAndEnabled && journal.Interactor == this) return;
            if (GetComponent<GmExplorationPause>() == null) gameObject.AddComponent<GmExplorationPause>();
        }

        private void OnDisable() => CurrentTarget = null;

        public void SuppressInteraction(float seconds = 0.15f)
        {
            inputAllowedTime = Mathf.Max(inputAllowedTime, Time.unscaledTime + Mathf.Max(0f, seconds));
            inputAllowedFrame = Mathf.Max(inputAllowedFrame, Time.frameCount + 1);
            nextScanTime = 0f;
        }

        private void Update()
        {
            if (!gate.CanExplore)
            {
                CurrentTarget = null;
                return;
            }
            bool pressed = input.WasPressedThisFrame();
            if (pressed || Time.unscaledTime >= nextScanTime)
            {
                FindTarget();
                nextScanTime = Time.unscaledTime + 0.075f;
            }
            if (!pressed || Time.frameCount < inputAllowedFrame || Time.unscaledTime < inputAllowedTime) return;
            GmInteractable target = CurrentTarget;
            if (target == null || !IsAvailable(target)) return;
            SuppressInteraction();
            target.TryInteract(this);
        }

        private void FindTarget()
        {
            GmInteractable previous = CurrentTarget;
            CurrentTarget = null;
            considered.Clear();
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            int count = Physics.OverlapSphereNonAlloc(origin, range, overlaps, targetLayers,
                QueryTriggerInteraction.Collide);
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                GmInteractable candidate = overlaps[i].GetComponentInParent<GmInteractable>();
                if (candidate == null || !considered.Add(candidate) || !IsAvailable(candidate)) continue;
                Vector3 delta = Vector3.ProjectOnPlane(InteractionPoint(candidate, origin) - origin, Vector3.up);
                float score = delta.sqrMagnitude;
                if (score > bestScore + 0.0001f) continue;
                if (Mathf.Abs(score - bestScore) <= 0.0001f && CurrentTarget == previous) continue;
                bestScore = score;
                CurrentTarget = candidate;
            }
        }

        private bool IsAvailable(GmInteractable target)
        {
            if (target == null || !target.CanInteract(this)) return false;
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 delta = InteractionPoint(target, origin) - origin;
            Vector3 planar = Vector3.ProjectOnPlane(delta, Vector3.up);
            var shopReach = target.GetComponent<GmShopInteractionReach>();
            bool atShop = shopReach != null && shopReach.Contains(origin);
            if (!atShop && planar.sqrMagnitude > range * range) return false;
            // Interaction follows the character's heading, not the camera or a quantized sprite.
            Vector3 facing = motor.WorldFacing;
            if (planar.sqrMagnitude > 0.0001f && Vector3.Dot(facing, planar.normalized) < minimumFacingDot) return false;
            if (delta.sqrMagnitude < 0.0001f) return true;
            int count = Physics.RaycastNonAlloc(origin, delta.normalized, hits, delta.magnitude,
                obstructionLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform hit = hits[i].collider.transform;
                if (hit.IsChildOf(transform) || hit.IsChildOf(target.transform)) continue;
                if (atShop && shopReach.IsCounter(hit)) continue;
                return false;
            }
            return true;
        }

        private static Vector3 InteractionPoint(GmInteractable target, Vector3 origin) =>
            target is GmWorldPortal portal ? portal.InteractionPoint(origin) : target.FocusPosition;
    }
}
