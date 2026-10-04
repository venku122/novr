using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace NOVR.Controllers;

internal static class HeadPoseReader
{
    private static XRHMD? _hmd;
    private static readonly List<UnityEngine.XR.InputDevice> XrHeads = new();
    public static string Source { get; private set; } = "unavailable";

    public static bool TryRead(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (_hmd == null || !_hmd.added)
        {
            _hmd = null;
            foreach (var device in InputSystem.devices)
                if (device is XRHMD hmd) { _hmd = hmd; break; }
        }
        if (_hmd != null && _hmd.isTracked.ReadValue() > .5f && (_hmd.trackingState.ReadValue() & 3) == 3)
        {
            position = _hmd.centerEyePosition.ReadValue();
            rotation = _hmd.centerEyeRotation.ReadValue();
            Source = "InputSystem/centerEye";
            return true;
        }
        // Older/OpenVR backends can expose Unity XR devices without Input System HMDs.
        // Enumerate actual tracked HMDs; XRNode.Head may report an untracked default (0,1.6,0).
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, XrHeads);
        foreach (var device in XrHeads)
        {
            // Eye-gaze devices also advertise HeadMounted. They are not HMD
            // head poses and must never drive the accepted seated camera.
            if ((device.characteristics & InputDeviceCharacteristics.EyeTracking) != 0) continue;
            if (!device.isValid || !device.TryGetFeatureValue(CommonUsages.isTracked, out var tracked) || !tracked ||
                !device.TryGetFeatureValue(CommonUsages.trackingState, out var state) ||
                (state & (InputTrackingState.Position | InputTrackingState.Rotation)) != (InputTrackingState.Position | InputTrackingState.Rotation)) continue;
            if (device.TryGetFeatureValue(CommonUsages.centerEyePosition, out position) &&
                device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rotation))
            { Source = "UnityXR/centerEye"; return true; }
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            { Source = "UnityXR/devicePose"; return true; }
        }
        Source = "unavailable:not-tracked";
        return false;
    }
}
