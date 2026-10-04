using System;
using System.Numerics;
using NOVR.Controllers;

internal static class SpottingAimTests
{
    public static void Run()
    {
        var aim = new SpottingAimPolicy();
        var head = Vector3.UnitZ;
        var eye = Vector3.Normalize(new Vector3(.3f, .1f, 1));
        var result = aim.Resolve(SpottingAimMode.EyePreferred, true, true, true, head, eye, 1, .01f, 0);
        Check(result.Source == SpottingAimSource.Eye && Near(result.Direction, eye), "valid gaze drives cue");
        result = aim.Resolve(SpottingAimMode.EyePreferred, true, true, false, head, eye, 1, .01f, .04f);
        Check(result.Source == SpottingAimSource.Head && Near(result.Direction, head), "invalid gaze immediately falls back to head");
        result = aim.Resolve(SpottingAimMode.EyeOnly, true, true, false, head, eye, 1, .01f, .04f);
        Check(!result.Valid, "strict eye mode blocks selection with unavailable gaze");
        Check(!aim.Resolve(SpottingAimMode.EyePreferred, false, true, true, head, eye, 1, .01f, 0).Valid, "focus loss clears cue");
        Check(!aim.Resolve(SpottingAimMode.EyePreferred, true, false, true, head, eye, 1, .01f, 0).Valid, "eye ray requires validated head reference");
        result = aim.Resolve(SpottingAimMode.EyePreferred, true, true, true, head, new Vector3(float.NaN, 0, 1), 1, .01f, 0);
        Check(result.Source == SpottingAimSource.Head, "nonfinite eye direction rejected");
        aim.Resolve(SpottingAimMode.EyePreferred, true, true, true, head, eye, 1, .01f, 0);
        var other = Vector3.Normalize(new Vector3(-.3f, 0, 1));
        result = aim.Resolve(SpottingAimMode.EyePreferred, true, true, true, head, other, 1, .01f, .04f);
        Check(!Near(result.Direction, other) && Vector3.Dot(result.Direction, other) > 0, "bounded filtering smooths valid same-source gaze");
        result = aim.Resolve(SpottingAimMode.EyePreferred, true, true, true, head, eye, 2, .01f, .04f);
        Check(Near(result.Direction, eye), "recenter resets filter into new coordinates");
        result = aim.Resolve(SpottingAimMode.Head, true, true, true, head, eye, 2, .01f, .04f);
        Check(result.Source == SpottingAimSource.Head && Near(result.Direction, head), "head mode ignores eyes and removes filter lag");
        var rawHeadRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
        var rawHeadPosition = new Vector3(.6f, 1.25f, -.8f);
        var relativeEye = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 18);
        var eyePosition = rawHeadPosition + Vector3.Transform(new Vector3(.03f, 0, 0), rawHeadRotation);
        GazeSpaceMapping.ToUi(rawHeadPosition, rawHeadRotation, eyePosition, rawHeadRotation * relativeEye,
            new Vector3(0, 0, .08f), Quaternion.Identity, out var origin, out var direction);
        Check(Near(origin, new Vector3(.03f, 0, .08f)), "eye origin uses calibrated head reference without floor offset or double rotation");
        Check(Near(direction, Vector3.Transform(Vector3.UnitZ, relativeEye)), "90 degree recenter preserves 10 degree eye offset");
        GazeSpaceMapping.ToUi(rawHeadPosition + Vector3.UnitX, rawHeadRotation, eyePosition + Vector3.UnitX,
            rawHeadRotation * relativeEye, new Vector3(0, 0, .18f), Quaternion.Identity, out origin, out direction);
        Check(Near(origin, new Vector3(.03f, 0, .18f)), "physical lean contributes once through calibrated camera");
        Console.WriteLine("PASS: gaze/head source selection, strict invalid cancellation, focus/tracking/finite validation and filter/calibration reset");
    }
    private static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < .0001f;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
