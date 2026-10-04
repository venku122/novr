using System;
using NOVR.Diagnostics;
using NOVR.Controllers;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

Check(DiagnosticCapturePolicy.Parse("off") == DiagnosticMode.Off, "case insensitive Off");
Check(DiagnosticCapturePolicy.Parse(" Snapshot ") == DiagnosticMode.Snapshot, "trimmed mode");
Check(DiagnosticCapturePolicy.Parse("garbage") == DiagnosticMode.Off, "invalid configuration fails closed");
var off = new DiagnosticCapturePolicy(DiagnosticMode.Off, 0);
Check(!off.ShouldSample(100), "disabled diagnostics never sample");
var snapshot = new DiagnosticCapturePolicy(DiagnosticMode.Snapshot, 0);
Check(!snapshot.ShouldSample(0), "snapshot mode never samples automatically");
var session = new DiagnosticCapturePolicy(DiagnosticMode.Session, 10);
Check(session.ShouldSample(10), "initial sample");
Check(!session.ShouldSample(10.05), "sampling is bounded, independent of frame rate");
Check(session.ShouldSample(10.11), "next eligible sample");
Check(!session.ShouldSample(70), "session cannot record beyond 60 seconds");
Check(!session.ShouldSample(9), "backwards clock rejected");
var cap = new DiagnosticCapturePolicy(DiagnosticMode.Session, 0);
for (var i = 0; i < 600; i++) Check(cap.ShouldSample(i * 0.100001), "valid sample within budget");
Check(!cap.ShouldSample(59.999), "sample budget remains bounded");
Console.WriteLine("PASS: diagnostic modes, disabled cost gate, sampling interval, duration, clock and budget safety");

var gesture = new CaptureGesture();
Check(gesture.Update(true, 0) == CaptureTransition.None, "held at startup never starts microphone");
Check(gesture.Update(false, 1) == CaptureTransition.None, "release arms capture");
Check(gesture.Update(true, 2) == CaptureTransition.Start, "press starts once");
Check(gesture.Update(true, 3) == CaptureTransition.None, "hold never repeats");
Check(gesture.Update(false, 4) == CaptureTransition.Stop, "release stops once");
Check(gesture.Update(false, 5) == CaptureTransition.None, "release never repeats");
Check(gesture.Update(true, 6) == CaptureTransition.Start, "next gesture starts");
Check(gesture.Update(true, 36) == CaptureTransition.Stop, "held capture bounded to 30 seconds");
Check(gesture.Update(true, 37) == CaptureTransition.None, "timeout cannot retrigger a held key");
Check(gesture.Update(false, 38) == CaptureTransition.None, "release after timeout arms next press");
Check(gesture.Update(true, 39) == CaptureTransition.Start, "new press after timeout");
Check(gesture.Cancel() == CaptureTransition.Stop, "component disable stops active capture");
Check(gesture.Cancel() == CaptureTransition.None, "disable does not repeat stop");
Console.WriteLine("PASS: capture press/release, startup privacy, bounded hold and lifecycle cancellation");

var pointer = new ControllerUiState();
var state = pointer.Update(true, true, "Auto", .0f, false, .0f, false);
Check(state.Hand == PointerHand.Right && !state.Pressed, "auto starts with right hand");
state = pointer.Update(true, true, "Auto", .6f, false, .0f, false);
Check(state.Down && state.Pressed, "trigger down is a single edge");
state = pointer.Update(true, true, "Auto", .50f, false, 1f, false);
Check(state.Hand == PointerHand.Right && state.Pressed && !state.Down, "hysteresis and other hand cannot steal drag");
state = pointer.Update(true, true, "Auto", .4f, false, 1f, false);
Check(state.Up && !state.Pressed, "trigger release edge");
state = pointer.Update(false, true, "Auto", 0f, false, 1f, false);
Check(state.Hand == PointerHand.Left && !state.Pressed, "hand switch requires releasing held select");
pointer.Update(false, true, "Auto", 0f, false, 0f, false);
state = pointer.Update(false, true, "Auto", 0f, false, 0f, true);
Check(state.Down && state.Hand == PointerHand.Left, "single controller confirm works");
state = pointer.Update(false, false, "Auto", 0f, false, 0f, false);
Check(state.Up && state.Hand == PointerHand.None, "disconnect cancels held selection");
Check(ControllerUiState.ChooseHand(true,true,"Left",PointerHand.None,false)==PointerHand.Left,"left preference");
Check(ControllerUiState.ChooseHand(true,false,"Left",PointerHand.None,false)==PointerHand.Right,"one hand fallback");
Console.WriteLine("PASS: controller hand selection, debounce, hysteresis, drag ownership, disconnect and reconnect hold safety");

var pressGate = new PointerPressGate();
Check(pressGate.Read(true), "fresh press allowed");
pressGate.Cancel(true);
Check(!pressGate.Read(true), "off-canvas return cannot re-press while held");
pressGate.Cancel(false);
Check(!pressGate.Read(true), "repeated focus cancellation cannot unblock a held press");
Check(!pressGate.Read(false), "release emits no press");
Check(pressGate.Read(true), "new physical press allowed after release");
Console.WriteLine("PASS: pointer cancellation requires physical release before re-entry");
