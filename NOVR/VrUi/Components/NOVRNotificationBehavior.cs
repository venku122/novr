using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

// MessageUI owns kill feed/mission/chat; KillDisplay owns credit/rank/unlock
// notifications. Never adapt the entire gameplay/menu canvas as a visor.
[DefaultExecutionOrder(510)]
public sealed class NOVRNotificationBehavior : UIRenderedCanvasBehavior
{
    private HudHeadFollowingPlacement? _placement;
    private void InitializePlacement()
    {
        if (_placement != null) return;
        bool isMessageRoot = GetComponent<MessageUI>() != null;
        if (isMessageRoot)
        {
            // Retain the existing NOVR MessageUI plane/scale as the baseline.
            transform.localScale = new Vector3(.003f, .003f, .003f);
            transform.position = new Vector3(0, 0, 3);
        }
        var canvas = GetComponentInParent<Canvas>();
        var origin = canvas != null && canvas.transform != transform ? canvas.transform.position : new Vector3(0, 0, 3);
        _placement = new HudHeadFollowingPlacement(transform, HudNotificationLayout.Offset(isMessageRoot, transform.position, origin), restoreWorld: isMessageRoot);
    }
    public override void OnEnable() { base.OnEnable(); Application.onBeforeRender += BeforeRender; }
    public override void OnDisable() { Application.onBeforeRender -= BeforeRender; _placement?.Restore(); base.OnDisable(); }
    private void OnDestroy() { Application.onBeforeRender -= BeforeRender; _placement?.Restore(); }
    // Capture after native GameplayUI.Update establishes the canvas plane and
    // .003 scale; Start can run before that first parent update.
    private void LateUpdate() { InitializePlacement(); UpdatePose(); }
    [BeforeRenderOrder(260)]
    private void BeforeRender() => UpdatePose();
    private void UpdatePose()
    {
        if (_placement == null) return;
        var manager = NOUIManager.I;
        if (manager == null || APIBus.MainCamera == null || !NOVRHeadsetData.HeadTracked ||
            ModConfiguration.Instance.HudStatusMode.Value == HudStatusMode.Aircraft)
        { _placement.Restore(); return; }
        var camera = manager.CockpitHudCamera;
        if (camera == null) { _placement.Restore(); return; }
        _placement.Apply(camera.transform.position, camera.transform.rotation);
    }
}
