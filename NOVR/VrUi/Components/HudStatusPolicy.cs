namespace NOVR.VrUi.SpecialBehavior;

public enum HudStatusMode { Aircraft, Helmet, Smart }

public sealed class HudStatusPolicy
{
    private bool _helmet;
    public bool CockpitDecluttered { get; private set; }
    public void Reset() { _helmet = false; CockpitDecluttered = false; }
    public bool UseHelmet(HudStatusMode mode, float angleDegrees, float enterDegrees, bool referenceValid,
        bool cockpitDeclutter = false, float downAngleDegrees = 0, float downEnterDegrees = 35)
    {
        if (!referenceValid || float.IsNaN(angleDegrees) || float.IsInfinity(angleDegrees)) { Reset(); return false; }
        if (mode == HudStatusMode.Helmet) { CockpitDecluttered = false; return _helmet = true; }
        if (mode != HudStatusMode.Smart) { Reset(); return false; }
        if (!cockpitDeclutter) CockpitDecluttered = false;
        else
        {
            if (float.IsNaN(downAngleDegrees) || float.IsInfinity(downAngleDegrees)) { Reset(); return false; }
            if (float.IsNaN(downEnterDegrees) || float.IsInfinity(downEnterDegrees)) downEnterDegrees = 35;
            downEnterDegrees = System.Math.Max(15, System.Math.Min(75, downEnterDegrees));
            if (CockpitDecluttered) { if (downAngleDegrees <= downEnterDegrees - 5) CockpitDecluttered = false; }
            else if (downAngleDegrees >= downEnterDegrees) CockpitDecluttered = true;
            if (CockpitDecluttered) return _helmet = false;
        }
        if (float.IsNaN(enterDegrees) || float.IsInfinity(enterDegrees)) enterDegrees = 25;
        enterDegrees = System.Math.Max(10, System.Math.Min(60, enterDegrees));
        float exitDegrees = enterDegrees - 5;
        if (_helmet) { if (angleDegrees <= exitDegrees) _helmet = false; }
        else if (angleDegrees >= enterDegrees) _helmet = true;
        return _helmet;
    }
}
