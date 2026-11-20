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
        private bool dialogueFront;

        internal void SetDialogueFront(bool value) => dialogueFront = value;

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
            if (!dialogueFront)
            {
                Vector3 world = headingRoot != null ? headingRoot.TransformDirection(localFacing) : localFacing;
                facing = GmCardinalFacing.FromWorld(world, viewCamera, facing);
            }
            Vector2 visibleFacing = dialogueFront ? Vector2.down : facing;
            animator.SetFloat(FacingX, visibleFacing.x);
            animator.SetFloat(FacingY, visibleFacing.y);
            sprite.flipX = Mathf.Abs(visibleFacing.x) > 0.5f && (visibleFacing.x > 0f) != sideArtworkFacesRight;
        }
    }
}
