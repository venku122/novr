using System;
using System.Numerics;
using NOVR.Controllers;

internal static class EyePoseValidationTests
{
    public static void Run()
    {
        static void Check(bool valid, string message) { if (!valid) throw new Exception(message); }
        Check(EyePoseValidation.TryValidate(true, 3, Vector3.Zero, new Quaternion(0, 0, 0, 2), out var normalized) && normalized == Quaternion.Identity, "native gaze quaternion normalized");
        Check(!EyePoseValidation.TryValidate(false, 3, Vector3.Zero, Quaternion.Identity, out _), "untracked identity is never fabricated gaze");
        Check(!EyePoseValidation.TryValidate(true, 1, Vector3.Zero, Quaternion.Identity, out _), "position-only pose fails closed");
        Check(!EyePoseValidation.TryValidate(true, 2, Vector3.Zero, Quaternion.Identity, out _), "rotation-only pose fails closed");
        Check(!EyePoseValidation.TryValidate(true, 3, new Vector3(float.NaN, 0, 0), Quaternion.Identity, out _), "non-finite gaze position rejected");
        Check(!EyePoseValidation.TryValidate(true, 3, Vector3.Zero, new Quaternion(float.PositiveInfinity, 0, 0, 1), out _), "non-finite gaze quaternion rejected");
        Check(!EyePoseValidation.TryValidate(true, 3, Vector3.Zero, default, out _), "zero gaze quaternion rejected");
        Check(!EyePoseValidation.HasValidTracking(true, false, true, true, true, 0, true, 3), "explicit untracked gaze cannot borrow a tracked device pose");
        Check(!EyePoseValidation.HasValidTracking(true, true, true, true, true, 1, true, 3), "explicit gaze flags override generic device flags");
        Check(EyePoseValidation.HasValidTracking(false, false, true, true, false, 0, true, 3), "native eye devices with only documented common flags are supported");
        Check(!EyePoseValidation.HasValidTracking(false, false, false, true, false, 0, false, 3), "missing tracking flags cannot manufacture gaze");
        Console.WriteLine("PASS: native gaze tracking flags, finite pose, normalization and invalid defaults");
    }
}
