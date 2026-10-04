using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Input;
using XrDevice = UnityEngine.XR.InputDevice;
using XrUsages = UnityEngine.XR.CommonUsages;

namespace NOVR.Controllers;

/// <summary>Native combined gaze only; no eye cameras, per-eye/blink or confidence API.
/// Tracking and sharing availability are supplied by the OpenXR runtime.</summary>
internal static class EyeGazeInput
{
    private static EyeGazeInteraction.EyeGazeDevice? _device;
    private static readonly List<XrDevice> XrEyes = new();
    private static readonly InputFeatureUsage<bool> GazeTracked = new("gazeIsTracked");
    private static readonly InputFeatureUsage<uint> GazeTrackingState = new("gazeTrackingState");
    private static float _nextDiscovery;
    public static string Source { get; private set; } = "unavailable";

    public static bool TryGetTrackingPose(out Vector3 position, out Quaternion rotation, out string status)
    {
        position = Vector3.zero; rotation = Quaternion.identity;
        Source = "unavailable";
        if (!ModConfiguration.Instance.EnableEyeTracking.Value) { status = "disabled"; return false; }
        if (!Application.isFocused) { status = "application-unfocused"; return false; }
        if (!OpenXRRuntime.IsExtensionEnabled("XR_EXT_eye_gaze_interaction")) { status = "extension-not-enabled:restart-or-runtime-unsupported"; return false; }
        if (_device != null && !_device.added) _device = null;
        if (Time.unscaledTime >= _nextDiscovery)
        {
            _device = InputSystem.GetDevice<EyeGazeInteraction.EyeGazeDevice>();
            // Unity XR still exposes native eyes when Input System layout creation
            // fails (for example a PoseControl type collision). Reuse the list,
            // bounded discovery also handles reconnect without per-frame scans.
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, XrEyes);
            _nextDiscovery = Time.unscaledTime + .5f;
        }
        status = "no-native-eye-device:inspect-device-inventory";
        if (_device != null && _device.enabled)
        {
            status = "input-system-eye-not-tracked-or-not-shared";
            if (_device.TryReadNativePose(out var nativePosition, out var nativeRotation, out var nativeTracked, out var nativeState) &&
                nativeTracked && (nativeState & 3) == 3)
            {
                if (Validate(nativePosition, nativeRotation, out position, out rotation))
                { Source = "InputSystem/nativeGaze"; status = "tracked"; return true; }
                status = "input-system-invalid-gaze-pose";
            }
        }
        foreach (var eye in XrEyes)
        {
            if (!eye.isValid) continue;
            status = "unity-xr-eye-not-tracked-or-not-shared";
            // A provided gaze-specific invalid flag must not fall through to a
            // generic device flag. HMD tracking is never accepted as eye tracking.
            var hasGazeTracked = eye.TryGetFeatureValue(GazeTracked, out var gazeTracked);
            var hasGazeState = eye.TryGetFeatureValue(GazeTrackingState, out var gazeState);
            var hasTracked = eye.TryGetFeatureValue(XrUsages.isTracked, out var tracked);
            var hasState = eye.TryGetFeatureValue(XrUsages.trackingState, out var state);
            if (!EyePoseValidation.HasValidTracking(hasGazeTracked, gazeTracked, hasTracked, tracked,
                hasGazeState, gazeState, hasState, (uint)state)) continue;
            if (!eye.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out var gazePosition) ||
                !eye.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out var gazeRotation))
            { status = "unity-xr-missing-native-gaze-features"; continue; }
            if (!Validate(gazePosition, gazeRotation, out position, out rotation))
            { status = "unity-xr-invalid-gaze-pose"; continue; }
            Source = "UnityXR/nativeGaze"; status = "tracked"; return true;
        }
        return false;
    }

    private static bool Validate(Vector3 value, Quaternion orientation, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero; rotation = Quaternion.identity;
        if (!EyePoseValidation.TryValidate(true, 3,
            new System.Numerics.Vector3(value.x, value.y, value.z),
            new System.Numerics.Quaternion(orientation.x, orientation.y, orientation.z, orientation.w), out var normalized)) return false;
        position = value; rotation = new Quaternion(normalized.X, normalized.Y, normalized.Z, normalized.W);
        return true;
    }

    /// <summary>Only requested snapshots enumerate/allocate detailed inventory.</summary>
    public static EyeGazeDeviceInventory CaptureDeviceInventory()
    {
        var inventory = new EyeGazeDeviceInventory { source = Source };
        inventory.eyeInteractionProfile = EyeGazeRuntimeDiagnostics.CurrentEyeProfile();
        inventory.poseLayoutType = InputSystem.LoadLayout("Pose")?.type?.FullName ?? "unregistered";
        inventory.eyeLayoutType = InputSystem.LoadLayout("EyeGaze")?.type?.FullName ?? "unregistered";
        var layouts = new List<string>();
        foreach (var layout in InputSystem.ListLayouts())
            if (layout.IndexOf("EyeGaze", StringComparison.OrdinalIgnoreCase) >= 0) layouts.Add(layout);
        inventory.eyeLayouts = layouts.ToArray();
        var inputDevices = new List<EyeInputDeviceSnapshot>();
        foreach (var device in InputSystem.devices)
            if (device is EyeGazeInteraction.EyeGazeDevice ||
                device.description.product != null && device.description.product.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0)
                inputDevices.Add(new EyeInputDeviceSnapshot { name = device.name, layout = device.layout,
                    product = device.description.product ?? "", enabled = device.enabled, added = device.added,
                    poseImplementation = device is EyeGazeInteraction.EyeGazeDevice eye ? eye.nativePoseImplementation : "not-eye-gaze-layout" });
        inventory.inputSystemDevices = inputDevices.ToArray();
        var nativeDevices = new List<XrDevice>();
        InputDevices.GetDevices(nativeDevices);
        var connected = new List<string>();
        foreach (var device in nativeDevices) connected.Add(device.name + ":" + device.characteristics);
        inventory.allUnityXrDevices = connected.ToArray();
        nativeDevices.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, nativeDevices);
        var snapshots = new List<EyeXrDeviceSnapshot>();
        var features = new List<InputFeatureUsage>();
        foreach (var device in nativeDevices)
        {
            var snapshot = new EyeXrDeviceSnapshot { name = device.name,
                characteristics = device.characteristics.ToString(), valid = device.isValid };
            snapshot.hasGazeTracked = device.TryGetFeatureValue(GazeTracked, out snapshot.gazeTracked);
            snapshot.hasGazeTrackingState = device.TryGetFeatureValue(GazeTrackingState, out snapshot.gazeTrackingState);
            snapshot.hasDeviceTracked = device.TryGetFeatureValue(XrUsages.isTracked, out snapshot.deviceTracked);
            snapshot.hasDeviceTrackingState = device.TryGetFeatureValue(XrUsages.trackingState, out var deviceState);
            snapshot.deviceTrackingState = (uint)deviceState;
            snapshot.hasGazePosition = device.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out snapshot.gazePosition);
            snapshot.hasGazeRotation = device.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out snapshot.gazeRotation);
            try
            {
                snapshot.gazePositionActionRegistered = OpenXRInput.GetActionHandle(device, EyeTrackingUsages.gazePosition.name) != 0;
                snapshot.gazeRotationActionRegistered = OpenXRInput.GetActionHandle(device, EyeTrackingUsages.gazeRotation.name) != 0;
                snapshot.gazePositionActionActive = OpenXRInput.GetActionIsActive(device, EyeTrackingUsages.gazePosition.name);
                snapshot.gazeRotationActionActive = OpenXRInput.GetActionIsActive(device, EyeTrackingUsages.gazeRotation.name);
                snapshot.nativeActionQueryStatus = "queried";
            }
            catch (DllNotFoundException) { snapshot.nativeActionQueryStatus = "native-library-unavailable"; }
            catch (EntryPointNotFoundException) { snapshot.nativeActionQueryStatus = "native-entry-point-unavailable"; }
            catch (InvalidOperationException) { snapshot.nativeActionQueryStatus = "native-session-unavailable"; }
            features.Clear(); device.TryGetFeatureUsages(features);
            var names = new List<string>();
            foreach (var feature in features) names.Add(feature.name + ":" + feature.type.Name);
            snapshot.features = names.ToArray(); snapshots.Add(snapshot);
        }
        inventory.unityXrDevices = snapshots.ToArray();
        return inventory;
    }
}

