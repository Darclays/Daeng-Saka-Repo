using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmMoveInput : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        [Tooltip("Optional shared action. Its owner is responsible for enabling it.")]
        [SerializeField] private InputActionReference moveAction;
        private InputAction keyboardMove;

        private void OnEnable()
        {
            if (keyboardMove == null)
            {
                keyboardMove = new InputAction("GmMove", InputActionType.Value);
                keyboardMove.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                keyboardMove.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            }
            keyboardMove.Enable();
        }

        private void OnDisable() => keyboardMove?.Disable();
        private void OnDestroy() => keyboardMove?.Dispose();
#endif

        public Vector2 ReadMove() => ReadMove(out _);

        internal Vector2 ReadMove(out bool sprint)
        {
            sprint = false;
            if (!isActiveAndEnabled || !Application.isFocused) return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Vector2 stick = GmGamepadInput.Move;
            if (stick.sqrMagnitude > 0f)
            {
                sprint = GmGamepadInput.SprintHeld;
                return Vector2.ClampMagnitude(stick, 1f);
            }
            Keyboard keyboard = Keyboard.current;
            sprint = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            InputAction action = moveAction != null ? moveAction.action : keyboardMove;
            return action != null && action.enabled
                ? Vector2.ClampMagnitude(action.ReadValue<Vector2>(), 1f) : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            sprint = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            float x = (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                    - (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float y = (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                    - (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
            return Vector2.zero;
#endif
        }
    }
}
