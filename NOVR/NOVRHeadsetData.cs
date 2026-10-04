using NOVR.Controllers;
using UnityEngine;
using NumericsVector = System.Numerics.Vector3;
using NumericsRotation = System.Numerics.Quaternion;

namespace NOVR;

[DefaultExecutionOrder(-100)]
public class NOVRHeadsetData : NOVRBehaviour
{
    private static readonly SeatedHeadPose Pose = new();
    public static int CalibrationSequence => Pose.CalibrationSequence;
    public static string LastCalibrationReason => Pose.CalibrationReason;
    public static string TrackingSource => HeadPoseReader.Source;
    public static bool HeadTracked => Pose.TrackingValid;
    public static Vector3 RawHeadPosition => FromNumerics(Pose.RawPosition);
    public static Quaternion RawHeadRotation => FromNumerics(Pose.RawRotation);
    public static Vector3 TranslationAnchor => FromNumerics(Pose.Anchor);
    public static Vector3 Translation { get; private set; }
    // Raw tracking-space origin, excluding the seat offset.
    public static Vector3 TranslationCalibrationOffset => FromNumerics(Pose.TranslationCalibration);
    public static Vector3 TranslationError => RawHeadPosition;
    public static Quaternion Rotation { get; private set; } = Quaternion.identity;
    public static Quaternion RotationCalibrationOffset => FromNumerics(Pose.RotationCalibration);
    public static Quaternion RotationError => RawHeadRotation;

    public static void SetAnchor(Vector3 anchor) => Pose.Anchor = ToNumerics(anchor);

    public static void CalibrateTranslation(CalibrationAxes calibrationAxes = CalibrationAxes.All, bool overrideExistingInNonCalibratedAxes = false, string reason = "manual")
    {
        RefreshRawPose();
        var config = ModConfiguration.Instance;
        Pose.RecenterTranslation((byte)calibrationAxes, overrideExistingInNonCalibratedAxes,
            new NumericsVector(config.CockpitHeadRightOffset.Value, 0, config.CockpitHeadForwardOffset.Value), reason);
        PublishPose();
    }

    public static void CalibrateRotation(CalibrationAxes calibrationAxes = CalibrationAxes.Yaw, bool overrideExistingInNonCalibratedAxes = false)
    {
        RefreshRawPose();
        if (!Pose.TrackingValid) return;
        var currentEuler = RawHeadRotation.eulerAngles;
        var previous = RotationCalibrationOffset.eulerAngles;
        var rotation = Quaternion.Euler(
            (calibrationAxes & CalibrationAxes.Pitch) != 0 ? -Mathf.DeltaAngle(0, currentEuler.x) : overrideExistingInNonCalibratedAxes ? previous.x : 0,
            (calibrationAxes & CalibrationAxes.Yaw) != 0 ? -Mathf.DeltaAngle(0, currentEuler.y) : overrideExistingInNonCalibratedAxes ? previous.y : 0,
            (calibrationAxes & CalibrationAxes.Roll) != 0 ? -Mathf.DeltaAngle(0, currentEuler.z) : overrideExistingInNonCalibratedAxes ? previous.z : 0);
        Pose.RotationCalibration = ToNumerics(rotation);
        PublishPose();
    }

    [BeforeRenderOrder(100)]
    protected override void OnBeforeRender()
    {
        base.OnBeforeRender();
        UpdateTransform();
    }

    private void Update() => UpdateTransform();
    private void LateUpdate() => UpdateTransform();

    private static void UpdateTransform()
    {
        RefreshRawPose();
        // Initialize once from a validated pose. Reacquisition events are not recenter commands.
        if (Pose.NeedsInitialReference) CalibrateTranslation(reason: "first-valid-head-pose");
        PublishPose();
    }

    private static void RefreshRawPose()
    {
        var valid = HeadPoseReader.TryRead(out var position, out var rotation);
        Pose.UpdateRaw(valid, ToNumerics(position), ToNumerics(rotation));
    }

    private static void PublishPose()
    {
        Translation = FromNumerics(Pose.Position);
        Rotation = FromNumerics(Pose.Rotation);
    }

    private static NumericsVector ToNumerics(Vector3 value) => new(value.x, value.y, value.z);
    private static NumericsRotation ToNumerics(Quaternion value) => new(value.x, value.y, value.z, value.w);
    private static Vector3 FromNumerics(NumericsVector value) => new(value.X, value.Y, value.Z);
    private static Quaternion FromNumerics(NumericsRotation value) => new(value.X, value.Y, value.Z, value.W);
}
