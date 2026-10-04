using System;
using System.Numerics;

namespace NOVR.Controllers;

/// <summary>One stable seat reference shared by translation and rotation. Tracking loss
/// freezes the last valid pose; reacquisition never changes that reference.</summary>
internal sealed class SeatedHeadPose
{
    private (byte axes, bool preserve, Vector3 offset, string reason)? _pendingAircraftRecenter;
    public bool TrackingValid { get; private set; }
    public bool HasPose { get; private set; }
    public bool HasReference { get; private set; }
    public bool NeedsInitialReference => TrackingValid && !HasReference;
    public Vector3 RawPosition { get; private set; }
    public Quaternion RawRotation { get; private set; } = Quaternion.Identity;
    public Vector3 TranslationCalibration { get; private set; }
    public Quaternion RotationCalibration { get; set; } = Quaternion.Identity;
    public Vector3 SeatOffset { get; private set; }
    public Vector3 Anchor { get; set; }
    public int CalibrationSequence { get; private set; }
    public string CalibrationReason { get; private set; } = "not-calibrated";
    public Vector3 Position => Anchor + SeatOffset + Vector3.Transform(RawPosition + TranslationCalibration, RotationCalibration);
    public Quaternion Rotation => RotationCalibration * RawRotation;

    public void UpdateRaw(bool valid, Vector3 position, Quaternion rotation)
    {
        TrackingValid = valid && Finite(position.X) && Finite(position.Y) && Finite(position.Z) &&
            Finite(rotation.X) && Finite(rotation.Y) && Finite(rotation.Z) && Finite(rotation.W) && rotation.LengthSquared() > .0001f;
        if (!TrackingValid) return;
        RawPosition = position;
        RawRotation = Quaternion.Normalize(rotation);
        HasPose = true;
        if (_pendingAircraftRecenter is { } pending)
        {
            _pendingAircraftRecenter = null;
            RecenterTranslation(pending.axes, pending.preserve, pending.offset, pending.reason);
        }
    }

    public bool RecenterTranslation(byte axes, bool preserveOtherAxes, Vector3 seatOffset, string reason)
    {
        if (!TrackingValid)
        {
            // An explicit aircraft transition remains pending; ordinary tracking
            // reacquisition never changes an established reference.
            if (reason == "aircraft-change") _pendingAircraftRecenter = (axes, preserveOtherAxes, seatOffset, reason);
            return false;
        }
        _pendingAircraftRecenter = null;
        TranslationCalibration = new Vector3(
            (axes & 1) != 0 ? -RawPosition.X : preserveOtherAxes ? TranslationCalibration.X : 0,
            (axes & 2) != 0 ? -RawPosition.Y : preserveOtherAxes ? TranslationCalibration.Y : 0,
            (axes & 4) != 0 ? -RawPosition.Z : preserveOtherAxes ? TranslationCalibration.Z : 0);
        SeatOffset = seatOffset;
        HasReference = true;
        CalibrationSequence++;
        CalibrationReason = reason;
        return true;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
