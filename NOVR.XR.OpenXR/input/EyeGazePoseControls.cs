using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.XR.OpenXR.Input;

/// <summary>Both native PoseControl variants expose the same typed children.
/// Do not replace the global Pose layout: existing controller layouts own it.</summary>
internal sealed class EyeGazePoseControls
{
    private readonly ButtonControl _tracked;
    private readonly IntegerControl _state;
    private readonly Vector3Control _position;
    private readonly QuaternionControl _rotation;
    public string ImplementationType { get; }

    public EyeGazePoseControls(InputControl parent)
    {
        ImplementationType = parent.GetType().FullName ?? parent.GetType().Name;
        _tracked = parent.GetChildControl<ButtonControl>("isTracked");
        _state = parent.GetChildControl<IntegerControl>("trackingState");
        _position = parent.GetChildControl<Vector3Control>("position");
        _rotation = parent.GetChildControl<QuaternionControl>("rotation");
    }

    public void Read(out Vector3 position, out Quaternion rotation, out bool tracked, out uint state)
    {
        tracked = _tracked.ReadValue() > .5f;
        state = unchecked((uint)_state.ReadValue());
        position = _position.ReadValue();
        rotation = _rotation.ReadValue();
    }
}
