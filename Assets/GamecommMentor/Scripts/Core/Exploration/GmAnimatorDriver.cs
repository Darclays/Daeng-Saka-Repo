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

        public void SetMotor(GmSakaMotor playerMotor) => motor = playerMotor;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (motor == null) motor = GetComponentInParent<GmSakaMotor>();
        }

        private void Update()
        {
            if (motor == null || animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat(FacingX, motor.Facing.x);
            animator.SetFloat(FacingY, motor.Facing.y);
            animator.SetBool(Moving, motor.IsMoving);
        }
    }
}
