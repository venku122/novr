using NOVR.Controllers;
using NOVR.PatchHelper;
using NOVR.VrUi.SpecialBehavior;

namespace NOVR.Patches.HUD;

internal static class GazeTargetSelectionPatch
{
    // Native selection refreshes marker positions in LateUpdate. Update the cue
    // at the selection boundary so it cannot select against last frame's gaze.
    // No new targeting call, range/exclusion override, or flight binding is added.
    [PatchPrefix(typeof(CombatHUD), "TargetSelect")]
    private static bool TargetSelect(CombatHUD __instance)
    {
        if (CameraStateManager.cameraMode != CameraMode.cockpit) return true;
        var cue = __instance.targetDesignator;
        var behavior = cue != null ? cue.GetComponent<NOVRTargetDesignatorBehavior>() : null;
        if (behavior != null) return behavior.RefreshAim();
        // Fail closed in strict eye mode if UI adaptation did not install.
        return ModConfiguration.Instance.SpottingAim.Value != SpottingAimMode.EyeOnly;
    }
}
