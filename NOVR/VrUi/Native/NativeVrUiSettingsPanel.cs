using System;
using NOVR.Controllers;
using NOVR.VrUi.SpecialBehavior;
using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.Native;

public sealed class NativeVrUiSettingsPanel : MonoBehaviour
{
    private const float DefaultScale = 1.25f;
    private const float DefaultDistance = 3.0f;
    private const float DefaultHeightOffset = 0.0f;
    private const float MinScale = 0.75f;
    private const float MaxScale = 2.0f;
    private const float MinDistance = 1.5f;
    private const float MaxDistance = 6.0f;
    private const float MinHeightOffset = -0.25f;
    private const float MaxHeightOffset = 1.0f;
    private const float DefaultMinimapOpacity = 1.0f;
    private const float MinMinimapOpacity = 0.0f;
    private const float MaxMinimapOpacity = 1.0f;

    private static readonly Color BackgroundColor = new(0.025f, 0.035f, 0.045f, 0.93f);
    private static readonly Color PanelColor = new(0.05f, 0.06f, 0.065f, 0.94f);
    private static readonly Color ButtonColor = new(0.24f, 0.29f, 0.31f, 0.96f);
    private static readonly Color BackButtonColor = new(0.62f, 0.12f, 0.14f, 0.96f);
    private static readonly Color ActionButtonColor = new(0.12f, 0.34f, 0.20f, 0.96f);
    private static readonly Color ToggleOnColor = new(0.12f, 0.34f, 0.20f, 0.96f);
    private static readonly Color ToggleOffColor = new(0.54f, 0.16f, 0.14f, 0.96f);

    private RectTransform? _container;
    private Font? _font;
    private Text? _nativeUiValueText;
    private Button? _nativeUiToggleButton;
    private Text? _environmentValueText;
    private Button? _environmentToggleButton;
    private Text? _scaleValueText;
    private Text? _distanceValueText;
    private Text? _heightValueText;
    private Text? _minimapOpacityValueText;
    private Button? _hotasModeButton;
    private Button? _gamepadModeButton;
    private Text? _eyeRequestText, _aimModeText, _gazeFeedbackText, _hudModeText, _helmetTrackingText, _trackingStatusText;
    private float _nextTrackingRefresh;
    private Text? _statusText;
    private Text? _minimapSizeText, _statusScaleText, _statusDetailText;
    private Action? _close;
    private Action? _recenter;

    public void Initialize(RectTransform root, Action close, Action recenter)
    {
        _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _close = close;
        _recenter = recenter;
        BuildLayout(root);
    }

    public void SetVisible(bool visible)
    {
        if (_container == null) return;

        if (visible)
        {
            RefreshValues();
        }

        NativePanelTransition.SetVisible(_container, visible);
    }

