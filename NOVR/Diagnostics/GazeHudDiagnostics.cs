using System;
using NOVR.Controllers;
using NOVR.VrUi;
using NOVR.VrUi.SpecialBehavior;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace NOVR.Diagnostics;

/// <summary>Main-thread, on-request evidence. No config, recenter, selection or network mutation.</summary>
public static class GazeHudDiagnostics
{
    public static string CaptureJson() => DiagnosticJson.Serialize(Capture());
    internal static GazeHudSnapshot Capture()
    {
        var config = ModConfiguration.Instance;
        var snapshot = new GazeHudSnapshot
        {
            timestamp = DateTime.UtcNow.ToString("O"),
            aircraft = Core.CurrentAircraftId ?? "unavailable",
            eyeTrackingRequested = config.EnableEyeTracking.Value,
            gazeExtensionEnabled = OpenXRRuntime.IsExtensionEnabled("XR_EXT_eye_gaze_interaction"),
            aimMode = config.SpottingAim.Value.ToString(),
            helmetTracking = config.HelmetHudTracking.Value,
            statusMode = config.HudStatusMode.Value.ToString(),
            smartAngle = config.HudSmartAngle.Value,
            statusFollowsHelmet = NOVRFlightHudBehavior.Instance != null && NOVRFlightHudBehavior.Instance.StatusFollowsHelmet,
            cockpitDecluttered = NOVRFlightHudBehavior.Instance != null && NOVRFlightHudBehavior.Instance.StatusCockpitDecluttered,
            lookingDownAngle = NOVRFlightHudBehavior.Instance != null ? NOVRFlightHudBehavior.Instance.StatusLookingDownAngle : 0,
            statusBoresightAngle = NOVRFlightHudBehavior.Instance != null ? NOVRFlightHudBehavior.Instance.StatusBoresightAngle : 0,
            panels = NOVRFlightHudBehavior.CaptureStatusPanelSnapshots(),
            pointer = VrUiCursor.Instance != null ? VrUiCursor.Instance.GetDiagnosticSnapshot() : null,
        };
        snapshot.gazeTracked = EyeGazeInput.TryGetTrackingPose(out snapshot.rawGazePosition, out snapshot.rawGazeRotation, out snapshot.gazeStatus);
        // Read cached application state. Evidence collection must not advance
        // the cue filter or consume the frame's smoothing delta.
        snapshot.aimSampleFrame = SpottingAimInput.SampleFrame;
        snapshot.snapshotFrame = Time.frameCount;
        snapshot.aimValid = SpottingAimInput.Valid && Application.isFocused && NOVRHeadsetData.HeadTracked;
        var ray = SpottingAimInput.LastUiRay;
        snapshot.aimSource = SpottingAimInput.Source.ToString();
        snapshot.aimStatus = SpottingAimInput.Status;
        if (snapshot.aimValid && NOUIManager.I != null && APIBus.MainCamera != null)
        {
            snapshot.uiOrigin = ray.origin; snapshot.uiDirection = ray.direction;
            var uiCamera = APIBus.CockpitHudCamera;
            var camera = APIBus.MainCamera;
            var uiToWorld = camera.transform.rotation * Quaternion.Inverse(uiCamera.transform.rotation);
            snapshot.worldOrigin = camera.transform.position + uiToWorld * (ray.origin - uiCamera.transform.position);
            snapshot.worldDirection = uiToWorld * ray.direction;
        }
        return snapshot;
    }
}

internal sealed class GazeHudSnapshot
{
    public int schemaVersion = 1;
    public int snapshotFrame, aimSampleFrame;
    public string timestamp = "", aircraft = "", aimMode = "", aimSource = "", aimStatus = "", gazeStatus = "", helmetTracking = "", statusMode = "";
    public bool eyeTrackingRequested, gazeExtensionEnabled, gazeTracked, aimValid, statusFollowsHelmet, cockpitDecluttered;
    public float smartAngle, statusBoresightAngle, lookingDownAngle;
    public Vector3 rawGazePosition, uiOrigin, uiDirection, worldOrigin, worldDirection;
    public Quaternion rawGazeRotation;
    public HudPanelSnapshot[] panels = Array.Empty<HudPanelSnapshot>();
    public UiPointerSnapshot? pointer;
}
