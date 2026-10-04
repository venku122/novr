using System;
using NOVR.VrUi.SpecialBehavior;

static class Program
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static int Main()
    {
        var policy = new HudStatusPolicy();
        Check(!policy.UseHelmet(HudStatusMode.Aircraft, 90, 25, true), "aircraft always stays registered");
        Check(policy.UseHelmet(HudStatusMode.Helmet, 0, 25, true), "helmet follows looking forward");
        Check(!policy.UseHelmet(HudStatusMode.Smart, 19, 25, true), "smart restores forward");
        Check(!policy.UseHelmet(HudStatusMode.Smart, 24, 25, true), "smart enter threshold");
        Check(policy.UseHelmet(HudStatusMode.Smart, 26, 25, true), "smart looking away");
        Check(policy.UseHelmet(HudStatusMode.Smart, 22, 25, true), "smart hysteresis holds helmet");
        Check(!policy.UseHelmet(HudStatusMode.Smart, 19, 25, true), "smart exit threshold");
        Check(!policy.UseHelmet(HudStatusMode.Helmet, 90, 25, false), "missing reference restores aircraft");
        Check(!policy.UseHelmet(HudStatusMode.Smart, float.NaN, 25, true), "invalid angle safe");
        Check(policy.UseHelmet(HudStatusMode.Smart, 26, float.NaN, true), "invalid threshold uses default");
        policy.Reset();
        for (int i=0;i<10000;i++) Check(!policy.UseHelmet(HudStatusMode.Smart, 24, 25, true), "no threshold chatter");
        Check(!policy.UseHelmet((HudStatusMode)99, 90, 25, true), "unknown mode safe");
        var declutter=new HudStatusPolicy();
        Check(!declutter.UseHelmet(HudStatusMode.Smart,40,25,true,true,36,35),"smart cockpit look-down returns aircraft");
        Check(declutter.CockpitDecluttered,"declutter diagnostic");
        Check(!declutter.UseHelmet(HudStatusMode.Smart,40,25,true,true,33,35),"downward hysteresis holds aircraft");
        Check(declutter.UseHelmet(HudStatusMode.Smart,40,25,true,true,29,35),"looking up restores smart helmet");
        Check(declutter.UseHelmet(HudStatusMode.Helmet,40,25,true,true,50,35),"explicit helmet ignores smart declutter");
        Check(!declutter.UseHelmet(HudStatusMode.Smart,40,25,true,true,40,35),"smart reenters declutter");
        Check(declutter.UseHelmet(HudStatusMode.Smart,40,25,true,false,40,35),"disabled declutter restores smart");
        Check(!declutter.CockpitDecluttered,"disabled declutter clears diagnostic");
        Check(!declutter.UseHelmet(HudStatusMode.Smart,40,25,true,true,float.NaN,35),"invalid downward angle safe");
        PlacementTests.Run();
        NumericVisibilityTests.Run();
        Console.WriteLine("HUD policy tests passed."); return 0;
    }
}
