using System;
using System.Numerics;

namespace NOVR.Controllers;

public enum SpottingAimMode { Head, EyePreferred, EyeOnly }
public enum SpottingAimSource { None, Head, Eye }

internal readonly struct SpottingAimResult
{
    public readonly SpottingAimSource Source;
    public readonly Vector3 Direction;
    public bool Valid => Source != SpottingAimSource.None;
    public SpottingAimResult(SpottingAimSource source, Vector3 direction) { Source = source; Direction = direction; }
}

/// <summary>Chooses a cue source only. Never discovers targets, selects, locks or fires.</summary>
internal sealed class SpottingAimPolicy
{
    private SpottingAimSource _source;
    private Vector3 _direction;
    private int _calibration;

    public SpottingAimResult Resolve(SpottingAimMode mode, bool focused, bool headValid, bool gazeValid,
        Vector3 head, Vector3 eye, int calibration, float elapsed, float smoothing)
    {
        if (!focused || !headValid || !TryNormalize(head, out head)) { Reset(); return default; }
        var eyeValid = gazeValid && TryNormalize(eye, out eye);
        var source = mode == SpottingAimMode.Head || !eyeValid ? SpottingAimSource.Head : SpottingAimSource.Eye;
        if (mode == SpottingAimMode.EyeOnly && !eyeValid) { Reset(); return default; }
        var direction = source == SpottingAimSource.Eye ? eye : head;
        if (source == SpottingAimSource.Eye && source == _source && calibration == _calibration &&
            Finite(smoothing) && smoothing > 0 && Finite(elapsed))
        {
            var alpha = 1f - (float)Math.Exp(-Math.Max(0, Math.Min(.1f, elapsed)) / smoothing);
            if (TryNormalize(Vector3.Lerp(_direction, direction, alpha), out var filtered)) direction = filtered;
        }
        _source = source; _direction = direction; _calibration = calibration;
        return new SpottingAimResult(source, direction);
    }

    public void Reset() { _source = SpottingAimSource.None; _direction = default; }
    private static bool TryNormalize(Vector3 value, out Vector3 direction)
    {
        var length = value.LengthSquared();
        if (!Finite(length) || length < .000001f) { direction = default; return false; }
        direction = value / (float)Math.Sqrt(length); return true;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