    private void BuildLayout(RectTransform root)
    {
        _container = CreateContainer("Native VR UI Settings", root, root.sizeDelta);
        CreateImage("Background", _container, BackgroundColor, Vector2.zero, _container.sizeDelta);
        CreateText("Header", _container, "VR UI SETTINGS", new Vector2(0f, NativeUiLayout.HeaderY), NativeUiLayout.HeaderSize, 22, TextAnchor.MiddleCenter, Color.white);

        var panel = CreatePanel("VR UI Settings Panel", _container, PanelColor, Vector2.zero, new Vector2(980f, 720f));
        CreateText("Panel Header", panel, "MENU MODE", new Vector2(0f, 310f), new Vector2(860f, 34f), 19, TextAnchor.MiddleCenter, Color.white);

        CreateToggleRow(
            panel,
            "NATIVE VR UI",
            "Use NOVR's native menu instead of the stock rendered UI.",
            new Vector2(0f, 230f),
            ToggleNativeUi,
            out _nativeUiToggleButton,
            out _nativeUiValueText);

        CreateToggleRow(
            panel,
            "3D MENU ENVIRONMENT",
            "Show an experimental scene behind the native menu.",
            new Vector2(0f, 140f),
            ToggleEnvironment,
            out _environmentToggleButton,
            out _environmentValueText);

        CreateText("Placement Header", panel, "PLACEMENT", new Vector2(0f, 50f), new Vector2(860f, 30f), 17, TextAnchor.MiddleCenter, new Color(0.84f, 0.90f, 0.92f, 1f));

        CreateSettingRow(
            panel,
            "SCALE",
            "Overall native menu size.",
            new Vector2(0f, -30f),
            () => ChangeScale(-0.05f),
            () => ChangeScale(0.05f),
            out _scaleValueText);

        CreateSettingRow(
            panel,
            "DISTANCE",
            "Meters from your headset when opened or recentered.",
            new Vector2(0f, -125f),
            () => ChangeDistance(-0.1f),
            () => ChangeDistance(0.1f),
            out _distanceValueText);

        CreateSettingRow(
            panel,
            "HEIGHT OFFSET",
            "Vertical offset in meters relative to your headset.",
            new Vector2(0f, -220f),
            () => ChangeHeightOffset(-0.05f),
            () => ChangeHeightOffset(0.05f),
            out _heightValueText);

        CreateText("Map Header", panel, "MAP", new Vector2(0f, -315f), new Vector2(860f, 30f), 17, TextAnchor.MiddleCenter, new Color(0.84f, 0.90f, 0.92f, 1f));

        CreateSettingRow(
            panel,
            "MINIMAP OPACITY",
            "Opacity of the small minimap in the HUD (not the full map).",
            new Vector2(0f, -395f),
            () => ChangeMinimapOpacity(-0.05f),
            () => ChangeMinimapOpacity(0.05f),
            out _minimapOpacityValueText);

        CreateMenuButton("RESET DEFAULTS", panel, new Vector2(-160f, -310f), new Vector2(220f, 42f), ButtonColor, ResetDefaults, 13);
        CreateMenuButton("RECENTER", panel, new Vector2(160f, -310f), new Vector2(180f, 42f), ActionButtonColor, Recenter, 14);
        _statusText = CreateText("Status", panel, "", new Vector2(0f, -340f), new Vector2(860f, 34f), 13, TextAnchor.MiddleCenter, new Color(0.84f, 0.90f, 0.92f, 1f));

        var controls = CreatePanel("Controller Mode Panel", _container, PanelColor,
            new Vector2(745f, 170f), new Vector2(420f, 300f));
        CreateText("Controller Mode Header", controls, "CONTROLLER MODE", new Vector2(0f, 112f),
            new Vector2(380f, 30f), 19, TextAnchor.MiddleCenter, Color.white);
        _hotasModeButton = CreateMenuButton("UI + HOTAS", controls, new Vector2(0f, 48f),
            new Vector2(350f, 48f), ButtonColor, () => SetControllerMode(ControllerUseMode.UiHotas));
        _gamepadModeButton = CreateMenuButton("UI + GAMEPAD", controls, new Vector2(0f, -12f),
            new Vector2(350f, 48f), ButtonColor, () => SetControllerMode(ControllerUseMode.UiGamepad));
        CreateText("Controller Mode Description", controls,
            "UI + HOTAS: controllers operate menus.\nUI + GAMEPAD: also use normal flight bindings.\nHOTAS stays available in both modes.",
            new Vector2(0f, -92f), new Vector2(390f, 72f), 13, TextAnchor.MiddleCenter, Color.white);

        var hudLayout = CreatePanel("HUD Layout Panel", _container, PanelColor,
            new Vector2(745f, -240f), new Vector2(420f, 440f));
        CreateText("HUD Layout Header", hudLayout, "PERIPHERAL HUD", new Vector2(0, 178), new Vector2(380, 30), 19, TextAnchor.MiddleCenter, Color.white);
        CreateText("Minimap Size Label", hudLayout, "MINIMAP SIZE (FULL MAP UNCHANGED)", new Vector2(0, 120), new Vector2(390, 26), 13, TextAnchor.MiddleCenter, Color.white);
        CreateMenuButton("-", hudLayout, new Vector2(-145, 75), new Vector2(54, 40), ButtonColor, () => ChangeMinimapSize(-1));
        _minimapSizeText = CreateText("Minimap Size Value", hudLayout, "", new Vector2(0, 75), new Vector2(170, 38), 16, TextAnchor.MiddleCenter, Color.white);
        CreateMenuButton("+", hudLayout, new Vector2(145, 75), new Vector2(54, 40), ButtonColor, () => ChangeMinimapSize(1));
        CreateText("Status Size Label", hudLayout, "HELMET STATUS SIZE", new Vector2(0, 15), new Vector2(390, 26), 13, TextAnchor.MiddleCenter, Color.white);
        CreateMenuButton("-", hudLayout, new Vector2(-145, -30), new Vector2(54, 40), ButtonColor, () => ChangeStatusSize(-.05f));
        _statusScaleText = CreateText("Status Size Value", hudLayout, "", new Vector2(0, -30), new Vector2(170, 38), 16, TextAnchor.MiddleCenter, Color.white);
        CreateMenuButton("+", hudLayout, new Vector2(145, -30), new Vector2(54, 40), ButtonColor, () => ChangeStatusSize(.05f));
        _statusDetailText = CreateMenuButton("", hudLayout, new Vector2(0, -105), new Vector2(350, 44), ButtonColor, ToggleStatusDetail).GetComponentInChildren<Text>();
        CreateText("HUD Layout Help", hudLayout, "Compact Smart: weapon and numbers.\nFull: additional status follows the helmet.", new Vector2(0, -170), new Vector2(390, 60), 13, TextAnchor.MiddleCenter, Color.white);

        var sight = CreatePanel("Sight and Helmet Panel", _container, PanelColor,
            new Vector2(-745f, 0f), new Vector2(420f, 840f));
        CreateText("Sight Header", sight, "SIGHT AND HELMET", new Vector2(0f, 380f),
            new Vector2(380f, 30f), 19, TextAnchor.MiddleCenter, Color.white);
        _eyeRequestText = CreateMenuButton("", sight, new Vector2(0f, 305f), new Vector2(350f, 44f),
            ButtonColor, ToggleEyeTracking).GetComponentInChildren<Text>();
        _aimModeText = CreateMenuButton("", sight, new Vector2(0f, 235f), new Vector2(350f, 44f),
            ButtonColor, CycleSpottingAim).GetComponentInChildren<Text>();
        _gazeFeedbackText = CreateMenuButton("", sight, new Vector2(0f, 165f), new Vector2(350f, 44f),
            ButtonColor, ToggleGazeFeedback).GetComponentInChildren<Text>();
        _hudModeText = CreateMenuButton("", sight, new Vector2(0f, 95f), new Vector2(350f, 44f),
            ButtonColor, CycleHudStatusMode).GetComponentInChildren<Text>();
        _helmetTrackingText = CreateMenuButton("", sight, new Vector2(0f, 25f), new Vector2(350f, 44f),
            ButtonColor, ToggleHelmetTracking).GetComponentInChildren<Text>();
        CreateText("Sight Help", sight,
            "Eye Preferred uses gaze when available.\nUse your normal Select control to designate.\nSmart HUD brings status into view looking away.\nAiming and flightpath symbols stay aligned.",
            new Vector2(0f, -100f), new Vector2(390f, 130f), 14, TextAnchor.MiddleCenter, Color.white);
        _trackingStatusText = CreateText("Tracking Status", sight, "", new Vector2(0f, -225f),
            new Vector2(390f, 90f), 13, TextAnchor.MiddleCenter, Color.white);
        CreateText("Eye Sharing Help", sight,
            "Eye tracking needs runtime gaze sharing.\nRestart after enabling eye tracking.\nLooking never auto-selects or fires.",
            new Vector2(0f, -345f), new Vector2(390f, 90f), 13, TextAnchor.MiddleCenter, Color.white);

        CreateMenuButton("BACK", _container, new Vector2(NativeUiLayout.FooterLeftX, NativeUiLayout.FooterY), NativeUiLayout.FooterButtonSize, BackButtonColor, Close, 15);
        NativePanelTransition.SetVisible(_container, false, instant: true);
    }

