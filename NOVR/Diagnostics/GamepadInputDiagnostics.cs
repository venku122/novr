using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rewired;
using Steamworks;

namespace NOVR.Diagnostics;

/// <summary>Read-only discovery; does not create devices, initialize Steam Input or change Rewired backends.</summary>
public static class GamepadInputDiagnostics
{
    public static string CaptureJson() => DiagnosticJson.Serialize(Capture());
    internal static GamepadDiscoverySnapshot Capture()
    {
        var result = new GamepadDiscoverySnapshot { timestamp = DateTime.UtcNow.ToString("O") };
        if (ReInput.isReady)
        {
            result.rewiredBackend = ReInput.configuration.windowsStandalonePrimaryInputSource.ToString();
            var devices = new List<RewiredJoystickSnapshot>();
            foreach (var joystick in ReInput.controllers.Joysticks)
            {
                if (devices.Count >= 32) break;
                var device = new RewiredJoystickSnapshot { id = joystick.id, name = joystick.name, hardwareGuid = joystick.hardwareTypeGuid.ToString() };
                if (GameManager.playerInput != null)
                    foreach (var assigned in GameManager.playerInput.controllers.Joysticks)
                        if (assigned.id == joystick.id) device.assignedToPlayer = true;
                device.axes = new float[Math.Min(32, joystick.axisCount)];
                device.buttons = new bool[Math.Min(128, joystick.buttonCount)];
                for (var i = 0; i < device.axes.Length; i++) device.axes[i] = joystick.GetAxis(i);
                for (var i = 0; i < device.buttons.Length; i++) device.buttons[i] = joystick.GetButton(i);
                devices.Add(device);
            }
            result.rewiredJoysticks = devices.ToArray();
        }
        var slots = new XInputSlotSnapshot[4];
        for (uint i = 0; i < slots.Length; i++)
        {
            var slot = slots[i] = new XInputSlotSnapshot { index = (int)i };
            try
            {
                var error = XInputGetState(i, out var state);
                slot.status = error == 0 ? "connected" : "disconnected:" + error;
                if (error == 0)
                {
                    slot.packet = state.packet;
                    slot.buttons = state.gamepad.buttons;
                    slot.leftTrigger = state.gamepad.leftTrigger; slot.rightTrigger = state.gamepad.rightTrigger;
                    slot.leftX = state.gamepad.leftX; slot.leftY = state.gamepad.leftY;
                    slot.rightX = state.gamepad.rightX; slot.rightY = state.gamepad.rightY;
                }
            }
            catch (Exception ex) { slot.status = "unavailable:" + ex.GetType().Name; }
            try
            {
                var handle = SteamInput.GetControllerForGamepadIndex((int)i);
                slot.steamHandle = handle.m_InputHandle.ToString();
                if (handle.m_InputHandle != 0) slot.steamInputType = SteamInput.GetInputTypeForHandle(handle).ToString();
            }
            catch (Exception ex) { slot.steamInputType = "unavailable:" + ex.GetType().Name; }
        }
        result.xinputSlots = slots;
        return result;
    }

    [DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern uint XInputGetState(uint index, out NativeState state);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeState { public uint packet; public NativeGamepad gamepad; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeGamepad { public ushort buttons; public byte leftTrigger, rightTrigger; public short leftX, leftY, rightX, rightY; }
}

internal sealed class GamepadDiscoverySnapshot
{
    public int schemaVersion = 1;
    public string timestamp = "", rewiredBackend = "unavailable:not-ready";
    public RewiredJoystickSnapshot[] rewiredJoysticks = Array.Empty<RewiredJoystickSnapshot>();
    public XInputSlotSnapshot[] xinputSlots = Array.Empty<XInputSlotSnapshot>();
}
internal sealed class RewiredJoystickSnapshot
{
    public int id;
    public string name = "", hardwareGuid = "";
    public bool assignedToPlayer;
    public float[] axes = Array.Empty<float>();
    public bool[] buttons = Array.Empty<bool>();
}
internal sealed class XInputSlotSnapshot
{
    public int index;
    public string status = "unavailable", steamHandle = "", steamInputType = "unavailable:not-Steam-managed";
    public uint packet;
    public ushort buttons;
    public byte leftTrigger, rightTrigger;
    public short leftX, leftY, rightX, rightY;
}
