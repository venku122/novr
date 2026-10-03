using System;
using NOVR.Diagnostics;

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
