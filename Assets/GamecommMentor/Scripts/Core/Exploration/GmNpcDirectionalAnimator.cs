using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(20)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class GmNpcDirectionalAnimator : MonoBehaviour
    {
        private static readonly int FacingX = Animator.StringToHash("FacingX");
        private static readonly int FacingY = Animator.StringToHash("FacingY");
        [SerializeField] private Transform headingRoot;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Vector3 localFacing = Vector3.back;
        [SerializeField] private bool sideArtworkFacesRight = true;
        private Vector2 facing = Vector2.down;
        private Animator animator;
        private SpriteRenderer sprite;

        public void Configure(Transform worldHeadingRoot, Camera cameraToUse)
        {
            headingRoot = worldHeadingRoot;
            viewCamera = cameraToUse;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            sprite = GetComponent<SpriteRenderer>();
            if (headingRoot == null) headingRoot = transform.parent;
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            Vector3 world = headingRoot != null ? headingRoot.TransformDirection(localFacing) : localFacing;
            facing = GmCardinalFacing.FromWorld(world, viewCamera, facing);
            animator.SetFloat(FacingX, facing.x);
            animator.SetFloat(FacingY, facing.y);
            sprite.flipX = Mathf.Abs(facing.x) > 0.5f && (facing.x > 0f) != sideArtworkFacesRight;
        }
    }
}
