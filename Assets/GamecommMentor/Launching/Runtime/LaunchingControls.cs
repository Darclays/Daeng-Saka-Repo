using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GamecommMentor.DaengSaka.Launching
{
    internal sealed class LaunchingControls
    {
        internal sealed class Hold
        {
            internal float Progress { get; private set; }
            internal bool Ready { get; private set; }
            internal bool Charging => owner != null;
            internal double ReadyAt { get; private set; }
            private readonly double seconds;
            private ButtonControl owner;
            private double started;

            internal Hold(double seconds) => this.seconds = seconds;

            internal void Start(ButtonControl button, double now)
            {
                if (Ready || owner != null) return;
                owner = button;
                started = now;
            }

            internal void Tick(double now)
            {
                if (owner == null) return;
                if (!owner.device.added || !owner.device.enabled || !owner.isPressed || owner.wasReleasedThisFrame)
                {
                    Cancel();
                    return;
                }
                if (now - started >= seconds) Complete(now);
                else Progress = (float)((now - started) / seconds);
            }

            internal void Complete(double now)
            {
                if (Ready) return;
                owner = null;
                Ready = true;
                Progress = 1f;
                ReadyAt = now;
            }

            internal void Cancel(InputDevice device = null)
            {
                if (Ready || (device != null && owner?.device != device)) return;
                owner = null;
                Progress = 0f;
            }

            internal void Reset()
            {
                Ready = false;
                ReadyAt = 0;
                Cancel();
            }
        }

        internal readonly Hold[] Heroes = { new Hold(10), new Hold(10), new Hold(10) };
        internal readonly Hold ResetButton = new Hold(2);
        internal readonly Hold OverrideButton = new Hold(2);
        private readonly HashSet<ButtonControl> armed = new HashSet<ButtonControl>();
        private bool focused;

        internal void SetFocus(bool value)
        {
            focused = value;
            armed.Clear();
            foreach (Hold hero in Heroes) hero.Cancel();
            ResetButton.Reset();
            OverrideButton.Reset();
        }

        internal void Reset(bool hasFocus)
        {
            foreach (Hold hero in Heroes) hero.Reset();
            SetFocus(hasFocus);
        }

        internal void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disabled:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Enabled:
                case InputDeviceChange.SoftReset:
                case InputDeviceChange.HardReset:
                    armed.RemoveWhere(button => button.device == device);
                    foreach (Hold hero in Heroes) hero.Cancel(device);
                    ResetButton.Cancel(device);
                    OverrideButton.Cancel(device);
                    break;
            }
        }

        internal void Sample(double now, bool hasFocus, bool acceptHeroes)
        {
            if (hasFocus != focused) SetFocus(hasFocus);
            if (!focused) return;

            ResetButton.Tick(now);
            if (acceptHeroes)
            {
                OverrideButton.Tick(now);
                foreach (Hold hero in Heroes) hero.Tick(now);
                foreach (Gamepad pad in Gamepad.all)
                {
                    Observe(pad.buttonWest, Heroes[0], now);
                    Observe(pad.buttonNorth, Heroes[1], now);
                    Observe(pad.buttonEast, Heroes[2], now);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            Observe(keyboard.f5Key, ResetButton, now);
            if (!acceptHeroes) return;
            Observe(keyboard.f8Key, OverrideButton, now);
            Observe(keyboard.f1Key, Heroes[0], now);
            Observe(keyboard.f2Key, Heroes[1], now);
            Observe(keyboard.f3Key, Heroes[2], now);
        }

        private void Observe(ButtonControl button, Hold hold, double now)
        {
            if (!button.device.added || !button.device.enabled) return;
            if (!button.isPressed) armed.Add(button);
            else if (armed.Remove(button)) hold.Start(button, now);
        }
    }
}
