using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using BepInEx.Configuration;
using UnityEngine;

namespace NOVR;

public class ModConfiguration
{
    public static ModConfiguration Instance;
    

    public readonly ConfigFile Config;
    public readonly ConfigEntry<float> TargetDesignatorOvershoot;
    public readonly ConfigEntry<bool> EnableNativeMenuUi;
    public readonly ConfigEntry<float> NativeMenuScale;
    public readonly ConfigEntry<float> NativeMenuDistance;
    public readonly ConfigEntry<float> NativeMenuHeightOffset;
    public readonly ConfigEntry<float> ZoomSpeed;
    public readonly ConfigEntry<float> MaximumZoom;
    public readonly ConfigEntry<bool> InstantZoomOut;
    public readonly ConfigEntry<string> CursorInputMode;
    public readonly ConfigEntry<bool> EnableSteamFrameInput;
    public readonly ConfigEntry<NOVR.Controllers.ControllerUseMode> ControllerMode;
    public readonly ConfigEntry<string> PointerHand;
    public readonly ConfigEntry<float> ControllerScrollSpeed;
    public readonly ConfigEntry<bool> ControllerUiHaptics;
    public readonly ConfigEntry<bool> ControllerPoseSmoothing;
    public readonly ConfigEntry<bool> ShowFrameControllerModels;
    public readonly ConfigEntry<string> SteamVrRuntimeDirectory;
    public readonly ConfigEntry<NOVR.Controllers.SpottingAimMode> SpottingAim;
    public readonly ConfigEntry<float> EyeAimSmoothing;
    public readonly ConfigEntry<NOVR.VrUi.SpecialBehavior.HudStatusMode> HudStatusMode;
    public readonly ConfigEntry<float> HudSmartAngle;
    public readonly ConfigEntry<bool> HudCockpitDeclutter;
    public readonly ConfigEntry<float> HudDeclutterDownAngle;
    public readonly ConfigEntry<bool> EnableEyeTracking;
    public readonly ConfigEntry<bool> ShowGazeReticle;
    public readonly ConfigEntry<string> HelmetHudTracking;
    public readonly ConfigEntry<bool> EnableNativeMenuEnvironment;
    public readonly ConfigEntry<bool> EnableExperimentalSteamVrControllerProfiles;
    public readonly ConfigEntry<bool> LogXrStartupDiagnostics;
    public readonly ConfigEntry<bool> VerboseDiagnostics;
    public readonly ConfigEntry<string> RawInputDiagnosticMode;
    public readonly ConfigEntry<KeyCode> RawInputSnapshotShortcut;
    public readonly ConfigEntry<bool> EnablePlaytestCapture;
    public readonly ConfigEntry<KeyCode> PlaytestCaptureShortcut;
    public readonly ConfigEntry<bool> PlaytestCaptureScreenshot;
    public readonly ConfigEntry<float> CockpitHeadForwardOffset;
    public readonly ConfigEntry<float> CockpitHeadRightOffset;
    public readonly ConfigEntry<KeyCode> RecenterShortcut;
    public readonly ConfigEntry<bool> ShowRecenterInPauseMenu;
    public readonly ConfigEntry<bool> SavePositionTrigger;
    public readonly ConfigEntry<float> MapClickMaxRadius;
    public readonly ConfigEntry<float> HudMinimapOpacity;
    public readonly ConfigEntry<float> HudMinimapSize;
    public readonly ConfigEntry<float> HudStatusScale;
    public readonly ConfigEntry<NOVR.VrUi.SpecialBehavior.HudStatusDetail> HudStatusDetail;

    private readonly Dictionary<string, (ConfigEntry<float> Forward, ConfigEntry<float> Right)> _perPlaneEntries = new();