internal sealed class EyeGazeDeviceInventory
{
    public string source = "";
    public string eyeInteractionProfile = "", poseLayoutType = "", eyeLayoutType = "";
    public string[] allUnityXrDevices = Array.Empty<string>();
    public string[] eyeLayouts = Array.Empty<string>();
    public EyeInputDeviceSnapshot[] inputSystemDevices = Array.Empty<EyeInputDeviceSnapshot>();
    public EyeXrDeviceSnapshot[] unityXrDevices = Array.Empty<EyeXrDeviceSnapshot>();
}
internal sealed class EyeInputDeviceSnapshot
{
    public string name = "", layout = "", product = "";
    public string poseImplementation = "";
    public bool enabled, added;
}
internal sealed class EyeXrDeviceSnapshot
{
    public string name = "", characteristics = "";
    public string nativeActionQueryStatus = "not-queried";
    public bool gazePositionActionRegistered, gazeRotationActionRegistered, gazePositionActionActive, gazeRotationActionActive;
    public bool valid, hasGazeTracked, gazeTracked, hasGazeTrackingState, hasDeviceTracked, deviceTracked,
        hasDeviceTrackingState, hasGazePosition, hasGazeRotation;
    public uint gazeTrackingState, deviceTrackingState;
    public Vector3 gazePosition;
    public Quaternion gazeRotation;
    public string[] features = Array.Empty<string>();
}
