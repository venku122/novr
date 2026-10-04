using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using NOVR.VrUi;
using XrDevice = UnityEngine.XR.InputDevice;

namespace NOVR.Diagnostics;

[Serializable]
internal sealed class InputDiagnosticSnapshot
{
    public int schemaVersion = 2;
    public string timestamp = "";
    public string reason = "";
    public int frame;
    public string runtime = "";
    public string runtimeVersion = "";
    public string backend = "";
    public string leftProfile = "";
    public string rightProfile = "";
    public string unityVersion = "";
    public string gameVersion = "";
    public string gpu = "";
    public string scene = "";
    public string aircraft = "";
    public string frameExtensionAvailable = "";
    public string frameExtensionEnabled = "";
    public float renderScale;
    public float frameMilliseconds;
    public bool controllerPoseSmoothing;
    public bool gazeValid;
    public string gazeStatus = "";
    public Vector3 gazeTrackingPosition;
    public Quaternion gazeTrackingRotation;
    public string helmetHudTracking = "";
    public CockpitSnapshot cockpit = new CockpitSnapshot();
    public UiPointerSnapshot uiPointer = new UiPointerSnapshot();
    public int eyeWidth;
    public int eyeHeight;
    public string[] novrBindings = Array.Empty<string>();
    public DeviceSnapshot[] xrDevices = Array.Empty<DeviceSnapshot>();
    public DeviceSnapshot[] inputSystemDevices = Array.Empty<DeviceSnapshot>();

    public static InputDiagnosticSnapshot Capture(string reason)
    {
        var snapshot = new InputDiagnosticSnapshot
        {
            timestamp = DateTime.UtcNow.ToString("O"), reason = reason, frame = Time.frameCount,
            runtime = Safe(() => OpenXRRuntime.name), runtimeVersion = Safe(() => OpenXRRuntime.version),
            backend = XrStartupDiagnostics.ActiveLoaderName,
            leftProfile = Safe(() => OpenXRInputDiagnostics.CurrentProfile(true)),
            rightProfile = Safe(() => OpenXRInputDiagnostics.CurrentProfile(false)),
            unityVersion = Application.unityVersion, gameVersion = Application.version,
            gpu = SystemInfo.graphicsDeviceName, scene = SceneManager.GetActiveScene().name,
            aircraft = Core.CurrentAircraftId ?? "unavailable",
            frameExtensionAvailable = Safe(() => OpenXRRuntime.GetExtensionVersion("XR_VALVE_frame_controller_interaction").ToString()),
            frameExtensionEnabled = Safe(() => OpenXRRuntime.IsExtensionEnabled("XR_VALVE_frame_controller_interaction").ToString()),
            renderScale = XRSettings.eyeTextureResolutionScale,
            frameMilliseconds = Time.unscaledDeltaTime * 1000f,
            controllerPoseSmoothing = ModConfiguration.Instance.ControllerPoseSmoothing.Value,
            eyeWidth = XRSettings.eyeTextureWidth, eyeHeight = XRSettings.eyeTextureHeight,
            novrBindings = VrControllerInput.GetDiagnosticBindings()
        };
        snapshot.cockpit = CockpitDiagnostics.Capture();
        snapshot.gazeValid = NOVR.Controllers.EyeGazeInput.TryGetTrackingPose(out snapshot.gazeTrackingPosition, out snapshot.gazeTrackingRotation, out snapshot.gazeStatus);
        snapshot.helmetHudTracking = ModConfiguration.Instance.HelmetHudTracking.Value;
        if (VrUiCursor.Instance != null) snapshot.uiPointer = VrUiCursor.Instance.GetDiagnosticSnapshot();
        var xr = new List<XrDevice>();
        InputDevices.GetDevices(xr);
        var xrSnapshots = new List<DeviceSnapshot>();
        foreach (var device in xr) xrSnapshots.Add(ReadXrDevice(device));
        snapshot.xrDevices = xrSnapshots.ToArray();
        var inputSnapshots = new List<DeviceSnapshot>();
        foreach (var device in InputSystem.devices)
        {
            var controls = new List<ControlSnapshot>();
            // Keep keyboard and mouse text/input out of evidence. Frame may appear as a gamepad.
            if (device is TrackedDevice || device is Gamepad || device is Joystick)
            {
                foreach (var control in device.allControls)
                {
                    var value = ReadControl(control);
                    if (value != null) controls.Add(new ControlSnapshot { name = control.path, type = control.layout, value = value });
                }
            }
            var roles = new List<string>();
            foreach (var usage in device.usages) roles.Add(usage.ToString());
            inputSnapshots.Add(new DeviceSnapshot
            {
                id = device.deviceId.ToString(), name = device.name, manufacturer = device.description.manufacturer ?? "",
                role = string.Join(",", roles), layout = device.layout,
                valid = device.added, controls = controls.ToArray()
            });
        }
        snapshot.inputSystemDevices = inputSnapshots.ToArray();
        return snapshot;
    }

