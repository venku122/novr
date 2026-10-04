using System;
using System.Numerics;

namespace NOVR.Controllers;

internal static class EyePoseValidation
{
    public static bool HasValidTracking(bool hasGazeTracked, bool gazeTracked, bool hasDeviceTracked, bool deviceTracked,
        bool hasGazeState, uint gazeState, bool hasDeviceState, uint deviceState)
    {
        // Availability fallback is allowed; a provided negative gaze value wins.
        var tracked = hasGazeTracked ? gazeTracked : hasDeviceTracked && deviceTracked;
        var state = hasGazeState ? gazeState : hasDeviceState ? deviceState : 0;
        return tracked && (state & 3) == 3;
    }
    // A default identity pose is not gaze unless the native runtime validates
    // both translation and orientation for an actively tracked eye device.
    public static bool TryValidate(bool tracked, uint trackingState, Vector3 position, Quaternion rotation, out Quaternion normalized)
    {
        normalized = Quaternion.Identity;
        if (!tracked || (trackingState & 3) != 3 || !Finite(position.X) || !Finite(position.Y) || !Finite(position.Z)) return false;
        var length = rotation.LengthSquared();
        if (!Finite(length) || length < .0001f) return false;
        normalized = Quaternion.Normalize(rotation);
        return true;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
