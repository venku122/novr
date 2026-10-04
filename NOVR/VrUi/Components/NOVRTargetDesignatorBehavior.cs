using NOVR.Controllers;
using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

[DefaultExecutionOrder(500)]
public class NOVRTargetDesignatorBehavior : UIRenderedCanvasBehavior
{
    private const float CueDistance = 1000f;
    private CanvasGroup? _group;
    private bool _suppressed;
    private float _originalAlpha;

    public override void Awake()
    {
        base.Awake();
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
    }
    public override void OnEnable() { base.OnEnable(); Application.onBeforeRender += BeforeRender; }
    public override void OnDisable() { Application.onBeforeRender -= BeforeRender; RestoreVisibility(); base.OnDisable(); }
    private void LateUpdate() => RefreshAim();
    [BeforeRenderOrder(300)]
    private void BeforeRender() => RefreshAim();

    public bool RefreshAim()
    {
        if (!SpottingAimInput.TryGetUiRay(out var ray))
        {
            if (!_suppressed && _group != null) { _originalAlpha = _group.alpha; _suppressed = true; }
            if (_group != null) _group.alpha = 0;
            return false;
        }
        RestoreVisibility();
        transform.position = ray.GetPoint(CueDistance);
        transform.rotation = Quaternion.LookRotation(ray.direction, APIBus.CockpitHudCamera.transform.up);
        return true;
    }
    private void RestoreVisibility()
    {
        if (_suppressed && _group != null) _group.alpha = _originalAlpha;
        _suppressed = false;
    }
}
