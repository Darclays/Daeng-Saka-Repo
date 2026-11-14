using UnityEngine;
#if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT
using UnityEngine.InputSystem;
using PixelCrushers;
#endif

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class GmPixelCrushersInputBridge : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT
        private InputAction submit;
        private InputAction cancel;

        private void OnEnable()
        {
            if (!InputDeviceManager.inputActionDict.ContainsKey("Submit"))
            {
                submit = new InputAction("Submit", InputActionType.Button, "<Keyboard>/enter");
                submit.AddBinding("<Keyboard>/space");
                InputDeviceManager.RegisterInputAction("Submit", submit);
            }
            if (!InputDeviceManager.inputActionDict.ContainsKey("Cancel"))
            {
                cancel = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
                InputDeviceManager.RegisterInputAction("Cancel", cancel);
            }
        }

        private void OnDisable()
        {
            Release("Submit", ref submit);
            Release("Cancel", ref cancel);
        }

        private static void Release(string name, ref InputAction action)
        {
            if (action == null) return;
            if (InputDeviceManager.inputActionDict.TryGetValue(name, out InputAction registered) && registered == action)
                InputDeviceManager.UnregisterInputAction(name);
            action.Disable();
            action.Dispose();
            action = null;
        }
#endif
    }
}
