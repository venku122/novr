namespace NOVR.Diagnostics;

internal enum CaptureTransition { None, Start, Stop }

/// <summary>Capture-only button state. Reads no hardware and consumes no flight input.</summary>
internal sealed class CaptureGesture
{
    private bool _armed;
    private bool _active;
    private double _started;

    public CaptureTransition Update(bool held, double now)
    {
        if (_active)
        {
            if (!held || now < _started || now - _started >= 30)
            {
                _active = false;
                _armed = !held;
                return CaptureTransition.Stop;
            }
            return CaptureTransition.None;
        }
        if (!held) { _armed = true; return CaptureTransition.None; }
        if (!_armed) return CaptureTransition.None;
        _active = true;
        _armed = false;
        _started = now;
        return CaptureTransition.Start;
    }

    public CaptureTransition Cancel()
    {
        var wasActive = _active;
        _active = false;
        _armed = false;
        return wasActive ? CaptureTransition.Stop : CaptureTransition.None;
    }
}
