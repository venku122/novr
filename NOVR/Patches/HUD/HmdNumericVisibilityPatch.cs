using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using NOVR.VrUi;
using NOVR.VrUi.SpecialBehavior;
using UnityEngine;

namespace NOVR.Patches.HUD;

[HarmonyPatch(typeof(HeadMountedDisplay), "Update")]
internal static class HmdNumericVisibilityPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var source = instructions.ToList();
        var signature = new[] { typeof(Vector3), typeof(Vector3), typeof(float) };
        var native = AccessTools.Method(typeof(FastMath), nameof(FastMath.InRange), signature);
        var adapter = AccessTools.Method(typeof(HmdNumericVisibilityPatch), nameof(InRange), signature);
        if (native == null || !native.IsStatic || native.ReturnType != typeof(bool) || adapter == null ||
            !ExactCallReplacement.TryReplace(source,
                instruction => instruction.opcode == OpCodes.Call && Equals(instruction.operand, native),
                instruction => { var copy = new CodeInstruction(instruction); copy.operand = adapter; return copy; },
                4, out var patched))
        {
            NOVRPlugin.LogSource?.LogWarning("Numeric HMD visibility patch skipped: expected exactly four FastMath.InRange(Vector3, Vector3, float) calls. Native visibility retained.");
            return source;
        }
        return patched;
    }

    private static bool InRange(Vector3 hudCenter, Vector3 appPosition, float hideDistance)
    {
        bool nativeHide = FastMath.InRange(hudCenter, appPosition, hideDistance);
        var behavior = NOVRFlightHudBehavior.Instance;
        bool referenceValid = behavior != null && behavior.isActiveAndEnabled && behavior.HelmetCenterAvailable &&
            APIBus.MainCamera != null && NOUIManager.I != null && NOVRHeadsetData.HeadTracked &&
            GameManager.GetLocalAircraft(out var aircraft) && aircraft != null && aircraft.cockpit != null;
        return HmdNumericVisibilityPolicy.Hide(nativeHide, ModConfiguration.Instance.HudStatusMode.Value,
            referenceValid, behavior != null && behavior.StatusFollowsHelmet,
            behavior != null && behavior.StatusCockpitDecluttered);
    }
}