    private static DeviceSnapshot ReadXrDevice(XrDevice device)
    {
        var result = new DeviceSnapshot
        {
            id = "unavailable:UnityXR-public-API", name = device.name, manufacturer = device.manufacturer,
            role = device.characteristics.ToString(), valid = device.isValid
        };
        result.haptics = device.TryGetHapticCapabilities(out var haptic)
            ? $"channels={haptic.numChannels};impulse={haptic.supportsImpulse};buffer={haptic.supportsBuffer}" : "unavailable";
        var usages = new List<InputFeatureUsage>();
        var values = new List<ControlSnapshot>();
        if (device.TryGetFeatureUsages(usages))
        {
            foreach (var usage in usages)
            {
                string value = "unavailable";
                if (usage.type == typeof(bool) && device.TryGetFeatureValue(usage.As<bool>(), out var b)) value = b.ToString();
                else if (usage.type == typeof(float) && device.TryGetFeatureValue(usage.As<float>(), out var f)) value = f.ToString("R", CultureInfo.InvariantCulture);
                else if (usage.type == typeof(uint) && device.TryGetFeatureValue(usage.As<uint>(), out var u)) value = u.ToString(CultureInfo.InvariantCulture);
                else if (usage.type == typeof(Vector2) && device.TryGetFeatureValue(usage.As<Vector2>(), out var v2)) value = v2.ToString("F6");
                else if (usage.type == typeof(Vector3) && device.TryGetFeatureValue(usage.As<Vector3>(), out var v3)) value = v3.ToString("F6");
                else if (usage.type == typeof(Quaternion) && device.TryGetFeatureValue(usage.As<Quaternion>(), out var q)) value = q.ToString("F6");
                values.Add(new ControlSnapshot { name = usage.name, type = usage.type.Name, value = value });
            }
        }
        result.controls = values.ToArray();
        return result;
    }

    private static string? ReadControl(InputControl control)
    {
        try
        {
            return control switch
            {
                AxisControl axis => axis.ReadValue().ToString("R", CultureInfo.InvariantCulture),
                IntegerControl integer => integer.ReadValue().ToString(CultureInfo.InvariantCulture),
                Vector2Control vector2 => vector2.ReadValue().ToString("F6"),
                Vector3Control vector3 => vector3.ReadValue().ToString("F6"),
                QuaternionControl rotation => rotation.ReadValue().ToString("F6"),
                _ => null
            };
        }
        catch (Exception ex) { return "unavailable:" + ex.GetType().Name; }
    }

    private static string Safe(Func<string> read)
    {
        try { return read(); }
        catch (Exception ex) { return "unavailable:" + ex.GetType().Name; }
    }
}

[Serializable]
public sealed class UiPointerSnapshot
{
    public string source = "unavailable", hand = "None";
    public bool cursorVisible, focused, hasCanvasHit, pressed, dragging;
    public string canvasName = "", renderMode = "", canvasCamera = "", canvasParent = "";
    public string hoveredObject = "", pressedObject = "", dragObject = "";
    public Vector3 canvasPosition, canvasScale, rayOrigin, rayDirection, hitWorldPoint;
    public Quaternion canvasRotation = Quaternion.identity;
    public Vector2 screenPoint;
}

[Serializable]
public sealed class DeviceSnapshot
{
    public string id = "";
    public string name = "";
    public string manufacturer = "";
    public string role = "";
    public string layout = "";
    public bool valid;
    public string haptics = "unavailable";
    public ControlSnapshot[] controls = Array.Empty<ControlSnapshot>();
}

[Serializable]
public sealed class ControlSnapshot
{
    public string name = "";
    public string type = "";
    public string value = "unavailable";
}
