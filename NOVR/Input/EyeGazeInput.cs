using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace NOVR.Controllers;

/// <summary>Native combined gaze only; no eye cameras, per-eye/blink or confidence API.
/// Tracking and sharing availability are supplied by the OpenXR runtime.</summary>
internal static class EyeGazeInput
{
    private static EyeGazeInteraction.EyeGazeDevice? _device;
    private static float _nextDiscovery;

    public static bool TryGetTrackingPose(out Vector3 position, out Quaternion rotation, out string status)
    {
        position = Vector3.zero; rotation = Quaternion.identity;
        if (!ModConfiguration.Instance.EnableEyeTracking.Value) { status = "disabled"; return false; }
        if (!Application.isFocused) { status = "application-unfocused"; return false; }
        if (!OpenXRRuntime.IsExtensionEnabled("XR_EXT_eye_gaze_interaction")) { status = "extension-not-enabled:restart-or-runtime-unsupported"; return false; }
        if (_device != null && !_device.added) _device = null;
        if (_device == null && Time.unscaledTime >= _nextDiscovery)
        {
            _device = InputSystem.GetDevice<EyeGazeInteraction.EyeGazeDevice>();
            _nextDiscovery = Time.unscaledTime + .5f;
        }
        if (_device == null || !_device.enabled) { status = "device-unavailable:check-runtime-sharing"; return false; }
        var pose = _device.pose;
        if (pose == null || !(pose.isTracked.ReadValue() > .5f) || (pose.trackingState.ReadValue() & 3) != 3)
        { status = "not-tracked-or-not-shared"; return false; }
        position = pose.position.ReadValue(); rotation = pose.rotation.ReadValue();
        var length = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
        if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z) || !Finite(length) || length < .0001f)
        { position = Vector3.zero; rotation = Quaternion.identity; status = "invalid-pose"; return false; }
        rotation = Quaternion.Normalize(rotation);
        status = "tracked";
        return true;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
