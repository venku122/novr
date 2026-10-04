using HarmonyLib;
using NOVR.VrUi.SpecialBehavior;

namespace NOVR.Patches.HUD.Map;

[HarmonyPatch(typeof(DynamicMap), "Maximize")]
internal static class DynamicMapCompactLayoutPatch
{
    [HarmonyPrefix]
    private static void BeforeMaximize(DynamicMap __instance)
    {
        // Restore BEFORE native reparent/size changes. Never restore minimized
        // coordinates over an already maximized tactical map.
        if (!DynamicMap.mapMaximized && DynamicMap.AllowedToOpen)
            __instance.GetComponent<NOVRDynamicMapBehavior>()?.RestoreCompactPlacement();
    }
}
