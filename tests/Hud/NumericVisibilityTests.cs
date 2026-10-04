using System;
using System.Collections.Generic;
using NOVR.VrUi.SpecialBehavior;
using NOVR.Patches.HUD;

static class NumericVisibilityTests
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        foreach (bool native in new[] { false, true })
        {
            Check(HmdNumericVisibilityPolicy.Hide(native, HudStatusMode.Aircraft, true, true, true) == native, "aircraft preserves native visibility");
            Check(HmdNumericVisibilityPolicy.Hide(native, HudStatusMode.Helmet, false, true, false) == native, "invalid references preserve native visibility");
            Check(!HmdNumericVisibilityPolicy.Hide(native, HudStatusMode.Helmet, true, false, false), "helmet numerics remain readable");
            Check(!HmdNumericVisibilityPolicy.Hide(native, HudStatusMode.Smart, true, true, false), "smart off-boresight numerics remain readable");
            Check(HmdNumericVisibilityPolicy.Hide(native, HudStatusMode.Smart, true, false, true), "smart cockpit declutters only known numerics");
            Check(HmdNumericVisibilityPolicy.Hide(native, HudStatusMode.Smart, true, false, false) == native, "smart forward preserves native hide");
            Check(HmdNumericVisibilityPolicy.Hide(native, (HudStatusMode)99, true, true, true) == native, "unknown mode preserves native hide");
        }
        Check(HmdNumericVisibilityPolicy.Hide(false, HudStatusMode.Smart, true, true, true), "declutter wins contradictory cached helmet state");
        // Unexpected game layouts must retain every original instruction and must
        // not even invoke a replacement factory before the complete count is known.
        foreach (int count in new[] { 0, 3, 5 })
        {
            var calls = new List<string>();
            for (int i = 0; i < count; i++) calls.Add("native");
            calls.Add("unrelated");
            int replacements = 0;
            Check(!ExactCallReplacement.TryReplace(calls, x => x == "native", x => { replacements++; return "adapter"; }, 4, out var result), "unexpected call count rejected");
            Check(ReferenceEquals(calls, result) && replacements == 0, "mismatch leaves original instructions intact");
        }
        var exact = new List<string> { "native", "label", "native", "other-call", "native", "native" };
        Check(ExactCallReplacement.TryReplace(exact, x => x == "native", x => "adapter", 4, out var replaced), "four-call layout accepted");
        Check(string.Join(",", replaced) == "adapter,label,adapter,other-call,adapter,adapter", "unrelated instructions/order preserved");
        Check(exact[0] == "native", "original instruction list not modified");
    }
}
