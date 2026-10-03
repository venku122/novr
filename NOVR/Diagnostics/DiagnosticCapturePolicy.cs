using System;

namespace NOVR.Diagnostics;

internal enum DiagnosticMode { Off, Snapshot, Session }

/// <summary>A bounded diagnostic window, shared by all Core instances in a game process.</summary>
internal sealed class DiagnosticCapturePolicy
{
    private readonly DiagnosticMode _mode;
    private readonly double _start;
    private double _next;
    private int _count;

    public DiagnosticCapturePolicy(DiagnosticMode mode, double start)
    {
        _mode = mode;
        _start = _next = start;
    }

    public static DiagnosticMode Parse(string value) =>
        Enum.TryParse(value.Trim(), true, out DiagnosticMode mode) && Enum.IsDefined(typeof(DiagnosticMode), mode)
            ? mode : DiagnosticMode.Off;

    public bool ShouldSample(double now)
    {
        if (_mode != DiagnosticMode.Session || now < _start || now < _next || now - _start >= 60 || _count >= 600)
            return false;
        _next = now + 0.1;
        _count++;
        return true;
    }
}