    private void CreateToggleRow(
        RectTransform parent,
        string label,
        string description,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction onClick,
        out Button toggleButton,
        out Text valueText)
    {
        var row = CreatePanel($"{label} Row", parent, new Color(0.08f, 0.095f, 0.105f, 0.72f), anchoredPosition, new Vector2(860f, 82f));
        CreateText($"{label} Label", row, label, new Vector2(-280f, 16f), new Vector2(250f, 28f), 17, TextAnchor.MiddleLeft, Color.white);
        CreateText($"{label} Description", row, description, new Vector2(-280f, -18f), new Vector2(430f, 28f), 12, TextAnchor.MiddleLeft, new Color(0.75f, 0.82f, 0.84f, 1f));
        toggleButton = CreateMenuButton("ON", row, new Vector2(290f, 0f), new Vector2(150f, 40f), ToggleOnColor, onClick, 15);
        valueText = toggleButton.GetComponentInChildren<Text>();
    }

    private void CreateSettingRow(
        RectTransform parent,
        string label,
        string description,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction decrease,
        UnityEngine.Events.UnityAction increase,
        out Text valueText)
    {
        var row = CreatePanel($"{label} Row", parent, new Color(0.08f, 0.095f, 0.105f, 0.72f), anchoredPosition, new Vector2(860f, 82f));
        CreateText($"{label} Label", row, label, new Vector2(-280f, 16f), new Vector2(220f, 28f), 17, TextAnchor.MiddleLeft, Color.white);
        CreateText($"{label} Description", row, description, new Vector2(-280f, -18f), new Vector2(360f, 28f), 12, TextAnchor.MiddleLeft, new Color(0.75f, 0.82f, 0.84f, 1f));
        CreateMenuButton("-", row, new Vector2(120f, 0f), new Vector2(54f, 38f), ButtonColor, decrease, 18);
        valueText = CreateText($"{label} Value", row, "", new Vector2(230f, 0f), new Vector2(130f, 38f), 16, TextAnchor.MiddleCenter, Color.white);
        CreateMenuButton("+", row, new Vector2(340f, 0f), new Vector2(54f, 38f), ButtonColor, increase, 18);
    }

