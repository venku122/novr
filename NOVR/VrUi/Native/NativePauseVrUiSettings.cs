using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NOVR.VrUi.Native;

/// <summary>Hosts the shared settings panel without changing the game's pause state.</summary>
public sealed class NativePauseVrUiSettings : MonoBehaviour, ICancelHandler
{
    private static NativePauseVrUiSettings? _current;
    private GameplayUI? _owner;
    private Canvas? _canvas;
    private CanvasGroup? _pauseGroup;
    private bool _addedGroup, _closed;
    private float _originalAlpha;
    private bool _originalInteractable, _originalBlocksRaycasts;
    private GameObject? _returnSelection;
    private Vector3 _anchor;
    private Quaternion _rotation;
    private float _recenterAt = -1;

    public static void Open(GameplayUI owner)
    {
        if (_current != null || owner == null || !GameplayUI.GameIsPaused || owner.menuCanvas == null) return;
        var root = new GameObject("NOVR Pause VR UI Settings", typeof(RectTransform));
        var host = root.AddComponent<NativePauseVrUiSettings>();
        _current = host;
        try { host.Initialize(owner); }
        catch (System.Exception error)
        {
            NOVRPlugin.LogSource?.LogWarning("Could not open pause VR UI settings: " + error.Message);
            host.Close();
        }
    }

    public static void CloseCurrent() => _current?.Close();
    public void OnCancel(BaseEventData eventData) { Close(); eventData.Use(); }

    private void Initialize(GameplayUI owner)
    {
        _owner = owner;
        _returnSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(2000, 1125);
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 5000;
        gameObject.AddComponent<GraphicRaycaster>();
        LayerHelper.SetLayerRecursive(transform, LayerHelper.GetVrUiLayer());
        var reference = APIBus.CockpitHudCamera.transform;
        _anchor = reference.position;
        _rotation = Quaternion.Euler(0, reference.rotation.eulerAngles.y, 0);
        UpdatePlacement();
        VrCanvasHitTester.Register(_canvas);

        var panel = gameObject.AddComponent<NativeVrUiSettingsPanel>();
        panel.Initialize(rect, Close, () => _recenterAt = Time.unscaledTime + 2);
        panel.SetVisible(true);
        // Hide only the stock pause UI; preserve native pause/flight/timer logic.
        _addedGroup = !owner.menuCanvas.TryGetComponent(out _pauseGroup);
        if (_pauseGroup == null) _pauseGroup = owner.menuCanvas.gameObject.AddComponent<CanvasGroup>();
        _originalAlpha = _pauseGroup.alpha;
        _originalInteractable = _pauseGroup.interactable;
        _originalBlocksRaycasts = _pauseGroup.blocksRaycasts;
        _pauseGroup.alpha = 0;
        _pauseGroup.interactable = false;
        _pauseGroup.blocksRaycasts = false;
        if (EventSystem.current != null)
        {
            var firstButton = panel.GetComponentInChildren<Button>(true);
            EventSystem.current.SetSelectedGameObject(firstButton != null ? firstButton.gameObject : gameObject);
        }
    }

    private void Update()
    {
        if (_closed) return;
        if (_owner == null || !GameplayUI.GameIsPaused || _owner.menuCanvas == null || !_owner.menuCanvas.enabled || GameManager.gameState == GameState.Menu)
        { Close(); return; }
        UpdatePlacement();
        if (_recenterAt >= 0 && Time.unscaledTime >= _recenterAt)
        {
            _recenterAt = -1;
            NOVRHeadsetData.CalibrateTranslation();
            NOVRHeadsetData.CalibrateRotation();
            var reference = APIBus.CockpitHudCamera.transform;
            _anchor = reference.position;
            _rotation = Quaternion.Euler(0, reference.rotation.eulerAngles.y, 0);
        }
    }

    private void UpdatePlacement()
    {
        var config = ModConfiguration.Instance;
        var distance = Mathf.Clamp(config.NativeMenuDistance.Value, 1.5f, 6);
        var height = Mathf.Clamp(config.NativeMenuHeightOffset.Value, -.25f, 1);
        transform.SetPositionAndRotation(_anchor + _rotation * Vector3.forward * distance + Vector3.up * height, _rotation);
        transform.localScale = Vector3.one * (.0015625f * Mathf.Clamp(config.NativeMenuScale.Value, .75f, 2));
        if (_canvas != null) { _canvas.worldCamera = APIBus.CockpitHudCamera; _canvas.planeDistance = distance; }
    }

    private void Close()
    {
        if (_closed) return;
        _closed = true;
        _recenterAt = -1;
        RestorePauseUi();
        if (_current == this) _current = null;
        if (_canvas != null) VrCanvasHitTester.Unregister(_canvas);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            EventSystem.current.SetSelectedGameObject(_returnSelection);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void RestorePauseUi()
    {
        if (_pauseGroup == null) return;
        _pauseGroup.alpha = _originalAlpha;
        _pauseGroup.interactable = _originalInteractable;
        _pauseGroup.blocksRaycasts = _originalBlocksRaycasts;
        if (_addedGroup) Destroy(_pauseGroup);
        _pauseGroup = null;
    }

    private void OnDestroy()
    {
        RestorePauseUi();
        if (_canvas != null) VrCanvasHitTester.Unregister(_canvas);
        if (_current == this) _current = null;
    }
}
