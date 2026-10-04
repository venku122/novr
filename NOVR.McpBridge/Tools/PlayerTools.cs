using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NOVR.McpBridge.Tools;

public static class PlayerTools
{
    [McpTool("get_player_state", "Reads player aircraft state via NOVR's aircraft tracking (position, velocity, health, etc.).")]
    public static string GetPlayerState()
    {
        var sb = new StringBuilder();

        var coreType = FindType("NOVR.Core");
        if (coreType == null)
        {
            sb.AppendLine("NOVR.Core not found. Is the NOVR plugin loaded?");
            return sb.ToString();
        }

        var coreInstance = coreType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)
                          ?? coreType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                              ?.GetValue(null);
        if (coreInstance == null)
        {
            sb.AppendLine("NOVR.Core.Instance is null.");
            return sb.ToString();
        }

        var trackedAircraftProp = coreType.GetProperty("TrackedAircraft",
            BindingFlags.Public | BindingFlags.Instance);
        var trackedAircraft = trackedAircraftProp?.GetValue(coreInstance);

        if (trackedAircraft == null)
        {
            sb.AppendLine("TrackedAircraft: not tracked");
            return sb.ToString();
        }

        var trackedType = trackedAircraft.GetType();

        AppendProperty(sb, trackedType, trackedAircraft, "Position");
        AppendProperty(sb, trackedType, trackedAircraft, "Velocity");
        AppendProperty(sb, trackedType, trackedAircraft, "Altitude");
        AppendProperty(sb, trackedType, trackedAircraft, "Health");
        AppendProperty(sb, trackedType, trackedAircraft, "Throttle");
        AppendProperty(sb, trackedType, trackedAircraft, "Speed");
        AppendProperty(sb, trackedType, trackedAircraft, "Name");

        return sb.ToString();
    }

    [McpTool("get_vr_head_state", "Read-only NOVR cockpit/HMD/seat transform and calibration snapshot. No recenter or configuration changes.")]
    public static string GetVrHeadState()
    {
        var diagnostics = FindType("NOVR.Diagnostics.CockpitDiagnostics");
        var capture = diagnostics?.GetMethod("CaptureJson", BindingFlags.Public | BindingFlags.Static);
        return capture?.Invoke(null, null) as string ?? "NOVR cockpit diagnostics unavailable.";
    }

    [McpTool("get_gamepad_state", "Read-only Rewired controller assignments/backend and XInput/Steam Input slot values. Does not change input settings or create devices.")]
    public static string GetGamepadState()
    {
        var diagnostics = FindType("NOVR.Diagnostics.GamepadInputDiagnostics");
        var capture = diagnostics?.GetMethod("CaptureJson", BindingFlags.Public | BindingFlags.Static);
        return capture?.Invoke(null, null) as string ?? "NOVR gamepad diagnostics unavailable.";
    }

    private static void AppendProperty(StringBuilder sb, Type type, object instance, string propName)
    {
        var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null) return;
        try
        {
            var value = prop.GetValue(instance);
            sb.AppendLine($"{propName}: {value}");
        }
        catch { }
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(fullName, false, true))
            .FirstOrDefault(t => t != null);
    }
}