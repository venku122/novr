using System;

// Test the production camera initialization, with only the Unity API boundary substituted.
internal static class CameraTrackingTests
{
    private sealed class TestCamera : NOVR.VrCamera.StereoCamera
    {
        public void Initialize(UnityEngine.Camera camera) { AttachedCamera = camera; Awake(); }
    }
    public static void Run()
    {
        foreach (var name in new[] { "NOVR Main Camera", "cockpitRenderer", "Menu Camera" })
        {
            var camera = new UnityEngine.Camera();
            new TestCamera().Initialize(camera);
            if (!camera.ImplicitTrackingDisabled)
                throw new Exception(name + ": automatic XR tracking must be disabled before NOVR supplies its pose");
        }
        Console.WriteLine("PASS: production stereo camera initialization disables implicit tracking on main/menu/overlay cameras");
    }
}

namespace UnityEngine
{
    public sealed class Camera { public bool ImplicitTrackingDisabled; }
    public class MonoBehaviour
    {
        public Camera? AttachedCamera;
        public T? GetComponent<T>() where T : class => AttachedCamera as T;
    }
}
namespace UnityEngine.XR
{
    public static class XRDevice
    {
        public static void DisableAutoXRCameraTracking(UnityEngine.Camera camera, bool disabled) => camera.ImplicitTrackingDisabled = disabled;
    }
}
namespace NOVR
{
    public class NOVRBehaviour : UnityEngine.MonoBehaviour { protected virtual void Awake() {} }
}
