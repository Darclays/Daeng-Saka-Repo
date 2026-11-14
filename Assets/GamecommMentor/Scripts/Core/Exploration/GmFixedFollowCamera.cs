using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GmFixedFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5.5f, -8f);
        [SerializeField] private float focusHeight = 0.8f;
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;
        private Vector3 velocity;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            SnapToTarget();
        }

        private void Start() => SnapToTarget();

        public void SnapToTarget()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            transform.position = target.position + offset;
            ApplyFixedRotation();
        }

        private void LateUpdate()
        {
            if (target == null || Time.deltaTime <= 0f) return;
            Vector3 destination = target.position + offset;
            transform.position = smoothTime <= 0f ? destination
                : Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime,
                    Mathf.Infinity, Time.deltaTime);
            ApplyFixedRotation();
        }

        private void ApplyFixedRotation()
        {
            // A constant world-space direction keeps yaw and pitch locked while following.
            Vector3 direction = Vector3.up * focusHeight - offset;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
