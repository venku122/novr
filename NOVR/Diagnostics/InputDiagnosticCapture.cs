using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using BepInEx;

namespace NOVR.Diagnostics;

/// <summary>Opt-in baseline evidence. Never sends input, changes profiles or writes game settings.</summary>
internal sealed class InputDiagnosticCapture : MonoBehaviour
{
    private static DiagnosticCapturePolicy? _policy;
    private static string? _sessionDirectory;
    private static int _sequence;
    private static int _captureCount;
    private static bool _failureReported;
    private static string _lastLeft = "";
    private static string _lastRight = "";
    private float _nextProfilePoll;
    private float _nextSnapshot;
    private bool _deviceChanged = true;
    private DiagnosticMode _mode;

    private void OnEnable()
    {
        _mode = DiagnosticCapturePolicy.Parse(ModConfiguration.Instance.RawInputDiagnosticMode.Value);
        if (_mode == DiagnosticMode.Off) { enabled = false; return; }
        _policy ??= new DiagnosticCapturePolicy(_mode, Time.realtimeSinceStartup);
        // Static process budget survives NOVR Core recreation / scene changes.
        _sessionDirectory ??= Path.Combine(Paths.BepInExRootPath, "NOVR", "input-diagnostics", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
        InputDevices.deviceConnected += OnXrDeviceChanged;
        InputDevices.deviceDisconnected += OnXrDeviceChanged;
        InputSystem.onDeviceChange += OnInputDeviceChanged;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnXrDeviceChanged;
        InputDevices.deviceDisconnected -= OnXrDeviceChanged;
        InputSystem.onDeviceChange -= OnInputDeviceChanged;
    }

    private void OnXrDeviceChanged(UnityEngine.XR.InputDevice device) => _deviceChanged = true;
    private void OnInputDeviceChanged(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change) => _deviceChanged = true;

    private void Update()
    {
        if (DiagnosticCapturePolicy.Parse(ModConfiguration.Instance.RawInputDiagnosticMode.Value) == DiagnosticMode.Off) return;
        var now = Time.realtimeSinceStartup;
        if (now >= _nextProfilePoll)
        {
            _nextProfilePoll = now + 1;
            try
            {
                var left = OpenXRInputDiagnostics.CurrentProfile(true);
                var right = OpenXRInputDiagnostics.CurrentProfile(false);
                if (_deviceChanged || left != _lastLeft || right != _lastRight)
                {
                    _deviceChanged = false;
                    _lastLeft = left;
                    _lastRight = right;
                    Capture("startup/device/profile-change", true);
                }
            }
            catch (Exception ex) { ReportFailure(ex); }
        }
        if (_policy != null && _policy.ShouldSample(now)) Capture("bounded-session-sample", false);
        if (Input.GetKeyDown(ModConfiguration.Instance.RawInputSnapshotShortcut.Value) && now >= _nextSnapshot)
        {
            _nextSnapshot = now + 0.5f;
            Capture("manual-snapshot", true);
        }
    }

    private static void Capture(string reason, bool logSummary)
    {
        // Bound disk usage even if devices continuously reconnect or snapshots are held.
        if (_captureCount >= 720 || _failureReported) return;
        try
        {
            var snapshot = InputDiagnosticSnapshot.Capture(reason);
            Directory.CreateDirectory(_sessionDirectory!);
            var filename = Path.Combine(_sessionDirectory!, $"{++_sequence:D5}.json");
            File.WriteAllText(filename, JsonUtility.ToJson(snapshot, true));
            _captureCount++;
            if (logSummary)
                Debug.Log($"[NOVR input] reason={reason} backend={snapshot.backend} runtime={snapshot.runtime} L={snapshot.leftProfile} R={snapshot.rightProfile} xrDevices={snapshot.xrDevices.Length} inputDevices={snapshot.inputSystemDevices.Length} evidence={filename}");
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static void ReportFailure(Exception ex)
    {
        if (_failureReported) return;
        _failureReported = true;
        Debug.LogWarning($"[NOVR input] Evidence capture disabled after failure: {ex}");
    }
}