    private void ChangeScale(float delta)
    {
        var config = ModConfiguration.Instance;
        var value = RoundToStep(Mathf.Clamp(config.NativeMenuScale.Value + delta, MinScale, MaxScale), 0.05f);
        config.NativeMenuScale.Value = value;
        SaveAndRefresh("Scale updated.");
    }

    private void ChangeDistance(float delta)
    {
        var config = ModConfiguration.Instance;
        var value = RoundToStep(Mathf.Clamp(config.NativeMenuDistance.Value + delta, MinDistance, MaxDistance), 0.1f);
        config.NativeMenuDistance.Value = value;
        SaveAndRefresh("Distance updated.");
    }

    private void ChangeHeightOffset(float delta)
    {
        var config = ModConfiguration.Instance;
        var value = RoundToStep(Mathf.Clamp(config.NativeMenuHeightOffset.Value + delta, MinHeightOffset, MaxHeightOffset), 0.05f);
        config.NativeMenuHeightOffset.Value = value;
        SaveAndRefresh("Height offset updated.");
    }

    private void ChangeMinimapOpacity(float delta)
    {
        var config = ModConfiguration.Instance;
        var value = RoundToStep(Mathf.Clamp(config.HudMinimapOpacity.Value + delta, MinMinimapOpacity, MaxMinimapOpacity), 0.05f);
        config.HudMinimapOpacity.Value = value;
        SaveAndRefresh("Minimap opacity updated.");
    }
    private void ChangeMinimapSize(float delta)
    {
        var config = ModConfiguration.Instance;
        config.HudMinimapSize.Value = HudPeripheralLayout.MinimapDegrees(config.HudMinimapSize.Value + delta);
        SaveAndRefresh("Minimap size updated. Full tactical map unchanged.");
    }
    private void ChangeStatusSize(float delta)
    {
        var config = ModConfiguration.Instance;
        config.HudStatusScale.Value = RoundToStep(HudPeripheralLayout.StatusScale(config.HudStatusScale.Value + delta), .05f);
        SaveAndRefresh("Helmet status size updated.");
    }
    private void ToggleStatusDetail()
    {
        var config = ModConfiguration.Instance;
        config.HudStatusDetail.Value = config.HudStatusDetail.Value == HudStatusDetail.Compact ? HudStatusDetail.Full : HudStatusDetail.Compact;
        SaveAndRefresh("Smart status detail updated.");
    }

    private void ResetDefaults()
    {
        var config = ModConfiguration.Instance;
        config.NativeMenuScale.Value = DefaultScale;
        config.NativeMenuDistance.Value = DefaultDistance;
        config.NativeMenuHeightOffset.Value = DefaultHeightOffset;
        config.HudMinimapOpacity.Value = DefaultMinimapOpacity;
        config.HudMinimapSize.Value = 12;
        config.HudStatusScale.Value = .9f;
        config.HudStatusDetail.Value = HudStatusDetail.Compact;
        config.ControllerMode.Value = ControllerUseMode.UiHotas;
        SaveAndRefresh("VR UI settings reset.");
    }

    private void SetControllerMode(ControllerUseMode mode)
    {
        ModConfiguration.Instance.ControllerMode.Value = mode;
        SaveAndRefresh(mode == ControllerUseMode.UiGamepad
            ? "UI + Gamepad selected. Use Nuclear Option's gamepad flight bindings."
            : "UI + HOTAS selected. Controllers operate menus only.");
    }

