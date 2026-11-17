using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    // Apply yaw before the motor reads its camera-relative movement basis.
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GmOrbitFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private GmControlGate controlGate;
        [SerializeField] private float yaw;
        [SerializeField, Range(10f, 60f)] private float pitch = 30.4f;
        [SerializeField, Min(0.5f)] private float distance = 9.28f;
        [SerializeField, Min(0.1f)] private float focusHeight = 0.8f;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.08f;
        [Tooltip("Degrees per mouse pixel. Mouse delta is already a per-frame displacement.")]
        [SerializeField, Range(0.01f, 1f)] private float horizontalSensitivity = 0.08f;
        [SerializeField] private bool invertHorizontal;
        [Tooltip("Short rotation smoothing in seconds. Zero applies mouse input directly.")]
        [SerializeField, Range(0f, 0.15f)] private float rotationSmoothTime = 0.04f;
        [Header("Vertical Orbit")]
        [SerializeField] private bool allowVerticalOrbit;
        [Tooltip("Degrees above and below the starting pitch. Pitch stays within 10 to 60 degrees.")]
        [SerializeField, Range(0f, 25f)] private float verticalRange = 12f;
        [SerializeField, Range(0.01f, 1f)] private float verticalSensitivity = 0.05f;
        [SerializeField] private bool invertVertical;
        [Header("Obstacles (3D colliders)")]
        [SerializeField] private LayerMask obstructionLayers = ~0;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.18f;
        [SerializeField, Min(0.01f)] private float collisionPadding = 0.05f;
        [SerializeField, Min(0f)] private float returnSmoothTime = 0.18f;
        [SerializeField] private Transform[] ignoredRoots = new Transform[0];
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] overlaps = new Collider[32];
        private Camera viewCamera;
        private Vector3 focus;
        private Vector3 focusVelocity;
        private float currentDistance;
        private float distanceVelocity;
        private float orbitPitch;
        private float requestedYaw;
        private float requestedPitch;
        private bool initialized;
        private bool captured;
        private CursorLockMode previousLock;
        private bool previousVisible;

        public Transform Target => target;
        public float CurrentDistance => currentDistance;

        public void SetTarget(Transform followTarget, GmControlGate gate)
        {
            ReleaseCursor();
            target = followTarget;
            controlGate = gate;
            initialized = false;
        }

        public void ConfigureView(float heading, float downwardAngle, float followDistance, float height)
        {
            SetStartingHeading(heading);
            pitch = Mathf.Clamp(downwardAngle, 10f, 60f);
            orbitPitch = pitch;
            requestedPitch = pitch;
            distance = Mathf.Max(0.5f, followDistance);
            focusHeight = Mathf.Max(0.1f, height);
        }

        public void SetStartingHeading(float heading)
        {
            yaw = Mathf.Repeat(heading, 360f);
            requestedYaw = yaw;
            initialized = false;
        }

        public void SetVerticalOrbit(bool enabled) => allowVerticalOrbit = enabled;

        public void SetIgnoredRoots(Transform[] roots) => ignoredRoots = roots ?? new Transform[0];

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            orbitPitch = pitch;
            if (controlGate == null && target != null) controlGate = target.GetComponent<GmControlGate>();
        }

        private void OnEnable()
        {
            initialized = false;
        }

        private void Start()
        {
            GmFixedFollowCamera fixedFollow = GetComponent<GmFixedFollowCamera>();
            if (fixedFollow != null && fixedFollow.enabled)
            {
                Debug.LogError("[Daeng Saka] Disable GmFixedFollowCamera before enabling horizontal orbit.", this);
                enabled = false;
                return;
            }
            SnapToTarget();
        }

        private bool CanRotate => target != null && controlGate != null && controlGate.CanExplore
            && Application.isFocused && Time.deltaTime > 0f;

        private Quaternion OrbitRotation => Quaternion.Euler(allowVerticalOrbit ? orbitPitch : pitch, yaw, 0f);

        private void Update()
        {
            if (!allowVerticalOrbit) orbitPitch = pitch;
            if (!CanRotate || CancelPressed())
            {
                ReleaseCursor();
                return;
            }
            if (captured)
            {
                if (!OrbitHeld())
                {
                    ReleaseCursor();
                    return;
                }
                UpdateOrbit(ReadLookDelta());
            }
            else if (OrbitPressed() && !PointerIsOverUi())
            {
                requestedYaw = yaw;
                requestedPitch = orbitPitch;
                previousLock = Cursor.lockState;
                previousVisible = Cursor.visible;
                captured = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // Ignore accumulated movement on the capture frame, avoiding a sudden jump.
            }
            transform.rotation = OrbitRotation;
        }

        private void UpdateOrbit(Vector2 delta)
        {
            requestedYaw += delta.x * horizontalSensitivity * (invertHorizontal ? -1f : 1f);
            float blend = rotationSmoothTime > 0f
                ? 1f - Mathf.Exp(-Time.deltaTime / rotationSmoothTime) : 1f;

            float smoothedYaw = Mathf.Lerp(yaw, requestedYaw, blend);
            yaw = Mathf.Repeat(smoothedYaw, 360f);
            // Rebase both angles together so crossing 0/360 keeps the direction of the drag.
            requestedYaw += yaw - smoothedYaw;

            if (!allowVerticalOrbit)
            {
                requestedPitch = pitch;
                return;
            }

            float minPitch = Mathf.Max(10f, pitch - verticalRange);
            float maxPitch = Mathf.Min(60f, pitch + verticalRange);
            float verticalDelta = delta.y * verticalSensitivity * (invertVertical ? 1f : -1f);
            requestedPitch = Mathf.Clamp(requestedPitch + verticalDelta, minPitch, maxPitch);
            orbitPitch = Mathf.Lerp(Mathf.Clamp(orbitPitch, minPitch, maxPitch), requestedPitch, blend);
        }

        private void LateUpdate()
        {
            // E can start dialogue after this camera's Update. Release the mouse in the same frame.
            if (!CanRotate) ReleaseCursor();
            if (target == null || Time.deltaTime <= 0f) return;
            if (!initialized) { SnapToTarget(); return; }
            Vector3 desiredFocus = target.position + Vector3.up * focusHeight;
            focus = followSmoothTime <= 0f ? desiredFocus : Vector3.SmoothDamp(focus, desiredFocus,
                ref focusVelocity, followSmoothTime, Mathf.Infinity, Time.deltaTime);
            float radius = EffectiveRadius();
            if (HasBlocker(focus, radius))
            {
                // A damped pivot can lag across a wall after a fast turn. Prefer the real actor pivot.
                focus = desiredFocus;
                focusVelocity = Vector3.zero;
            }
            Quaternion rotation = OrbitRotation;
            Vector3 backward = -(rotation * Vector3.forward);
            float allowed = FindAllowedDistance(focus, backward, radius);
            if (allowed <= currentDistance)
            {
                // Contract immediately so smoothing cannot put the camera through a wall.
                currentDistance = allowed;
                distanceVelocity = 0f;
            }
            else currentDistance = returnSmoothTime <= 0f ? allowed
                : Mathf.SmoothDamp(currentDistance, allowed, ref distanceVelocity,
                    returnSmoothTime, Mathf.Infinity, Time.deltaTime);
            currentDistance = Mathf.Clamp(currentDistance, 0f, allowed);
            transform.SetPositionAndRotation(focus + backward * currentDistance, rotation);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            if (!Application.isPlaying) orbitPitch = pitch;
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
            if (controlGate == null) controlGate = target.GetComponent<GmControlGate>();
            focus = target.position + Vector3.up * focusHeight;
            focusVelocity = Vector3.zero;
            distanceVelocity = 0f;
            Quaternion rotation = OrbitRotation;
            Vector3 backward = -(rotation * Vector3.forward);
            currentDistance = FindAllowedDistance(focus, backward, EffectiveRadius());
            transform.SetPositionAndRotation(focus + backward * currentDistance, rotation);
            initialized = true;
        }

        private float EffectiveRadius()
        {
            if (viewCamera == null) return collisionRadius;
            float halfHeight = viewCamera.orthographic ? viewCamera.orthographicSize
                : viewCamera.nearClipPlane * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * viewCamera.aspect;
            // Enclose the near-plane corners as well as the camera centre.
            return Mathf.Max(collisionRadius, Mathf.Sqrt(halfHeight * halfHeight + halfWidth * halfWidth
                + viewCamera.nearClipPlane * viewCamera.nearClipPlane));
        }

        private float FindAllowedDistance(Vector3 origin, Vector3 direction, float radius)
        {
            // Check the starting volume explicitly, including a damped pivot inside geometry.
            if (HasBlocker(origin, radius)) return 0f;
            int count = Physics.SphereCastNonAlloc(origin, radius, direction, hits, distance,
                obstructionLayers, QueryTriggerInteraction.Ignore);
            float allowed = distance;
            if (count == hits.Length)
            {
                // A saturated non-alloc buffer may omit the nearest hit. Retry only in this rare case.
                RaycastHit[] all = Physics.SphereCastAll(origin, radius, direction, distance,
                    obstructionLayers, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in all)
                    if (!Ignore(hit.collider)) allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - collisionPadding));
            }
            else for (int i = 0; i < count; i++)
                if (!Ignore(hits[i].collider)) allowed = Mathf.Min(allowed, Mathf.Max(0f, hits[i].distance - collisionPadding));
            return allowed;
        }

        private bool HasBlocker(Vector3 position, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, overlaps, obstructionLayers,
                QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return true;
            for (int i = 0; i < count; i++)
                if (!Ignore(overlaps[i])) return true;
            return false;
        }

        private bool Ignore(Collider collider)
        {
            if (collider == null) return true;
            Transform hit = collider.transform;
            if (target != null && hit.IsChildOf(target)) return true;
            if (ignoredRoots != null)
                foreach (Transform root in ignoredRoots)
                    if (root != null && hit.IsChildOf(root)) return true;
            return false;
        }

        private static bool PointerIsOverUi() => EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();

        private static bool OrbitHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButton(1);
#else
            return false;
#endif
        }

        private static bool OrbitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }

        private static bool CancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private Vector2 ReadLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            float x = UnityEngine.Input.GetAxisRaw("Mouse X") * 20f;
            float y = allowVerticalOrbit ? UnityEngine.Input.GetAxisRaw("Mouse Y") * 20f : 0f;
            return new Vector2(x, y);
#else
            return Vector2.zero;
#endif
        }

        private void ReleaseCursor()
        {
            if (!captured) return;
            captured = false;
            requestedYaw = yaw;
            requestedPitch = orbitPitch;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
        }

        private void OnDisable() => ReleaseCursor();
        private void OnDestroy() => ReleaseCursor();
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        private void OnApplicationPause(bool paused) { if (paused) ReleaseCursor(); }
    }
}
