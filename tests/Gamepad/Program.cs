using System;
using NOVR.Gamepad;

static class Program
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static HandInput Hand(float trigger = 0, PadButtons buttons = 0) => new HandInput { Valid = true, Trigger = trigger, Buttons = buttons };
    static int Main()
    {
        var mapper = new GamepadPolicy();
        var left = Hand(1, PadButtons.X | PadButtons.LeftShoulder | PadButtons.DpadUp);
        left.StickX = -2; left.StickY = float.NaN;
        var right = Hand(.5f, PadButtons.A | PadButtons.RightShoulder | PadButtons.Start); right.StickX = 2;
        var report = mapper.Map(true, left, right, false, false);
        Check(report.LeftX == short.MinValue && report.LeftY == 0 && report.RightX == short.MaxValue, "clamp and NaN");
        Check(report.LeftTrigger == 255 && report.RightTrigger == 128, "trigger conversion");
        Check(report.Buttons == (left.Buttons | right.Buttons), "buttons combine");
        Check(mapper.Map(false, left, right, false, false).IsNeutral, "focus neutral");
        Check(mapper.Map(true, default, right, false, false).IsNeutral, "missing hand neutral");
        mapper = new GamepadPolicy();
        Check(mapper.Map(true, left, right, false, true).RightTrigger == 0, "UI owns trigger");
        report = mapper.Map(true, left, right, false, false);
        Check(report.RightTrigger == 0 && (report.Buttons & PadButtons.A) == 0 && (report.Buttons & PadButtons.RightShoulder) != 0, "release gate only suppresses UI controls");
        Check((report.Buttons & PadButtons.Start) == 0, "UI menu release gate");
        var dpadPolicy = new GamepadPolicy();
        Check((dpadPolicy.Map(true, left, right, true, false).Buttons & PadButtons.DpadUp) == 0, "native left Dpad UI suppression");
        var navigationPolicy = new GamepadPolicy();
        Check((navigationPolicy.Map(true, left, right, false, false, true).Buttons & PadButtons.Start) == 0, "NOVR navigation reserves Start");
        Check((navigationPolicy.Map(true, left, right, false, false, false).Buttons & PadButtons.Start) == 0, "navigation release gate");
        navigationPolicy.Map(true, left, Hand(), false, false, false);
        Check((navigationPolicy.Map(true, left, right, false, false, false).Buttons & PadButtons.Start) != 0, "navigation release rearms");
        mapper.Map(true, left, Hand(), false, false);
        Check(mapper.Map(true, left, right, false, false).RightTrigger == 128, "release rearms");
        mapper.Map(true, left, right, false, true);
        mapper.Map(true, left, Hand(.02f), false, false);
        Check(mapper.Map(true, left, right, false, false).RightTrigger == 128, "small resting trigger noise permits release");
        mapper.Map(false, left, right, false, false);
        Check(mapper.Map(true, left, right, false, false).RightTrigger == 0, "focus regain held trigger gated");
        var sink = new FakeSink(); var session = new GamepadSession(() => sink);
        session.Update(false, default); Check(sink.Sends == 0, "disabled never creates");
        session.Update(true, report); Check(sink.Sends == 1, "enabled sends");
        session.Update(false, report); Check(sink.Disposals == 1 && sink.Last.IsNeutral, "disable neutral and dispose");
        session.Dispose(); Check(sink.Disposals == 1, "cleanup idempotent");
        sink = new FakeSink { FailSend = true }; session = new GamepadSession(() => sink);
        Check(session.Update(true, report) != null && sink.Disposals == 1, "send failure cleanup");
        session.Update(true, report); Check(sink.Sends == 2, "failure latches without reconnect");
        session = new GamepadSession(() => throw new Exception("missing bus"));
        Check(session.Update(true, report) != null, "missing bus graceful");
        Console.WriteLine("Gamepad policy and lifecycle tests passed."); return 0;
    }
    sealed class FakeSink : IGamepadSink
    {
        public int UserIndex => 0;
        public int Sends, Disposals; public bool FailSend; public PadReport Last;
        public void Send(PadReport report) { Sends++; Last = report; if (FailSend) throw new Exception("send"); }
        public void Dispose() { Disposals++; }
    }
}
