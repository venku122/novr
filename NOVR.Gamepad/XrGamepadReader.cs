using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

namespace NOVR.Gamepad
{
    internal sealed class XrGamepadReader
    {
        private HandControls? _left, _right;
        public void Reset() { _left = _right = null; }
        public HandInput Read(bool left)
        {
            var cache = left ? _left : _right;
            if (cache == null || !cache.Device.added)
            {
                cache = Find(left);
                if (left) _left = cache; else _right = cache;
            }
            return cache?.Read() ?? default;
        }
        private static HandControls? Find(bool left)
        {
            var usage = left ? CommonUsages.LeftHand : CommonUsages.RightHand;
            foreach (var device in InputSystem.devices)
            {
                if (!(device is XRController)) continue;
                bool frame = device.layout.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0;
                bool touch = device.layout.IndexOf("Touch", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!frame && !touch) continue;
                foreach (var u in device.usages) if (u == usage) return new HandControls(device, left, frame);
            }
            return null;
        }
        private sealed class HandControls
        {
            public readonly InputDevice Device;
            private readonly bool _left, _frame;
            private readonly Vector2Control? _stick;
            private readonly AxisControl? _trigger;
            private readonly ButtonControl? _tracked, _shoulder, _menu, _click, _bottom, _outside, _top, _inside;
            private readonly IntegerControl? _tracking;
            public HandControls(InputDevice device, bool left, bool frame)
            {
                Device = device; _left = left; _frame = frame;
                _stick = Control<Vector2Control>("thumbstick", "primary2DAxis");
                _trigger = Control<AxisControl>("trigger");
                _tracked = Control<ButtonControl>("isTracked", "devicePose/isTracked");
                _tracking = Control<IntegerControl>("trackingState", "devicePose/trackingState");
                _shoulder = Control<ButtonControl>("shoulder", "shoulderButton");
                // Grips deliberately remain dedicated to NOVR's UI hand switching.
                _menu = Control<ButtonControl>("menu", "menuButton");
                _click = Control<ButtonControl>("thumbstickClicked", "thumbstickClick");
                _bottom = Control<ButtonControl>("faceButtonBottom", "primaryButton");
                _outside = Control<ButtonControl>("faceButtonOutside", "secondaryButton");
                _top = Control<ButtonControl>("faceButtonTop");
                _inside = Control<ButtonControl>("faceButtonInside");
            }
            private T? Control<T>(string first, string? fallback = null) where T : InputControl
                => Device.TryGetChildControl<T>(first) ?? (fallback == null ? null : Device.TryGetChildControl<T>(fallback));
            private static bool Down(ButtonControl? control) => control != null && control.ReadValue() > .5f;
            public HandInput Read()
            {
                if (!Device.enabled || !Down(_tracked) || (_tracking != null && (_tracking.ReadValue() & 3) != 3)) return default;
                Vector2 stick = _stick?.ReadValue() ?? Vector2.zero;
                PadButtons buttons = 0;
                if (Down(_shoulder)) buttons |= _left ? PadButtons.LeftShoulder : PadButtons.RightShoulder;
                if (Down(_menu)) buttons |= _left ? PadButtons.Back : PadButtons.Start;
                if (Down(_click)) buttons |= _left ? PadButtons.LeftThumb : PadButtons.RightThumb;
                if (_frame && _left)
                {
                    if (Down(_bottom)) buttons |= PadButtons.DpadDown;
                    if (Down(_outside)) buttons |= PadButtons.DpadLeft;
                    if (Down(_top)) buttons |= PadButtons.DpadUp;
                    if (Down(_inside)) buttons |= PadButtons.DpadRight;
                }
                else
                {
                    if (Down(_bottom)) buttons |= _left ? PadButtons.X : PadButtons.A;
                    if (Down(_outside)) buttons |= _left ? PadButtons.Y : PadButtons.B;
                    if (_frame) { if (Down(_top)) buttons |= PadButtons.Y; if (Down(_inside)) buttons |= PadButtons.X; }
                }
                return new HandInput { Valid = true, StickX = stick.x, StickY = stick.y, Trigger = _trigger?.ReadValue() ?? 0, Buttons = buttons };
            }
        }
    }
}
