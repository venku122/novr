namespace NOVR.Controllers;

internal enum PointerHand { None, Left, Right }
internal readonly struct ControllerUiFrame
{
    public readonly PointerHand Hand;
    public readonly bool Pressed, Down, Up;
    public ControllerUiFrame(PointerHand hand, bool pressed, bool down, bool up)
    { Hand = hand; Pressed = pressed; Down = down; Up = up; }
}

/// <summary>UI-only selection. No aircraft actions or OS input.</summary>
internal sealed class ControllerUiState
{
    private PointerHand _hand;
    private bool _pressed;
    private bool _armed;

    public static PointerHand ChooseHand(bool right, bool left, string preference, PointerHand current, bool pressed)
    {
        if (pressed && ((current == PointerHand.Right && right) || (current == PointerHand.Left && left))) return current;
        if (preference == "Left") return left ? PointerHand.Left : right ? PointerHand.Right : PointerHand.None;
        return right ? PointerHand.Right : left ? PointerHand.Left : PointerHand.None;
    }

    public ControllerUiFrame Update(bool right, bool left, string preference, float rightTrigger, bool rightConfirm, float leftTrigger, bool leftConfirm)
    {
        var hand = ChooseHand(right, left, preference, _hand, _pressed);
        var up = false;
        if (hand != _hand)
        {
            up = _pressed;
            _pressed = false;
            _armed = false;
            _hand = hand;
        }
        var trigger = hand == PointerHand.Left ? leftTrigger : rightTrigger;
        var confirm = hand == PointerHand.Left ? leftConfirm : rightConfirm;
        if (float.IsNaN(trigger) || float.IsInfinity(trigger)) trigger = 0;
        var released = hand == PointerHand.None || (!confirm && trigger <= .45f);
        var down = false;
        if (released) { up |= _pressed; _pressed = false; _armed = hand != PointerHand.None; }
        else if (_armed && !_pressed && (confirm || trigger >= .55f)) { _pressed = true; down = true; }
        return new ControllerUiFrame(hand, _pressed, down, up);
    }
}

/// <summary>Focus/ray/lifecycle cancellation requires a fresh physical release before re-entry.</summary>
internal sealed class PointerPressGate
{
    private bool _blocked;
    public void Cancel(bool wasPressed) { _blocked |= wasPressed; }
    public bool Read(bool pressed)
    {
        if (!pressed) _blocked = false;
        return pressed && !_blocked;
    }
}
