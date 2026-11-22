using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmInteractInput : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        [Tooltip("Optional shared Button action. Its owner enables/disables it.")]
        [SerializeField] private InputActionReference interactAction;
#endif
        public bool WasPressedThisFrame()
        {
            if (!isActiveAndEnabled || !Application.isFocused) return false;
#if ENABLE_INPUT_SYSTEM
            if (GmGamepadInput.TakeSubmit()) return true;
            if (interactAction != null)
                return interactAction.action != null && interactAction.action.enabled
                    && interactAction.action.WasPressedThisFrame();
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }
    }
}
