using UnityEngine;
using NumericsVector = System.Numerics.Vector3;
using NumericsRotation = System.Numerics.Quaternion;

namespace NOVR.Controllers;

/// <summary>Maps native gaze into NOVR's calibrated UI-camera space. The actual
/// world camera, flight controls and game detection rules are never changed.</summary>
internal static class SpottingAimInput
{
    private static readonly SpottingAimPolicy Policy = new();
    private static int _filterFrame = -1;
    private static string _statusPrefix = "", _statusDetail = "";
    public static int SampleFrame => _filterFrame;
    private static Quaternion _calibrationRotation = Quaternion.identity;
    public static SpottingAimSource Source { get; private set; }
    public static string GazeStatus { get; private set; } = "unavailable";
    public static string Status { get; private set; } = "unavailable";
    public static Ray LastUiRay { get; private set; }
    public static bool Valid { get; private set; }

    public static bool TryGetUiRay(out Ray ray)
    {
        ray = default;
        var config = ModConfiguration.Instance;
        if (NOUIManagerUnavailable()) return Invalidate("ui-camera-unavailable");
        var camera = APIBus.CockpitHudCamera;
        if (camera == null) return Invalidate("ui-camera-unavailable");
        var gazeValid = EyeGazeInput.TryGetTrackingPose(out var position, out var rotation, out var gazeStatus);
        GazeStatus = gazeStatus;
        var calibrationRotation = NOVRHeadsetData.RotationCalibrationOffset;
        if (Mathf.Abs(Quaternion.Dot(_calibrationRotation, calibrationRotation)) < .999999f) Policy.Reset();
        _calibrationRotation = calibrationRotation;
        GazeSpaceMapping.ToUi(ToNumerics(NOVRHeadsetData.RawHeadPosition), ToNumerics(NOVRHeadsetData.RawHeadRotation),
            ToNumerics(position), ToNumerics(rotation), ToNumerics(camera.transform.position), ToNumerics(camera.transform.rotation),
            out var eyeOrigin, out var eyeDirection);
        // Advance filtering only once per game frame; before-render callbacks may
        // refresh head mapping without repeatedly integrating the frame's delta.
        var elapsed = _filterFrame == Time.frameCount ? 0 : Time.unscaledDeltaTime;
        _filterFrame = Time.frameCount;
        var resolved = Policy.Resolve(config.SpottingAim.Value, Application.isFocused, NOVRHeadsetData.HeadTracked,
            gazeValid, ToNumerics(camera.transform.forward), eyeDirection,
            NOVRHeadsetData.CalibrationSequence, elapsed, config.EyeAimSmoothing.Value);
        Source = resolved.Source; Valid = resolved.Valid;
        if (!Valid) { SetStatus(config.SpottingAim.Value == SpottingAimMode.EyeOnly ? "eye-required:" : "head-not-tracked-or-unfocused", config.SpottingAim.Value == SpottingAimMode.EyeOnly ? gazeStatus : ""); return false; }
        var origin = camera.transform.position;
        if (Source == SpottingAimSource.Eye)
            origin = FromNumerics(eyeOrigin);
        ray = new Ray(origin, FromNumerics(resolved.Direction));
        LastUiRay = ray;
        SetStatus(Source == SpottingAimSource.Eye ? "eye" : config.SpottingAim.Value == SpottingAimMode.Head ? "head" : "head-fallback:",
            Source == SpottingAimSource.Head && config.SpottingAim.Value != SpottingAimMode.Head ? gazeStatus : "");
        return true;
    }

    private static bool NOUIManagerUnavailable() => NOVR.VrUi.NOUIManager.I == null;
    private static bool Invalidate(string status)
    { Policy.Reset(); Source = SpottingAimSource.None; Valid = false; SetStatus(status); return false; }
    private static void SetStatus(string prefix, string detail = "")
    {
        if (_statusPrefix == prefix && _statusDetail == detail) return;
        _statusPrefix = prefix; _statusDetail = detail;
        Status = detail.Length == 0 ? prefix : prefix + detail;
    }
    private static NumericsVector ToNumerics(Vector3 value) => new(value.x, value.y, value.z);
    private static NumericsRotation ToNumerics(Quaternion value) => new(value.x, value.y, value.z, value.w);
    private static Vector3 FromNumerics(NumericsVector value) => new(value.X, value.Y, value.Z);
}
