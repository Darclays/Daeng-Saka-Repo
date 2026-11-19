using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(GmMoveInput), typeof(GmControlGate))]
    public sealed class GmSakaMotor : MonoBehaviour
    {
        [SerializeField] private Camera movementCamera;
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float acceleration = 28f;
        [SerializeField, Min(0.1f)] private float deceleration = 36f;
        [SerializeField, Min(0.1f)] private float gravity = 25f;
        [Tooltip("Local heading at spawn. The visual can face the camera independently.")]
        [SerializeField] private Vector3 initialLocalFacing = Vector3.back;
        [SerializeField, Range(0f, 10f)] private float directionHysteresis = 3f;
        private CharacterController controller;
        private GmMoveInput input;
        private GmControlGate gate;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        private Object stepOwner;
        private Vector3 stepTarget;

        // A modal interaction can guide one short walk while it owns the exploration gate.
        internal void BeginInteractionStep(Object owner, Vector3 target)
        {
            stepOwner = owner;
            stepTarget = target;
            horizontalVelocity = Vector3.zero;
        }

        internal void EndInteractionStep(Object owner)
        {
            if (!ReferenceEquals(stepOwner, owner)) return;
            stepOwner = null;
            horizontalVelocity = Vector3.zero;
            IsMoving = false;
        }

        public Vector2 Facing { get; private set; } = Vector2.down;
        public Vector3 WorldFacing { get; private set; } = Vector3.back;
        public bool IsMoving { get; private set; }

        public void SetCamera(Camera viewCamera) => movementCamera = viewCamera;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<GmMoveInput>();
            gate = GetComponent<GmControlGate>();
            if (movementCamera == null) movementCamera = Camera.main;
            WorldFacing = Vector3.ProjectOnPlane(transform.TransformDirection(initialLocalFacing), Vector3.up);
            WorldFacing = WorldFacing.sqrMagnitude > 0.0001f ? WorldFacing.normalized : Vector3.back;
            Facing = GmCardinalFacing.FromWorld(WorldFacing, movementCamera, Facing, 0f);
        }

        private void OnDisable()
        {
            stepOwner = null;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            IsMoving = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || controller == null || !controller.enabled) return;

            if (stepOwner != null && !gate.CanExplore)
            {
                Vector3 remaining = Vector3.ProjectOnPlane(stepTarget - transform.position, Vector3.up);
                if (remaining.sqrMagnitude > .0004f)
                {
                    WorldFacing = remaining.normalized;
                    Facing = GmCardinalFacing.FromWorld(WorldFacing, movementCamera, Facing, directionHysteresis);
                }
                MoveOnGround(Vector3.ClampMagnitude(remaining, moveSpeed * dt), dt);
                return;
            }

            Vector2 move = gate.CanExplore ? input.ReadMove() : Vector2.zero;
            Vector3 forward = movementCamera != null
                ? Vector3.ProjectOnPlane(movementCamera.transform.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = right * move.x + forward * move.y;
            bool hasMove = direction.sqrMagnitude > 0.001f;
            if (hasMove) WorldFacing = direction.normalized;

            // Equal diagonals use the vertical view regardless of which key was pressed first.
            if (hasMove && Mathf.Approximately(Mathf.Abs(move.x), Mathf.Abs(move.y)))
                Facing = new Vector2(0f, Mathf.Sign(move.y));
            else
                Facing = GmCardinalFacing.FromWorld(WorldFacing, movementCamera, Facing, directionHysteresis);
            Vector3 desired = direction * moveSpeed;
            horizontalVelocity = gate.CanExplore
                ? Vector3.MoveTowards(horizontalVelocity, desired,
                    (move.sqrMagnitude > 0.001f ? acceleration : deceleration) * dt)
                : Vector3.zero;

            MoveOnGround(horizontalVelocity * dt, dt);
        }

        private void MoveOnGround(Vector3 planarTravel, float dt)
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity = Mathf.Max(verticalVelocity - gravity * dt, -50f);
            Vector3 previous = transform.position;
            controller.Move(planarTravel + Vector3.up * (verticalVelocity * dt));
            IsMoving = Vector3.ProjectOnPlane(transform.position - previous, Vector3.up).sqrMagnitude
                > 0.0004f * dt * dt;
        }

        public void Teleport(Vector3 position)
        {
            stepOwner = null;
            if (controller == null) controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = wasEnabled;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            IsMoving = false;
        }
    }
}
