using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace NOVR.Controllers;

/// <summary>Read-only gaze. Invalid/blinking/unshared data never becomes a valid ray.</summary>
internal static class EyeGazeInput
{
    public static bool TryGetTrackingPose(out Vector3 position, out Quaternion rotation, out string status)
    {
        position = Vector3.zero; rotation = Quaternion.identity;
        if (!ModConfiguration.Instance.EnableEyeTracking.Value) { status = "disabled"; return false; }
        if (!OpenXRRuntime.IsExtensionEnabled("XR_EXT_eye_gaze_interaction")) { status = "extension-not-enabled"; return false; }
        var device = InputSystem.GetDevice<EyeGazeInteraction.EyeGazeDevice>();
        if (device == null || !device.added) { status = "device-unavailable"; return false; }
        var pose = device.pose;
        if (pose == null || pose.isTracked.ReadValue() < .5f || (pose.trackingState.ReadValue() & 3) != 3)
        { status = "not-tracked-or-not-shared"; return false; }
        position = pose.position.ReadValue(); rotation = pose.rotation.ReadValue(); status = "tracked";
        return true;
    }
}
