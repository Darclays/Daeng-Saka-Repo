using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
#endif

namespace GamecommMentor.DaengSaka
{
    internal static class GmGamepadInput
    {
#if ENABLE_INPUT_SYSTEM
        private static int submitFrame = -1, cancelFrame = -1, menuFrame = -1, sampledFrame = -1;
        private static Gamepad lastPad;
        private static bool gamepadHints;

        private static Gamepad Current => Application.isFocused ? Gamepad.current : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            submitFrame = cancelFrame = menuFrame = sampledFrame = -1;
            lastPad = null;
            gamepadHints = false;
        }

        private static bool Take(ButtonControl button, ref int frame)
        {
            if (button?.wasPressedThisFrame != true || frame == Time.frameCount) return false;
            frame = Time.frameCount;
            return true;
        }

        private static Vector2 Stick(Vector2 axis) => axis.sqrMagnitude >= .04f ? axis : Vector2.zero;

        private static void SampleDevice()
        {
            if (sampledFrame == Time.frameCount) return;
            sampledFrame = Time.frameCount;
            Gamepad pad = Gamepad.current;
            if (pad != lastPad)
            {
                lastPad = pad;
                gamepadHints = pad != null;
            }
            if (!Application.isFocused || pad == null) return;
            Mouse mouse = Mouse.current;
            bool desktop = Keyboard.current?.anyKey.wasPressedThisFrame == true ||
                (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame ||
                    mouse.delta.ReadValue().sqrMagnitude > 4f));
            bool controller = pad.leftStick.ReadValue().sqrMagnitude >= .04f || pad.rightStick.ReadValue().sqrMagnitude >= .04f ||
                pad.dpad.ReadValue().sqrMagnitude > .25f || pad.buttonSouth.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame ||
                pad.rightShoulder.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame;
            if (controller) gamepadHints = true;
            else if (desktop) gamepadHints = false;
        }

        internal static bool UsingGamepad { get { SampleDevice(); return gamepadHints; } }
        internal static Vector2 Move => Stick(Current?.leftStick.ReadValue() ?? Vector2.zero);
        internal static bool SprintHeld => Current?.rightTrigger.isPressed == true;
        internal static Vector2 Look => Stick(Current?.rightStick.ReadValue() ?? Vector2.zero);
        internal static bool SubmitHeld => Current?.buttonSouth.isPressed == true;
        internal static bool TakeSubmit() => cancelFrame != Time.frameCount && menuFrame != Time.frameCount &&
            Take(Current?.buttonSouth, ref submitFrame);
        internal static bool TakeCancel() => Take(Current?.buttonEast, ref cancelFrame);
        internal static bool TakeMenu() => Take(Current?.startButton, ref menuFrame);
        internal static bool InfoPressed => Current?.buttonWest.wasPressedThisFrame == true;
        internal static bool JournalPressed => Current?.buttonNorth.wasPressedThisFrame == true;
        internal static bool PreviousTabPressed => Current?.leftShoulder.wasPressedThisFrame == true;
        internal static bool NextTabPressed => Current?.rightShoulder.wasPressedThisFrame == true;

        // uGUI owns menu navigation. A press already used by exploration or dialogue cannot also submit.
        internal static bool MaySubmitUi() => Application.isFocused &&
            (Current?.buttonSouth.wasPressedThisFrame != true || TakeSubmit());

        internal static void ConfigureUi(EventSystem events)
        {
            var move = events != null ? events.GetComponent<InputSystemUIInputModule>()?.move?.action : null;
            if (move == null) return;
            for (int i = 0; i < move.bindings.Count; i++)
            {
                string path = move.bindings[i].effectivePath;
                if (path != null && (path.Equals("<Gamepad>/rightStick", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("<Gamepad>/rightStick/", StringComparison.OrdinalIgnoreCase)))
                    move.ApplyBindingOverride(i, "");
            }
        }
#else
        internal static bool UsingGamepad => false;
        internal static Vector2 Move => Vector2.zero;
        internal static Vector2 Look => Vector2.zero;
        internal static bool SubmitHeld => false;
        internal static bool TakeSubmit() => false;
        internal static bool TakeCancel() => false;
        internal static bool TakeMenu() => false;
        internal static bool InfoPressed => false;
        internal static bool JournalPressed => false;
        internal static bool PreviousTabPressed => false;
        internal static bool NextTabPressed => false;
        internal static bool MaySubmitUi() => true;
        internal static void ConfigureUi(EventSystem events) { }
#endif

        internal static string BackLabel => UsingGamepad ? "B" : "Esc";

        internal static bool PressedBack()
        {
            if (!Application.isFocused) return false;
#if ENABLE_INPUT_SYSTEM
            return TakeCancel() || Keyboard.current?.escapeKey.wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        internal static bool PressedMenu()
        {
            if (!Application.isFocused) return false;
#if ENABLE_INPUT_SYSTEM
            return TakeMenu() || Keyboard.current?.pKey.wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(KeyCode.P);
#else
            return false;
#endif
        }

        internal static bool PressedInfo()
        {
            if (!Application.isFocused) return false;
#if ENABLE_INPUT_SYSTEM
            return InfoPressed || Keyboard.current?.xKey.wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(KeyCode.X);
#else
            return false;
#endif
        }
    }
}