    private void ToggleEyeTracking()
    {
        var config = ModConfiguration.Instance;
        config.EnableEyeTracking.Value = !config.EnableEyeTracking.Value;
        SaveAndRefresh(config.EnableEyeTracking.Value ? "Eye tracking requested. Restart to enable; runtime must share gaze." : "Eye tracking disabled. Eye Preferred falls back to head.");
    }

    private void CycleSpottingAim()
    {
        var config = ModConfiguration.Instance;
        config.SpottingAim.Value = config.SpottingAim.Value switch
        {
            SpottingAimMode.Head => SpottingAimMode.EyePreferred,
            SpottingAimMode.EyePreferred => SpottingAimMode.EyeOnly,
            _ => SpottingAimMode.Head,
        };
        SaveAndRefresh("Spotting aim updated. Normal Select binding still applies.");
    }

    private void ToggleGazeFeedback()
    {
        var config = ModConfiguration.Instance;
        config.ShowGazeReticle.Value = !config.ShowGazeReticle.Value;
        SaveAndRefresh("Gaze feedback updated.");
    }

    private void CycleHudStatusMode()
    {
        var config = ModConfiguration.Instance;
        config.HudStatusMode.Value = config.HudStatusMode.Value switch
        {
            HudStatusMode.Aircraft => HudStatusMode.Helmet,
            HudStatusMode.Helmet => HudStatusMode.Smart,
            _ => HudStatusMode.Aircraft,
        };
        SaveAndRefresh("Status placement updated. Aiming and flightpath symbols remain aligned.");
    }

    private void ToggleHelmetTracking()
    {
        var config = ModConfiguration.Instance;
        config.HelmetHudTracking.Value = config.HelmetHudTracking.Value == "Head" ? "Smoothed" : "Head";
        SaveAndRefresh("Helmet display tracking updated.");
    }

    private void Update()
    {
        if (_container == null || !_container.gameObject.activeInHierarchy || Time.unscaledTime < _nextTrackingRefresh) return;
        _nextTrackingRefresh = Time.unscaledTime + .5f;
        RefreshTrackingStatus();
    }

    private void RefreshTrackingStatus()
    {
        if (_trackingStatusText == null) return;
        EyeGazeInput.TryGetTrackingPose(out _, out _, out var gazeStatus);
        var source = SpottingAimInput.Source;
        var gazeLabel = gazeStatus == "tracked" ? "Eyes tracked" : gazeStatus == "disabled" ? "Eye tracking off"
            : gazeStatus == "application-unfocused" ? "Game not focused"
            : gazeStatus.StartsWith("extension-not-enabled") ? "Restart needed or gaze unsupported"
            : gazeStatus.StartsWith("device-unavailable") ? "Gaze unavailable; check sharing"
            : "Eyes not tracked; check sharing/calibration";
        var sourceLabel = source == SpottingAimSource.Eye ? "Eye cue" : source == SpottingAimSource.Head ? "Head cue" : "Cue inactive";
        _trackingStatusText.text = $"{gazeLabel}\n{sourceLabel}";
    }

    private void ToggleNativeUi()
    {
        var config = ModConfiguration.Instance;
        var nextValue = !config.EnableNativeMenuUi.Value;
        config.EnableNativeMenuUi.Value = nextValue;
        config.Config.Save();
        RefreshValues();
        SetStatus(nextValue
            ? "Native VR UI enabled."
            : "Native VR UI disabled. Use the headset VR UI ON button to return.");
    }

    private void ToggleEnvironment()
    {
        var config = ModConfiguration.Instance;
        var nextValue = !config.EnableNativeMenuEnvironment.Value;
        config.EnableNativeMenuEnvironment.Value = nextValue;
        config.Config.Save();
        RefreshValues();
        SetStatus(nextValue
            ? "3D menu environment enabled."
            : "3D menu environment disabled.");
    }

    private void Recenter()
    {
        _recenter?.Invoke();
        SetStatus("Recenter queued. Look where you want the menu.");
    }

    private void Close()
    {
        _close?.Invoke();
    }

    private void SaveAndRefresh(string status)
    {
        ModConfiguration.Instance.Config.Save();
        RefreshValues();
        SetStatus(status);
    }

