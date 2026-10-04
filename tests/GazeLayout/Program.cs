using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.OpenXR.Input;
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static InputControl Make(InputControl parent)
{
    parent.Children["isTracked"] = new ButtonControl { Value = 1 };
    parent.Children["trackingState"] = new IntegerControl { Value = 3 };
    parent.Children["position"] = new Vector3Control { Value = new UnityEngine.Vector3 { x = .04f, y = 1.2f, z = -.1f } };
    parent.Children["rotation"] = new QuaternionControl { Value = new UnityEngine.Quaternion { w = 1 } };
    return parent;
}
foreach (var parent in new InputControl[] { Make(new UnityEngine.InputSystem.XR.PoseControl()), Make(new UnityEngine.XR.OpenXR.Input.PoseControl()) })
{
    var reader = new EyeGazePoseControls(parent);
    reader.Read(out var position, out var rotation, out var tracked, out var state);
    Check(tracked && state == 3 && position.x == .04f && rotation.w == 1, "Both managed native pose parents read the same typed children");
    Check(reader.ImplementationType == parent.GetType().FullName, "Actual parent implementation retained for diagnostics");
    ((ButtonControl)parent.Children["isTracked"]).Value = float.NaN;
    reader.Read(out _, out _, out tracked, out _);
    Check(!tracked, "Nonfinite native tracking flag fails closed");
    ((ButtonControl)parent.Children["isTracked"]).Value = 0;
    ((IntegerControl)parent.Children["trackingState"]).Value = 0;
    reader.Read(out _, out _, out tracked, out state);
    Check(!tracked && state == 0, "Tracking loss remains visible to application validity gate");
    // Discovery happens only in constructor; native child objects stay cached.
    parent.Children.Clear();
    reader.Read(out _, out _, out _, out _);
}
var missing = Make(new InputControl()); missing.Children.Remove("rotation");
var wrong = Make(new InputControl()); wrong.Children["rotation"] = new Vector3Control();
foreach (var parent in new[] { missing, wrong })
{
    var rejected = false;
    try { _ = new EyeGazePoseControls(parent); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Missing or wrong typed native children rejected during setup");
}
Console.WriteLine("PASS: production gaze adapter reads both PoseControl parents, caches typed children, reports tracking loss, and rejects incomplete/incompatible poses");
