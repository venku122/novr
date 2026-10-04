using System;
using System.Numerics;
using NOVR.Controllers;

internal static class SeatedHeadPoseTests
{
    private static void Near(Vector3 actual, Vector3 expected, string reason)
    {
        if (Vector3.Distance(actual, expected) > .0001f) throw new Exception(reason + ": " + actual);
    }
    public static void Run()
    {
        var pose = new SeatedHeadPose();
        var center = new Vector3(.6f, 1.25f, -.8f);
        var seat = new Vector3(0, 0, .08f);
        pose.UpdateRaw(true, center, Quaternion.Identity);
        if (!pose.NeedsInitialReference) throw new Exception("First validated pose needs a seat reference");
        pose.RecenterTranslation(7, false, seat, "initial");
        Near(pose.Position, seat, "Initial floor-space position removed");
        for (int i = 0; i < 10000; i++)
        {
            pose.UpdateRaw(true, center, Quaternion.CreateFromAxisAngle(Vector3.UnitY, i * .001f));
            if (pose.NeedsInitialReference) throw new Exception("Tracking/acquisition must not repeatedly recenter");
            Near(pose.Position, seat, "Rotation at fixed tracking position cannot translate the viewpoint");
        }
        if (pose.CalibrationSequence != 1) throw new Exception("Repeated valid tracking must preserve reference");
        pose.UpdateRaw(false, new Vector3(0, 1.6f, 0), Quaternion.Identity);
        Near(pose.Position, seat, "Untracked placeholder cannot move the seated view");
        pose.UpdateRaw(true, center + Vector3.UnitX * .1f, Quaternion.Identity);
        Near(pose.Position, seat + Vector3.UnitX * .1f, "Reacquisition preserves reference and physical lean");
        pose.RotationCalibration = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -MathF.PI / 2);
        Near(pose.Position, seat + Vector3.UnitZ * .1f, "Position and orientation share the recentered coordinate system");
        pose.UpdateRaw(true, center, Quaternion.Identity);
        Near(pose.Position, seat, "Return to neutral restores original seat after yaw recenter");
        pose.RecenterTranslation(7, false, seat, "manual");
        if (pose.CalibrationSequence != 2 || pose.CalibrationReason != "manual") throw new Exception("Explicit recenter remains available");
        pose.UpdateRaw(false, Vector3.Zero, Quaternion.Identity);
        if (pose.RecenterTranslation(7, false, seat, "untracked")) throw new Exception("Cannot calibrate invalid tracking");
        pose.UpdateRaw(true, new Vector3(float.NaN, 0, 0), Quaternion.Identity);
        Near(pose.Position, seat, "Nonfinite sample cannot poison camera transforms");
        pose.RecenterTranslation(7, false, new Vector3(.02f, 0, .12f), "aircraft-change");
        if (pose.CalibrationSequence != 2) throw new Exception("Aircraft recenter must wait for tracked pose");
        pose.UpdateRaw(true, center + Vector3.UnitX, Quaternion.Identity);
        Near(pose.Position, new Vector3(.02f, 0, .12f), "Pending aircraft recenter applies new seat offset once");
        if (pose.CalibrationSequence != 3 || pose.CalibrationReason != "aircraft-change") throw new Exception("Pending explicit aircraft recenter applied once");
        pose.UpdateRaw(true, center + Vector3.UnitX, Quaternion.Identity);
        if (pose.CalibrationSequence != 3) throw new Exception("Pending recenter must not repeat");
        Console.WriteLine("PASS: fixed-position yaw, 10000 reacquisition updates, invalid-pose freeze, seated lean/return, shared yaw space and explicit recenter");
    }
}