    private void RefreshValues()
    {
        var config = ModConfiguration.Instance;
        RefreshNativeUiToggle(config.EnableNativeMenuUi.Value);
        if (_eyeRequestText != null) _eyeRequestText.text = "EYE TRACKING: " + (config.EnableEyeTracking.Value ? "ON" : "OFF");
        if (_aimModeText != null) _aimModeText.text = "SIGHT: " + (config.SpottingAim.Value == SpottingAimMode.EyePreferred ? "EYE PREFERRED" : config.SpottingAim.Value == SpottingAimMode.EyeOnly ? "EYE ONLY" : "HEAD");
        if (_gazeFeedbackText != null) _gazeFeedbackText.text = "GAZE FEEDBACK: " + (config.ShowGazeReticle.Value ? "ON" : "OFF");
        if (_hudModeText != null) _hudModeText.text = "STATUS: " + config.HudStatusMode.Value.ToString().ToUpperInvariant();
        if (_helmetTrackingText != null) _helmetTrackingText.text = "HELMET: " + config.HelmetHudTracking.Value.ToUpperInvariant();
        RefreshTrackingStatus();
        var gamepad = config.ControllerMode.Value == ControllerUseMode.UiGamepad;
        if (_hotasModeButton != null) NativeButtonFeedback.SetNormalColor(_hotasModeButton, gamepad ? ButtonColor : ToggleOnColor);
        if (_gamepadModeButton != null) NativeButtonFeedback.SetNormalColor(_gamepadModeButton, gamepad ? ToggleOnColor : ButtonColor);
        RefreshEnvironmentToggle(config.EnableNativeMenuEnvironment.Value);
        if (_scaleValueText != null) _scaleValueText.text = $"{config.NativeMenuScale.Value:0.00}x";
        if (_distanceValueText != null) _distanceValueText.text = $"{config.NativeMenuDistance.Value:0.0} m";
        if (_heightValueText != null) _heightValueText.text = $"{config.NativeMenuHeightOffset.Value:+0.00;-0.00;0.00} m";
        if (_minimapOpacityValueText != null) _minimapOpacityValueText.text = $"{Mathf.RoundToInt(config.HudMinimapOpacity.Value * 100f)}%";
        if (_minimapSizeText != null) _minimapSizeText.text = $"{HudPeripheralLayout.MinimapDegrees(config.HudMinimapSize.Value):0}°";
        if (_statusScaleText != null) _statusScaleText.text = $"{HudPeripheralLayout.StatusScale(config.HudStatusScale.Value):0.00}";
        if (_statusDetailText != null) _statusDetailText.text = "SMART DETAIL: " + config.HudStatusDetail.Value.ToString().ToUpperInvariant();
    }

    private void RefreshNativeUiToggle(bool enabled)
    {
        if (_nativeUiValueText != null)
        {
            _nativeUiValueText.text = enabled ? "ON" : "OFF";
        }

        if (_nativeUiToggleButton != null)
        {
            NativeButtonFeedback.SetNormalColor(_nativeUiToggleButton, enabled ? ToggleOnColor : ToggleOffColor);
        }
    }

    private void RefreshEnvironmentToggle(bool enabled)
    {
        if (_environmentValueText != null)
        {
            _environmentValueText.text = enabled ? "ON" : "OFF";
        }

        if (_environmentToggleButton != null)
        {
            NativeButtonFeedback.SetNormalColor(_environmentToggleButton, enabled ? ToggleOnColor : ToggleOffColor);
        }
    }

    private void SetStatus(string status)
    {
        if (_statusText != null)
        {
            _statusText.text = status;
        }
    }

    private static float RoundToStep(float value, float step)
    {
        return Mathf.Round(value / step) * step;
    }

    private RectTransform CreateContainer(string name, RectTransform parent, Vector2 size)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        LayerHelper.SetLayerRecursive(gameObject.transform, LayerHelper.GetVrUiLayer());

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateImage(name, parent, color, anchoredPosition, size);
    }

    private RectTransform CreateImage(string name, RectTransform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        LayerHelper.SetLayerRecursive(gameObject.transform, LayerHelper.GetVrUiLayer());

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var image = gameObject.AddComponent<Image>();
        image.color = color;
        return rectTransform;
    }

    private Text CreateText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        LayerHelper.SetLayerRecursive(gameObject.transform, LayerHelper.GetVrUiLayer());

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var textComponent = gameObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = _font;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = color;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private Button CreateMenuButton(string label, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick, int fontSize = 15)
    {
        var rectTransform = CreateImage(label, parent, color, anchoredPosition, size);
        var button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = rectTransform.GetComponent<Image>();
        button.onClick.AddListener(onClick);
        NativeButtonFeedback.Configure(button, color);
        CreateText($"{label} Text", rectTransform, label, Vector2.zero, size, fontSize, TextAnchor.MiddleCenter, Color.white);
        return button;
    }
}
