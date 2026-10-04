using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

[DefaultExecutionOrder(500)]
public class NOVRHMDBehavior : UIRenderedCanvasBehavior
{
    private const float Offset = 1000;
    private Transform? _speed, _altitude, _bearing, _horizon;

    public override void Awake()
    {
        base.Awake();
        // Cache known children once. The game's HeadMountedDisplay owns values,
        // settings and active state; NOVR only preserves its existing VR layout.
        _speed = FindChildStartingWith(transform, "Speed");
        _altitude = FindChildStartingWith(transform, "Altitude");
        _bearing = FindChildStartingWith(transform, "Bearing");
        _horizon = FindChildStartingWith(transform, "Artificial Horizon");
    }
    public override void OnEnable()
    {
        base.OnEnable();
        Application.onBeforeRender += BeforeRender;
    }
    public override void OnDisable()
    {
        Application.onBeforeRender -= BeforeRender;
        base.OnDisable();
    }
    private void OnDestroy() => Application.onBeforeRender -= BeforeRender;
    private void LateUpdate() => UpdateLayout();
    [BeforeRenderOrder(250)]
    private void BeforeRender() => UpdateLayout();
    private void UpdateLayout()
    {
        var manager = NOUIManager.I;
        if (manager == null) return;
        Transform? reference;
        if (ModConfiguration.Instance.HelmetHudTracking.Value == "Head")
        {
            var camera = manager.CockpitHudCamera;
            if (camera == null) return;
            reference = camera.transform;
        }
        else
        {
            var smoothed = manager.CockpitHudReference;
            if (smoothed == null) return;
            reference = smoothed.transform;
        }
        if (reference == null) return;
        transform.SetPositionAndRotation(reference.position + reference.forward * Offset, reference.rotation);
        if (ModConfiguration.Instance.HudStatusMode.Value == HudStatusMode.Aircraft)
        {
            if (_speed != null) _speed.localPosition = new Vector3(-110f, 150f, 0);
            if (_altitude != null) _altitude.localPosition = new Vector3(110f, 150f, 0);
            if (_bearing != null) _bearing.localPosition = new Vector3(0, 200f, 0);
        }
        else
        {
            PlaceNumber(_speed, reference, new Vector3(-150f, -45f, Offset));
            PlaceNumber(_altitude, reference, new Vector3(150f, -45f, Offset));
            PlaceNumber(_bearing, reference, new Vector3(0, -95f, Offset));
        }
        if (_horizon != null) _horizon.localPosition = new Vector3(0, 150f, 0);
    }
    private static void PlaceNumber(Transform? number, Transform reference, Vector3 offset)
    {
        if (number != null) number.position = reference.position + reference.rotation * offset;
    }
}
