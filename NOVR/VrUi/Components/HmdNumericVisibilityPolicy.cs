namespace NOVR.VrUi.SpecialBehavior;

internal static class HmdNumericVisibilityPolicy
{
    // This decides only the native overlap predicate. Native Refresh/SetActive
    // and settings remain responsible for rendering the four known HMD apps.
    public static bool Hide(bool nativeHide, HudStatusMode mode, bool referenceValid,
        bool followsHelmet, bool cockpitDecluttered)
    {
        if (!referenceValid) return nativeHide;
        if (mode == HudStatusMode.Helmet) return false;
        if (mode != HudStatusMode.Smart) return nativeHide;
        if (cockpitDecluttered) return true;
        return followsHelmet ? false : nativeHide;
    }
}
