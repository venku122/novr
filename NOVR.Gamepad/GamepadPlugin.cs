using System;
using System.Runtime.CompilerServices;
using BepInEx;
using NOVR.Controllers;
using NOVR.VrUi;
using UnityEngine;

namespace NOVR.Gamepad
{
    [BepInPlugin("deltawing.novr.gamepad", "NOVR Steam Frame Gamepad", "0.1.0")]
    [BepInDependency("deltawing.novr")]
    [DefaultExecutionOrder(1000)]
    public sealed class GamepadPlugin : BaseUnityPlugin
    {
        private GamepadSession? _session;
        private readonly GamepadPolicy _policy = new GamepadPolicy();
        private readonly XrGamepadReader _reader = new XrGamepadReader();
        private bool _focused = true;
        private bool _paused;
        public static string Status { get; private set; } = "disabled";
        public static bool BusConnected { get; private set; }
        public static int UserIndex { get; private set; } = -1;
        public static PadReport LastReport { get; private set; }
        public static bool MenuForwarded => BusConnected && (LastReport.Buttons & (PadButtons.Start | PadButtons.Back)) != 0;
        private void Awake()
        {
            _session = new GamepadSession(CreateSink);
            _policy.Reset();
            _focused = Application.isFocused;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IGamepadSink CreateSink() { return new ViGEmSink(); }
        private void Update()
        {
            if (_session == null) return;
            bool wasConnected = BusConnected;
            bool enabled = ModConfiguration.Instance.ControllerMode.Value == ControllerUseMode.UiGamepad;
            try
            {
                if (!enabled) _policy.Reset();
                VrUiCursor.TryGetGamepadUiOwnership(out bool leftUi, out bool rightUi);
                LastReport = enabled ? _policy.Map(!_paused && _focused && Application.isFocused, _reader.Read(true), _reader.Read(false), leftUi, rightUi, VrUiCursor.OwnsControllerNavigation) : default;
                var error = _session.Update(enabled, LastReport);
                BusConnected = _session.Connected;
                UserIndex = _session.UserIndex;
                if (BusConnected && !wasConnected) Logger.LogInfo("Optional Steam Frame Xbox 360 gamepad connected; XInput user index=" + UserIndex);
                else if (!BusConnected && wasConnected) Logger.LogInfo("Optional Steam Frame gamepad disconnected.");
                if (error != null) { Status = "unavailable: " + error.GetType().Name; LastReport = default; Logger.LogWarning("Optional gamepad stopped: " + error.Message + ". No drivers were installed or changed. Select UI + HOTAS, then UI + GAMEPAD to retry."); }
                else if (!enabled) Status = "disabled";
                else if (BusConnected) Status = LastReport.IsNeutral ? "connected, neutral" : "connected";
            }
            catch (Exception error)
            {
                Cleanup(); enabled = false;
                ModConfiguration.Instance.ControllerMode.Value = ControllerUseMode.UiHotas;
                Status = "unavailable: " + error.GetType().Name;
                Logger.LogWarning("Optional gamepad stopped: " + error.Message);
            }
        }
        private void OnApplicationFocus(bool focus) { _focused = focus; if (!focus) Neutralize(); }
        private void OnApplicationPause(bool pause) { _paused = pause; if (pause) Neutralize(); }
        private void Neutralize()
        {
            bool wasConnected = BusConnected;
            _policy.Reset(); LastReport = default;
            var error = _session != null && _session.Connected ? _session.Update(true, default) : null;
            BusConnected = _session != null && _session.Connected;
            if (!BusConnected) UserIndex = -1;
            if (wasConnected && !BusConnected) Logger.LogInfo("Optional Steam Frame gamepad disconnected.");
            if (error != null) Logger.LogWarning("Optional gamepad neutralization failed: " + error.Message);
        }
        private void Cleanup()
        {
            bool wasConnected = BusConnected;
            _session?.Dispose(); _policy.Reset(); _reader.Reset();
            BusConnected = false; UserIndex = -1; LastReport = default; Status = "disabled";
            if (wasConnected) Logger.LogInfo("Optional Steam Frame gamepad disconnected.");
        }
        private void OnDisable() { Cleanup(); }
        private void OnDestroy() { Cleanup(); }
        private void OnApplicationQuit() { Cleanup(); }
    }
}
