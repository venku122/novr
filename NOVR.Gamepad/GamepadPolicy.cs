using System;
namespace NOVR.Gamepad
{
    [Flags] public enum PadButtons : ushort { DpadUp=1, DpadDown=2, DpadLeft=4, DpadRight=8, Start=16, Back=32, LeftThumb=64, RightThumb=128, LeftShoulder=256, RightShoulder=512, A=4096, B=8192, X=16384, Y=32768 }
    public struct HandInput { public bool Valid; public float StickX, StickY, Trigger; public PadButtons Buttons; }
    public struct PadReport
    {
        public short LeftX, LeftY, RightX, RightY; public byte LeftTrigger, RightTrigger; public PadButtons Buttons;
        public bool IsNeutral => LeftX == 0 && LeftY == 0 && RightX == 0 && RightY == 0 && LeftTrigger == 0 && RightTrigger == 0 && Buttons == 0;
    }
    public sealed class GamepadPolicy
    {
        private bool _leftGate, _rightGate, _navigationGate;
        private const PadButtons Faces = PadButtons.A | PadButtons.B | PadButtons.X | PadButtons.Y | PadButtons.Start | PadButtons.Back | PadButtons.DpadUp | PadButtons.DpadDown | PadButtons.DpadLeft | PadButtons.DpadRight;
        public void Reset() { _leftGate = _rightGate = _navigationGate = true; }
        public PadReport Map(bool focused, HandInput left, HandInput right, bool uiLeft, bool uiRight, bool navigationOwns = false)
        {
            if (!focused || !left.Valid || !right.Valid) { Reset(); return default; }
            const PadButtons menu = PadButtons.Start | PadButtons.Back;
            if (navigationOwns) _navigationGate = true;
            else if (((left.Buttons | right.Buttons) & menu) == 0) _navigationGate = false;
            if (_navigationGate) { left.Buttons &= ~menu; right.Buttons &= ~menu; }
            Filter(ref left, uiLeft, ref _leftGate);
            Filter(ref right, uiRight, ref _rightGate);
            return new PadReport { LeftX = Axis(left.StickX), LeftY = Axis(left.StickY), RightX = Axis(right.StickX), RightY = Axis(right.StickY), LeftTrigger = Trigger(left.Trigger), RightTrigger = Trigger(right.Trigger), Buttons = left.Buttons | right.Buttons };
        }
        private static void Filter(ref HandInput hand, bool ui, ref bool gate)
        {
            // Allow resting analog noise below the release threshold. This is only
            // a rearm threshold; normal gamepad trigger output remains analog.
            bool held = !float.IsNaN(hand.Trigger) && hand.Trigger >= .1f || (hand.Buttons & Faces) != 0;
            if (ui) gate = true;
            else if (!held) gate = false;
            if (gate) { hand.Trigger = 0; hand.Buttons &= ~Faces; }
        }
        private static short Axis(float value) => float.IsNaN(value) || float.IsInfinity(value) ? (short)0 : (short)Math.Round(Math.Max(-1, Math.Min(1, value)) * (value < 0 ? 32768 : 32767));
        private static byte Trigger(float value) => float.IsNaN(value) || float.IsInfinity(value) ? (byte)0 : (byte)Math.Round(Math.Max(0, Math.Min(1, value)) * 255);
    }
    public interface IGamepadSink : IDisposable { int UserIndex { get; } void Send(PadReport report); }
    public sealed class GamepadSession : IDisposable
    {
        private readonly Func<IGamepadSink> _factory;
        private IGamepadSink? _sink;
        private bool _failed;
        public bool Connected => _sink != null;
        public int UserIndex => _sink?.UserIndex ?? -1;
        public GamepadSession(Func<IGamepadSink> factory) { _factory = factory; }
        public Exception? Update(bool enabled, PadReport report)
        {
            if (!enabled) { var error = Close(); _failed = false; return error; }
            if (_failed) return null;
            try { if (_sink == null) _sink = _factory(); _sink.Send(report); return null; }
            catch (Exception error) { _failed = true; Close(); return error; }
        }
        private Exception? Close()
        {
            var sink = _sink; _sink = null;
            if (sink == null) return null;
            Exception? failure = null;
            try { sink.Send(default); } catch (Exception error) { failure = error; }
            try { sink.Dispose(); } catch (Exception error) { failure = failure ?? error; }
            return failure;
        }
        public void Dispose() { Close(); }
    }
}
