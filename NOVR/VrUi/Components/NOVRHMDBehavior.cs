using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

[DefaultExecutionOrder(500)]
public class NOVRHMDBehavior : UIRenderedCanvasBehavior
{
    private const float Offset = 1000;
    private Transform? _speed, _altitude, _bearing, _horizon;
    private Vector3 _speedScale, _altitudeScale, _bearingScale;

    public override void Awake()
    {
        base.Awake();
        // Cache known children once. The game's HeadMountedDisplay owns values,
        // settings and active state; NOVR only preserves its existing VR layout.
        _speed = FindChildStartingWith(transform, "Speed");
        _altitude = FindChildStartingWith(transform, "Altitude");
        _bearing = FindChildStartingWith(transform, "Bearing");
        _horizon = FindChildStartingWith(transform, "Artificial Horizon");
        _speedScale = _speed != null ? _speed.localScale : Vector3.one;
        _altitudeScale = _altitude != null ? _altitude.localScale : Vector3.one;
        _bearingScale = _bearing != null ? _bearing.localScale : Vector3.one;
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
            bool compact = HudPeripheralLayout.Compact(ModConfiguration.Instance.HudStatusMode.Value, ModConfiguration.Instance.HudStatusDetail.Value);
            PlaceNumber(_speed, reference, compact ? HudPeripheralLayout.NumberOffset(0) : new Vector3(-150f, -45f, Offset));
            PlaceNumber(_altitude, reference, compact ? HudPeripheralLayout.NumberOffset(1) : new Vector3(150f, -45f, Offset));
            PlaceNumber(_bearing, reference, compact ? HudPeripheralLayout.NumberOffset(2) : new Vector3(0, -95f, Offset));
            ApplyNumberScale(_speed, _speedScale, compact); ApplyNumberScale(_altitude, _altitudeScale, compact); ApplyNumberScale(_bearing, _bearingScale, compact);
        }
        if (ModConfiguration.Instance.HudStatusMode.Value == HudStatusMode.Aircraft)
        { ApplyNumberScale(_speed, _speedScale, false); ApplyNumberScale(_altitude, _altitudeScale, false); ApplyNumberScale(_bearing, _bearingScale, false); }
        if (_horizon != null) _horizon.localPosition = new Vector3(0, 150f, 0);
    }
    private static void ApplyNumberScale(Transform? number, Vector3 original, bool compact)
    {
        if (number == null) return;
        if (!compact || number.parent == null) { number.localScale = original; return; }
        var inherited = number.parent.lossyScale;
        if (Mathf.Abs(inherited.x) < .00001f || Mathf.Abs(inherited.y) < .00001f || Mathf.Abs(inherited.z) < .00001f) return;
        var scale = HudPeripheralLayout.StatusScale(ModConfiguration.Instance.HudStatusScale.Value);
        number.localScale = new Vector3(original.x * scale / inherited.x, original.y * scale / inherited.y, original.z * scale / inherited.z);
    }
    private static void PlaceNumber(Transform? number, Transform reference, Vector3 offset)
    {
        if (number != null) number.position = reference.position + reference.rotation * offset;
    }
}
