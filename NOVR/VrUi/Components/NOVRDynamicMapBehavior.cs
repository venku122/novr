using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.SpecialBehavior;

[DefaultExecutionOrder(450)]
public class NOVRDynamicMapBehavior : MonoBehaviour
{
    private Canvas? _canvas;
    private global::DynamicMap? _map;
    private HudPanelPlacement? _compact;
    private void OnEnable() => Application.onBeforeRender += BeforeRender;
    private void OnDisable() { Application.onBeforeRender -= BeforeRender; RestoreCompactPlacement(); }
    private void LateUpdate() => UpdateCompactPlacement();
    [BeforeRenderOrder(245)]
    private void BeforeRender() => UpdateCompactPlacement();
    public void RestoreCompactPlacement() { _compact?.Restore(); _compact = null; }
    private void UpdateCompactPlacement()
    {
        if (_map == null || global::DynamicMap.mapMaximized) return;
        if (NOUIManager.I == null || !NOVRHeadsetData.HeadTracked || APIBus.MainCamera == null || _map.hudMapAnchor == null)
        { RestoreCompactPlacement(); return; }
        var rect = transform as RectTransform;
        if (rect == null) return;
        bool fromHead = ModConfiguration.Instance.HudStatusMode.Value != HudStatusMode.Aircraft;
        var hud = SceneSingleton<FlightHud>.i;
        var camera = fromHead ? APIBus.CockpitHudCamera : null;
        if (fromHead && camera == null) { RestoreCompactPlacement(); return; }
        var reference = fromHead ? camera!.transform : hud != null ? hud.GetHUDCenter() : null;
        if (reference == null) { RestoreCompactPlacement(); return; }
        float width = Mathf.Max(rect.rect.width, rect.rect.height);
        if (width <= 0 || float.IsNaN(width) || float.IsInfinity(width)) { RestoreCompactPlacement(); return; }
        _compact ??= new HudPanelPlacement(transform);
        var offset = HudPeripheralLayout.MinimapOffset(fromHead);
        _compact.SetWorldLayout(reference.position + reference.rotation * offset, reference.rotation,
            HudPeripheralLayout.MinimapWorldSize(ModConfiguration.Instance.HudMinimapSize.Value) / width);
    }

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
        {
            _canvas.worldCamera = APIBus.CockpitHudCamera;
            VrCanvasHitTester.Register(_canvas);
        }

        // The game uses coordinate-math for map interaction instead of EventSystem,
        // so map graphics have raycastTarget=false by design. The VR cursor's
        // HasGraphicAtPoint check needs raycastTarget to find the map surface.
        var map = _map = GetComponent<global::DynamicMap>();
        if (map != null)
        {
            if (map.mapBackground != null)
            {
                var bgImg = map.mapBackground.GetComponent<Image>();
                if (bgImg != null) bgImg.raycastTarget = true;
            }
            if (map.mapImage != null)
            {
                var mapImg = map.mapImage.GetComponent<Image>();
                if (mapImg != null) mapImg.raycastTarget = true;
            }
        }
    }

    private void OnDestroy()
    {
        Application.onBeforeRender -= BeforeRender;
        RestoreCompactPlacement();
        if (_canvas != null)
            VrCanvasHitTester.Unregister(_canvas);
    }
}
