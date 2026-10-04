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

var switching = new ControllerUiState();
switching.Update(true, true, "Auto", 0, false, 0, false, 0, 0);
var switched = switching.Update(true, true, "Auto", 0, false, 0, false, 0, .8f);
Check(switched.Hand == PointerHand.Left, "left grip selects left pointer");
switched = switching.Update(true, true, "Auto", 0, false, 0, false, 0, 0);
Check(switched.Hand == PointerHand.Left, "grip selection persists after release");
switched = switching.Update(true, true, "Auto", 0, false, 0, false, .8f, 0);
Check(switched.Hand == PointerHand.Right, "right grip selects right pointer");
switching.Update(true, true, "Auto", .8f, false, 0, false, 0, 0);
switched = switching.Update(true, true, "Auto", .8f, false, 0, false, 0, .8f);
Check(switched.Hand == PointerHand.Right && switched.Pressed, "grip cannot steal active drag");
switching.Update(true, true, "Auto", 0, false, 0, false, 0, .8f);
switched = switching.Update(true, true, "Auto", 0, false, 0, false, 0, .8f);
Check(switched.Hand == PointerHand.Right, "held grip during drag must release before switching");
switching.Update(true, true, "Auto", 0, false, 0, false, 0, 0);
switched = switching.Update(true, true, "Auto", 0, false, 0, false, 0, .8f);
Check(switched.Hand == PointerHand.Left, "fresh grip switches after drag");
switched = switching.Update(false, false, "Auto", 0, false, 0, false, 0, 0);
switched = switching.Update(true, true, "Auto", 0, false, 0, false, 0, .8f);
Check(switched.Hand == PointerHand.Right, "reconnect held grip cannot steal pointer");
Console.WriteLine("PASS: grip hand switching, persistent selection, drag ownership and reconnect safety");

var obj = NOVR.SteamVr.FrameModelAssets.ParseObj(new System.IO.StringReader("v 0 0 0\nv 1 0 0\nv 0 1 1\nvt 0 0\nvt 1 0\nvt 0 1\nvn 0 0 1\nf -3/1/1 -2/2/1 -1/3/1\n"));
Check(obj.Positions.Length == 9 && obj.Positions[8] == -1, "OBJ reflected into Unity coordinate system");
Check(obj.Triangles[0] == 0 && obj.Triangles[1] == 2 && obj.Triangles[2] == 1, "OBJ winding reflected with coordinates");
Check(obj.Normals[2] == -1 && obj.Uv[2] == 1, "OBJ normals and UV preserved");
bool rejected = false;
try { NOVR.SteamVr.FrameModelAssets.ParseObj(new System.IO.StringReader("v 0 0 0\nf 1 2 3\n")); }
catch (System.IO.InvalidDataException) { rejected = true; }
Check(rejected, "OBJ rejects nonexistent vertices");
Console.WriteLine("PASS: controller model geometry reflection, negative indices, UV and invalid data safety");

var modelRuntime = Environment.GetEnvironmentVariable("NOVR_STEAMVR_MODEL_TEST_ROOT");
if (!string.IsNullOrWhiteSpace(modelRuntime))
{
    foreach (var left in new[] { true, false })
    {
        var asset = NOVR.SteamVr.FrameModelAssets.Read(modelRuntime, left);
        Check(asset.Positions.Length > 1000 && asset.Normals.Length == asset.Positions.Length && asset.Uv.Length * 3 == asset.Positions.Length * 2, "actual model mesh buffers valid");
        Check(asset.GripPosition.Length == 3 && asset.GripRotationXyz.Length == 3, "actual OpenXR grip metadata loaded");
        Check(asset.Texture.Length > 8 && asset.Texture[0] == 137 && asset.Texture[1] == 80, "actual model PNG loaded");
        Console.WriteLine($"PASS: installed {asset.Name} assets ({asset.Positions.Length / 3} vertices, {asset.Triangles.Length / 3} triangles)");
    }
}

var fixedHand = new ControllerUiState();
fixedHand.Update(true,true,"Right",0,false,0,false,0,0);
Check(fixedHand.Update(true,true,"Right",0,false,0,false,0,1).Hand == PointerHand.Right, "explicit pointer preference stays fixed");
var bothGrips = new ControllerUiState();
bothGrips.Update(true,true,"Auto",0,false,0,false,0,0);
Check(bothGrips.Update(true,true,"Auto",0,false,0,false,1,1).Hand == PointerHand.Right, "simultaneous grips preserve hand ownership");

CameraTrackingTests.Run();

DiagnosticJsonTests.Run();

SeatedHeadPoseTests.Run();

SpottingAimTests.Run();