    public ModConfiguration(ConfigFile config)
    {
        Instance = this;

        Config = config;
        TargetDesignatorOvershoot = config.Bind(
            "General",
            "Target Designator Overshoot",
            1.2f,
            "How much the target designator should multiply rotation to make for easier high off boresight target designation. Set to 1.0 to disable");

        EnableNativeMenuUi = config.Bind(
            "Experimental",
            "Enable Native Menu UI",
            true,
            "Use NOVR's native VR menu UI for non-flight menus. Disable to fall back to the existing patched game UI.");

        NativeMenuScale = config.Bind(
            "Experimental",
            "Native Menu Scale",
            1.25f,
            "Size multiplier for NOVR's native VR menu UI. Values from 0.75 to 2.0 are supported.");

        NativeMenuDistance = config.Bind(
            "Experimental",
            "Native Menu Distance",
            3.0f,
            "Distance in meters from the headset when NOVR's native VR menu UI is opened or recentered. Values from 1.5 to 6.0 are supported.");

        NativeMenuHeightOffset = config.Bind(
            "Experimental",
            "Native Menu Height Offset",
            0.0f,
            "Vertical offset in meters applied when NOVR's native VR menu UI is opened or recentered. Values from -0.25 to 1.0 are supported.");

        ZoomSpeed = config.Bind(
            "VR Zoom",
            "Zoom Speed",
            2.0f,
            new ConfigDescription(
                "How quickly headset magnification changes while Zoom View is held, in magnification units per second.",
                new AcceptableValueRange<float>(0.1f, 20.0f)));

        MaximumZoom = config.Bind(
            "VR Zoom",
            "Maximum Zoom",
            4.0f,
            new ConfigDescription(
                "Maximum binocular-style headset magnification.",
                new AcceptableValueRange<float>(1.0f, 10.0f)));

        InstantZoomOut = config.Bind(
            "VR Zoom",
            "Instant Zoom Out",
            false,
            "When enabled, any Zoom View out input immediately returns the headset view to 1x magnification.");
        CursorInputMode = config.Bind(
            "Experimental",
            "Cursor Input Mode",
            "Auto",
            "Selects input source for the VR UI cursor. 'Auto' = use controller if tracked, else mouse. 'Mouse' = always use mouse. 'Controller' = always use controller ray.");

        EnableNativeMenuEnvironment = config.Bind(
            "Experimental",
            "Enable Native Menu Environment",
            false,
            "Show an experimental 3D native menu environment using real game preview assets.");

        EnableSteamFrameInput = config.Bind("VR Input", "Enable Steam Frame Input", false,
            "Enable Valve native Frame and Oculus Touch compatibility OpenXR profiles before XR initialization. Restart required. UI actions only; HOTAS flight input is unchanged. Hardware validation pending.");
        ControllerMode = config.Bind("VR Input", "Controller Mode", NOVR.Controllers.ControllerUseMode.UiHotas,
            "UiHotas: controllers operate UI only, with HOTAS flight controls. UiGamepad: also expose a normal Xbox gamepad for the game's flight bindings. HOTAS bindings stay available in both. Switches live; requires the optional NOVR.Gamepad companion for gamepad output.");
        PointerHand = config.Bind("VR Input", "Pointer Hand", "Auto", "Auto, Right or Left preference; a single available controller is usable. Selection holds its hand until release.");
        ControllerScrollSpeed = config.Bind("VR Input", "Scroll Speed", 8f, "UI scroll units per second from the pointer hand thumbstick; no flight-axis mapping.");
        ControllerPoseSmoothing = config.Bind("VR Input", "Controller Pose Smoothing", false, "Optional legacy filtering. Off uses fresh tracked poses for minimum latency; click/drag edges still run once per game frame.");
        ShowFrameControllerModels = config.Bind("VR Input", "Show Frame Controller Models", true, "Load static Valve Frame meshes/textures from the installed SteamVR runtime. Requires Steam Frame Input; restart after toggling. No packaged Valve model assets.");
        SteamVrRuntimeDirectory = config.Bind("VR Input", "SteamVR Runtime Directory", "", "Optional SteamVR installation root for model assets. Empty discovers the registered runtime from openvrpaths.vrpath.");
        ControllerUiHaptics = config.Bind("VR Input", "UI Haptics", true, "Short pulse on controller UI activation, when Steam Frame input and impulse haptics are supported. No pulse on hover, drag or mouse activation.");
        SpottingAim = config.Bind("VR Input", "Spotting Aim", NOVR.Controllers.SpottingAimMode.EyePreferred,
            "Head uses helmet direction. EyePreferred uses valid native OpenXR gaze with head fallback. EyeOnly cancels target picking when gaze is unavailable. Requires Enable Eye Tracking (restart after enabling) and runtime gaze sharing for eye modes. Existing Select/paint bindings and game visibility/sensor rules remain in force; never auto-selects or fires.");
        EyeAimSmoothing = config.Bind("VR Input", "Eye Aim Smoothing", .035f,
            new ConfigDescription("Gaze direction filter time in seconds. 0 disables. Head fallback/recenter clears the filter immediately.", new AcceptableValueRange<float>(0, .15f)));
        HudStatusMode = config.Bind("VR Input", "HUD Status Placement", NOVR.VrUi.SpecialBehavior.HudStatusMode.Smart,
            "Aircraft preserves fixed status panels. Helmet keeps weapon/status panels in the visor. Smart keeps them forward until you look away, then follows the helmet. Flightpath, pitch, aiming and target symbols retain aircraft/world registration.");
        HudSmartAngle = config.Bind("VR Input", "Smart HUD Angle", 25f,
            new ConfigDescription("Head angle in degrees away from aircraft forward to move status panels into the helmet. Returns at 5 degrees less to avoid flicker.", new AcceptableValueRange<float>(10, 60)));
        HudCockpitDeclutter = config.Bind("VR Input", "Smart HUD Cockpit Declutter", true,
            "In Smart mode, keep status panels aircraft-fixed when looking down at instruments. Does not disable panels or change warning visibility. Helmet mode remains explicitly head-following.");
        HudDeclutterDownAngle = config.Bind("VR Input", "Smart HUD Down Angle", 35f,
            new ConfigDescription("Downward head angle relative to aircraft to return status to the cockpit. Clears at 5 degrees less; applies only to Smart cockpit declutter.", new AcceptableValueRange<float>(15, 75)));
        EnableEyeTracking = config.Bind("VR Input", "Enable Eye Tracking", false, "Request native Frame/standard OpenXR gaze through XR_EXT_eye_gaze_interaction. Restart after enabling; runtime eye-data sharing must allow access. Spotting Aim controls cueing. Never moves the camera, auto-clicks, fires, or changes graphics.");
        ShowGazeReticle = config.Bind("VR Input", "Show Gaze Reticle", false, "Show a small noninteractive gaze ring on VR UI when tracked gaze is available. No automatic hover, click or targeting. Requires Eye Tracking enabled; restart after enabling.");
        HelmetHudTracking = config.Bind("VR Input", "Helmet HUD Tracking", "Head", "Head follows the fresh head pose before rendering. Smoothed preserves the existing helmet HUD; Aircraft HUD stays aircraft-fixed.");

        EnableExperimentalSteamVrControllerProfiles = config.Bind(
            "Experimental",
            "Enable Experimental SteamVR Controller Profiles",
            false,
            "Register a minimal set of OpenXR controller interaction profiles before VR startup. Enables Valve Index, HTC Vive, and Khronos Simple Controller profiles only; hand tracking is not enabled.");

        LogXrStartupDiagnostics = config.Bind(
            "Diagnostics",
            "Log XR Startup Diagnostics",
            false,
            "Log read-only XR loader, OpenXR runtime, subsystem, and input device state during VR startup.");

        RawInputDiagnosticMode = config.Bind(
            "Diagnostics", "Raw Input Mode", "Off",
            "Read-only controller evidence: Off (default), Snapshot (startup/device/profile changes and shortcut), or Session (also samples at up to 10 Hz for 60 seconds, max 600 samples). Restart to change active modes. Max 720 files per game process.");
        RawInputSnapshotShortcut = config.Bind(
            "Diagnostics", "Raw Input Snapshot Shortcut", KeyCode.F10,
            "Capture a read-only XR/InputSystem snapshot when raw input diagnostics are enabled. Can be mapped from an unused HOTAS button externally.");

        EnablePlaytestCapture = config.Bind(
            "Playtest", "Enable Capture", false,
            "Send bounded capture-only requests to the external tools/vr-playtest service. Disabled by default; restart after enabling. No audio, speech recognition or AI runs in Unity.");
        PlaytestCaptureShortcut = config.Bind(
            "Playtest", "Capture Shortcut", KeyCode.None,
            "Hold an unused keyboard or legacy joystick KeyCode to record one observation, release to stop. None disables capture. Does not consume or remap flight controls; choose a key/button unused by flight controls. No motion-controller bindings are changed.");
        PlaytestCaptureScreenshot = config.Bind(
            "Playtest", "Capture Screenshot", true,
            "Request a game-rendered screenshot once per observation, not continuous video. At most 124 captures and 250 requests per game process.");

        VerboseDiagnostics = config.Bind(
            "Diagnostics",
            "Verbose Diagnostics",
            false,
            "When enabled, NOVR emits per-frame and per-second diagnostic logs " +
            "(controller laser dumps, controller input pose dumps, cursor mode dumps, " +
            "asset-cache waiting messages). Disabled by default for performance — " +
            "enable only when troubleshooting.");

        CockpitHeadForwardOffset = config.Bind(
            "Experimental",
            "Cockpit Head Forward Offset",
            0.08f,
            "Offset in meters applied to the cockpit head forward vector. Helps keep the ejection seat bars out of your face.");

        CockpitHeadRightOffset = config.Bind(
            "Experimental",
            "Cockpit Head Right Offset",
            0.0f,
            "Offset in meters applied to the cockpit head right vector. Moves you left (negative) or right (positive) to correct off-center seating.");

        RecenterShortcut = config.Bind(
            "Input",
            "Recenter Shortcut",
            KeyCode.F9,
            "Keyboard shortcut to recenter the VR view. For HOTAS users, map a joystick button to this key via external software.");

        ShowRecenterInPauseMenu = config.Bind(
            "Input",
            "Show Recenter In Pause Menu",
            true,
            "Add a 'RECENTER VIEW' button to the in-game pause menu while seated in a cockpit. Clicking it recenters the VR view immediately.");

        SavePositionTrigger = config.Bind(
            "Experimental",
            "Save Position For Current Aircraft",
            false,
            "Check this box to save the current Cockpit Head Forward/Right Offset values for the aircraft you're currently in. Automatically unchecks itself.");

        MapClickMaxRadius = config.Bind(
            "Map",
            "Click Max Radius",
            0.0375f,
            "Maximum normalized distance from the VR cursor to a map icon for the icon to be selectable on click. Distance is measured as a fraction of the map image's smaller dimension, so it stays consistent regardless of zoom level, HUD scale, or HMD resolution. Values from 0.0 to 0.5 are supported.");

        HudMinimapOpacity = config.Bind(
            "HUD",
            "Minimap Opacity",
            1.0f,
            "Opacity of the in-cockpit minimap (the small map in the HUD, not the full clickable map). " +
            "1.0 is fully opaque, 0.0 hides it completely. Applies only when the minimap is shown; the full map view is unaffected.");
        HudMinimapSize = config.Bind("HUD", "Minimap Angular Size", 12f, new ConfigDescription("Compact HUD minimap angular diameter in degrees; full tactical map is unchanged.", new AcceptableValueRange<float>(6, 22)));
        HudStatusScale = config.Bind("HUD", "Helmet Status Scale", .9f, new ConfigDescription("World scale for peripheral helmet status; native aircraft layout is unchanged.", new AcceptableValueRange<float>(.6f, 1.35f)));
        HudStatusDetail = config.Bind("HUD", "Smart Status Detail", NOVR.VrUi.SpecialBehavior.HudStatusDetail.Compact, "Compact Smart follows weapon and numeric readouts only; secondary panels retain aircraft placement. Full follows the full status panel. Targeting and flight symbols are unchanged.");

        SavePositionTrigger.SettingChanged += (_, _) =>
        {
            if (!SavePositionTrigger.Value) return;
            SavePositionTrigger.Value = false;

            if (string.IsNullOrEmpty(Core.CurrentAircraftId))
            {
                Debug.LogWarning("[NOVR] Cannot save cockpit offset: no aircraft is currently active.");
                return;
            }

            SaveCurrentOffsetFor(Core.CurrentAircraftId, CockpitHeadForwardOffset.Value, CockpitHeadRightOffset.Value);
        };

        PreloadPerPlaneEntries();
    }

