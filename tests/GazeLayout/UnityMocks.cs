using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector3 { public float x, y, z; }
    public struct Quaternion { public float x, y, z, w; }
}
namespace UnityEngine.InputSystem
{
    public class InputControl
    {
        public readonly Dictionary<string, InputControl> Children = new();
        public T GetChildControl<T>(string name) where T : InputControl
        {
            if (!Children.TryGetValue(name, out var control) || control is not T value) throw new ArgumentException("Missing or incompatible child: " + name);
            return value;
        }
    }
}
namespace UnityEngine.InputSystem.Controls
{
    public sealed class ButtonControl : InputControl { public float Value; public float ReadValue() => Value; }
    public sealed class IntegerControl : InputControl { public int Value; public int ReadValue() => Value; }
    public sealed class Vector3Control : InputControl { public UnityEngine.Vector3 Value; public UnityEngine.Vector3 ReadValue() => Value; }
    public sealed class QuaternionControl : InputControl { public UnityEngine.Quaternion Value; public UnityEngine.Quaternion ReadValue() => Value; }
}
namespace UnityEngine.InputSystem.XR { public sealed class PoseControl : UnityEngine.InputSystem.InputControl { } }
namespace UnityEngine.XR.OpenXR.Input { public sealed class PoseControl : UnityEngine.InputSystem.InputControl { } }
