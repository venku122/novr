using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace NOVR.Diagnostics;

/// <summary>Read-only snapshots. Hierarchy and render matrix reads happen only on request.</summary>
public static class CockpitDiagnostics
{
    public static string CaptureJson() => DiagnosticJson.Serialize(Capture());

    internal static CockpitSnapshot Capture()
    {
        var snapshot = new CockpitSnapshot
        {
            timestamp = DateTime.UtcNow.ToString("O"), frame = Time.frameCount,
            aircraft = Core.CurrentAircraftId ?? "unavailable",
            cameraMode = CameraStateManager.cameraMode.ToString(),
            calibratedHeadPosition = NOVRHeadsetData.Translation,
            calibratedHeadRotation = NOVRHeadsetData.Rotation,
            translationAnchor = NOVRHeadsetData.TranslationAnchor,
            translationCalibration = NOVRHeadsetData.TranslationCalibrationOffset,
            rotationCalibration = NOVRHeadsetData.RotationCalibrationOffset,
            calibrationSequence = NOVRHeadsetData.CalibrationSequence,
            calibrationReason = NOVRHeadsetData.LastCalibrationReason,
            seatForwardOffset = ModConfiguration.Instance.CockpitHeadForwardOffset.Value,
            seatRightOffset = ModConfiguration.Instance.CockpitHeadRightOffset.Value
        };
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!head.isValid) head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        snapshot.headPositionValid = head.TryGetFeatureValue(CommonUsages.devicePosition, out snapshot.rawHeadPosition);
        snapshot.headRotationValid = head.TryGetFeatureValue(CommonUsages.deviceRotation, out snapshot.rawHeadRotation);
        head.TryGetFeatureValue(CommonUsages.isTracked, out snapshot.headTracked);
        var camera = Camera.main;
        if (camera != null)
        {
            snapshot.vrCamera = TransformSnapshot.Read(camera.transform);
            var parents = new List<TransformSnapshot>();
            for (var parent = camera.transform.parent; parent != null && parents.Count < 8; parent = parent.parent)
                parents.Add(TransformSnapshot.Read(parent));
            snapshot.cameraParents = parents.ToArray();
            var manager = camera.GetComponentInParent<CameraStateManager>();
            if (manager != null)
            {
                snapshot.gameCameraRoot = TransformSnapshot.Read(manager.transform);
                snapshot.cameraPivot = TransformSnapshot.Read(manager.cameraPivot);
            }
            snapshot.cameraWorldToView = MatrixValues(camera.worldToCameraMatrix);
            if (camera.stereoEnabled)
            {
                snapshot.leftEyeView = MatrixValues(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
                snapshot.rightEyeView = MatrixValues(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
            }
        }
        if (GameManager.GetLocalAircraft(out var aircraft) && aircraft != null)
        {
            snapshot.aircraftRoot = TransformSnapshot.Read(aircraft.transform);
            snapshot.cockpit = TransformSnapshot.Read(aircraft.cockpit != null ? aircraft.cockpit.transform : null);
            snapshot.seatReference = TransformSnapshot.Read(aircraft.cockpitViewPoint);
            if (aircraft.pilots != null && aircraft.pilots.Length > 0 && aircraft.pilots[0] != null)
                snapshot.pilot = TransformSnapshot.Read(aircraft.pilots[0].transform);
            if (camera != null)
            {
                snapshot.headInAircraft = aircraft.transform.InverseTransformPoint(camera.transform.position);
                if (aircraft.cockpitViewPoint != null)
                    snapshot.headInSeat = aircraft.cockpitViewPoint.InverseTransformPoint(camera.transform.position);
            }
        }
        return snapshot;
    }

    private static float[] MatrixValues(Matrix4x4 matrix)
    {
        var values = new float[16];
        for (var i = 0; i < values.Length; i++) values[i] = matrix[i];
        return values;
    }
}

internal sealed class CockpitSnapshot
{
    public int schemaVersion = 1, frame, calibrationSequence;
    public string timestamp = "", aircraft = "", cameraMode = "", calibrationReason = "";
    public bool headTracked, headPositionValid, headRotationValid;
    public Vector3 rawHeadPosition, calibratedHeadPosition, translationAnchor, translationCalibration, headInAircraft, headInSeat;
    public Quaternion rawHeadRotation, calibratedHeadRotation, rotationCalibration;
    public float seatForwardOffset, seatRightOffset;
    public TransformSnapshot vrCamera = new(), gameCameraRoot = new(), cameraPivot = new(), aircraftRoot = new(), cockpit = new(), seatReference = new(), pilot = new();
    public TransformSnapshot[] cameraParents = Array.Empty<TransformSnapshot>();
    public float[] cameraWorldToView = Array.Empty<float>(), leftEyeView = Array.Empty<float>(), rightEyeView = Array.Empty<float>();
}

internal sealed class TransformSnapshot
{
    public bool available;
    public string name = "", parent = "";
    public Vector3 localPosition, worldPosition, localScale;
    public Quaternion localRotation, worldRotation;

    public static TransformSnapshot Read(Transform? transform) => transform == null ? new() : new()
    {
        available = true, name = transform.name, parent = transform.parent != null ? transform.parent.name : "",
        localPosition = transform.localPosition, localRotation = transform.localRotation,
        worldPosition = transform.position, worldRotation = transform.rotation, localScale = transform.localScale
    };
}