    private void PreloadPerPlaneEntries()
    {
        if (!File.Exists(Config.ConfigFilePath)) return;

        var inPerPlaneSection = false;
        foreach (var rawLine in File.ReadAllLines(Config.ConfigFilePath))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                inPerPlaneSection = line.Equals("[PerPlaneOffsets]");
                continue;
            }

            if (!inPerPlaneSection) continue;

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0) continue;

            var key = line.Substring(0, equalsIndex).Trim();
            if (key.EndsWith("_Forward"))
            {
                GetOrCreatePlaneEntries(key.Substring(0, key.Length - "_Forward".Length));
            }
            else if (key.EndsWith("_Right"))
            {
                GetOrCreatePlaneEntries(key.Substring(0, key.Length - "_Right".Length));
            }
        }
    }

    private (ConfigEntry<float> Forward, ConfigEntry<float> Right) GetOrCreatePlaneEntries(string aircraftId)
    {
        if (_perPlaneEntries.TryGetValue(aircraftId, out var entries)) return entries;

        var forward = Config.Bind(
            "PerPlaneOffsets",
            $"{aircraftId}_Forward",
            float.NaN,
            "Saved Cockpit Head Forward Offset for this aircraft type. NaN means no value has been saved yet.");

        var right = Config.Bind(
            "PerPlaneOffsets",
            $"{aircraftId}_Right",
            float.NaN,
            "Saved Cockpit Head Right Offset for this aircraft type. NaN means no value has been saved yet.");

        entries = (forward, right);
        _perPlaneEntries[aircraftId] = entries;
        return entries;
    }

    public bool TryGetSavedOffset(string aircraftId, out float forward, out float right)
    {
        forward = 0f;
        right = 0f;
        if (string.IsNullOrEmpty(aircraftId)) return false;

        var entries = GetOrCreatePlaneEntries(aircraftId);
        if (float.IsNaN(entries.Forward.Value) || float.IsNaN(entries.Right.Value)) return false;

        forward = entries.Forward.Value;
        right = entries.Right.Value;
        return true;
    }

    public void SaveCurrentOffsetFor(string aircraftId, float forward, float right)
    {
        if (string.IsNullOrEmpty(aircraftId)) return;

        var entries = GetOrCreatePlaneEntries(aircraftId);
        entries.Forward.Value = forward;
        entries.Right.Value = right;
    }
}
