using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace NOVR.Diagnostics;

/// <summary>Opt-in capture requests only. Audio/recognition/AI stay outside Unity.</summary>
internal sealed class PlaytestCaptureBridge : MonoBehaviour
{
    private readonly CaptureGesture _gesture = new();
    private static int _requestCount;
    private static bool _failed;
    private string _captureId = "";
    private bool _configurationWasEnabled;

    private void Update()
    {
        var config = ModConfiguration.Instance;
        var configured = config.EnablePlaytestCapture.Value && config.PlaytestCaptureShortcut.Value != KeyCode.None && !_failed;
        if (!configured)
        {
            if (_configurationWasEnabled && _gesture.Cancel() == CaptureTransition.Stop) Publish("stop", null);
            _configurationWasEnabled = false;
            return;
        }
        _configurationWasEnabled = true;
        var transition = _gesture.Update(Input.GetKey(config.PlaytestCaptureShortcut.Value), Time.realtimeSinceStartup);
        if (transition == CaptureTransition.Start)
        {
            if (_requestCount >= 248) { _gesture.Cancel(); return; }
            _captureId = Guid.NewGuid().ToString();
            var relative = "BepInEx/NOVR/playtest/evidence/" + _captureId;
            try
            {
                var directory = Path.Combine(Paths.GameRootPath, "BepInEx", "NOVR", "playtest", "evidence");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, _captureId + ".json"), DiagnosticJson.Serialize(InputDiagnosticSnapshot.Capture("playtest-capture")));
                if (config.PlaytestCaptureScreenshot.Value) ScreenCapture.CaptureScreenshot(Path.Combine(directory, _captureId + ".png"));
                Publish("start", new CaptureEvidence { telemetry = relative + ".json", screenshot = config.PlaytestCaptureScreenshot.Value ? relative + ".png" : "", log = "BepInEx/LogOutput.log" });
            }
            catch (Exception ex) { Fail(ex); }
        }
        else if (transition == CaptureTransition.Stop) Publish("stop", null);
    }

    private void OnDisable()
    {
        if (_gesture.Cancel() == CaptureTransition.Stop) Publish("stop", null);
    }

    private void Publish(string kind, CaptureEvidence? evidence)
    {
        if (_failed || _captureId.Length == 0 || _requestCount >= 250) return;
        try
        {
            var directory = Path.Combine(Paths.BepInExRootPath, "NOVR", "playtest", "requests");
            Directory.CreateDirectory(directory);
            var request = new CaptureRequest { id = Guid.NewGuid().ToString(), captureId = _captureId, timestamp = DateTime.UtcNow.ToString("O"), kind = kind, evidence = evidence };
            var target = Path.Combine(directory, request.id + ".json");
            var temporary = target + ".tmp";
            File.WriteAllText(temporary, DiagnosticJson.Serialize(request));
            File.Move(temporary, target);
            _requestCount++;
            Debug.Log($"[NOVR playtest] {kind} capture={_captureId}; external helper handles recording. Requests are not recording acknowledgments.");
        }
        catch (Exception ex) { Fail(ex); }
    }

    private static void Fail(Exception ex)
    {
        if (_failed) return;
        _failed = true;
        Debug.LogWarning($"[NOVR playtest] Capture bridge disabled after failure: {ex}");
    }

    [Serializable] private sealed class CaptureRequest
    {
        public int version = 1;
        public string id = "";
        public string captureId = "";
        public string timestamp = "";
        public string kind = "";
        public CaptureEvidence? evidence;
    }
    [Serializable] private sealed class CaptureEvidence
    {
        public string telemetry = "";
        public string screenshot = "";
        public string log = "";
    }
}
