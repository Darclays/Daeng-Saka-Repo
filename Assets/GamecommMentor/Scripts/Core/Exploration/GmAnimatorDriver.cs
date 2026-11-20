using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(20)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class GmAnimatorDriver : MonoBehaviour
    {
        private static readonly int FacingX = Animator.StringToHash("FacingX");
        private static readonly int FacingY = Animator.StringToHash("FacingY");
        private static readonly int Moving = Animator.StringToHash("Moving");
        [SerializeField] private GmSakaMotor motor;
        private Animator animator;
        private Transform dialoguePartner;
        private Camera dialogueCamera;

        public void SetMotor(GmSakaMotor playerMotor) => motor = playerMotor;

        internal void SetDialogueFacing(Transform partner, Camera camera)
        {
            dialoguePartner = partner;
            dialogueCamera = camera;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (motor == null) motor = GetComponentInParent<GmSakaMotor>();
        }

        private void Update()
        {
            if (motor == null || animator == null || animator.runtimeAnimatorController == null) return;
            bool talking = dialoguePartner != null && dialogueCamera != null;
            Vector2 facing = motor.Facing;
            if (talking)
            {
                Vector3 heading = Vector3.ProjectOnPlane(dialoguePartner.position - motor.transform.position, Vector3.up);
                facing = GmCardinalFacing.FromWorld(heading, dialogueCamera, Vector2.zero, 0f);
            }
            animator.SetFloat(FacingX, facing.x);
            animator.SetFloat(FacingY, facing.y);
            animator.SetBool(Moving, !talking && motor.IsMoving);
        }
    }
}
