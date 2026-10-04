using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using NOVR.Controllers;
using UnityEngine.InputSystem.Controls;
using PoseControl = UnityEngine.XR.OpenXR.Input.PoseControl;

namespace NOVR.VrUi
{
    internal static class VrControllerInput
    {
        private static bool _leftTriggerWasPressedCache;
        private static bool _rightTriggerWasPressedCache;

        private static InputAction? _rightAimPos;
        private static InputAction? _rightAimRot;
        private static InputAction? _leftAimPos;
        private static InputAction? _leftAimRot;
        private static InputAction? _rightTrigger;
        private static InputAction? _leftTrigger;
        private static InputAction? _rightGrip, _leftGrip;
        private static InputAction? _headPos;
        private static InputAction? _headRot;
        private static bool _actionsInitialized;
        private static InputAction? _rightConfirm, _leftConfirm, _rightBack, _leftBack, _rightScroll, _leftScroll, _rightMenu, _leftMenu;
        private static readonly ControllerUiState UiState = new();
        private static ControllerUiFrame _uiFrame;
        private static bool _rightBackWasHeld, _leftBackWasHeld;
        private static bool _menuWasHeld;
        public static bool MenuDown { get; private set; }
        public static bool BackDown { get; private set; }
        public static ControllerUiFrame UiFrame { get { EnsureFrame(); return _uiFrame; } }
        public static Vector2 UiScroll { get { EnsureFrame(); var action = _uiFrame.Hand == PointerHand.Left ? _leftScroll : _rightScroll; return _uiFrame.Hand == PointerHand.None || action == null ? Vector2.zero : action.ReadValue<Vector2>(); } }

        // Read only: do not initialize actions or alter their bindings for diagnostics.
        internal static string[] GetDiagnosticBindings()
        {
            var result = new List<string>();
            var actions = new (string Name, InputAction? Action)[]
            {
                ("rightAimPosition", _rightAimPos), ("rightAimRotation", _rightAimRot),
                ("leftAimPosition", _leftAimPos), ("leftAimRotation", _leftAimRot),
                ("rightTrigger", _rightTrigger), ("leftTrigger", _leftTrigger),
                ("rightConfirm", _rightConfirm), ("leftConfirm", _leftConfirm),
                ("rightBack", _rightBack), ("leftBack", _leftBack),
                ("rightScroll", _rightScroll), ("leftScroll", _leftScroll),
                ("rightMenu", _rightMenu), ("leftMenu", _leftMenu),
                ("rightGrip", _rightGrip), ("leftGrip", _leftGrip),
                ("headPosition", _headPos), ("headRotation", _headRot)
            };
            foreach (var item in actions)
            {
                result.Add(item.Name + ": " + (item.Action == null ? "not-initialized" : $"enabled={item.Action.enabled}; resolvedControls={item.Action.controls.Count}"));
                if (item.Action == null) continue;
                foreach (var binding in item.Action.bindings) result.Add(item.Name + " binding=" + binding.effectivePath);
                foreach (var control in item.Action.controls) result.Add(item.Name + " resolved=" + control.path);
            }
            result.Add($"UI pointer hand={_uiFrame.Hand}; pressed={_uiFrame.Pressed}; configuredHand={ModConfiguration.Instance.PointerHand.Value}");
            return result.ToArray();
        }

        // Frame cache — all raw tracking data read once per frame
        private static int _cachedFrame = -1;
        private static bool _rightValid, _leftValid, _headValid;
        private static Vector3 _rawRightPos, _rawLeftPos, _rawHeadPos;
        private static Quaternion _rawRightRot, _rawLeftRot, _rawHeadRot;
        private static float _rawRightTrigger, _rawLeftTrigger;

        // Per-frame edge detection — GetTriggerWasPressedThisFrame and
        // GetTriggerWasReleasedThisFrame are idempotent within a frame so a
        // consumer (e.g. VrUiCursor) can poll them from multiple call sites in
        // the same Update without the second call seeing a cache the first
        // call already advanced.
        private static int _triggerEdgeFrame = -1;
        private static bool _leftTriggerPressedThisFrame;
        private static bool _rightTriggerPressedThisFrame;
        private static bool _leftTriggerReleasedThisFrame;
        private static bool _rightTriggerReleasedThisFrame;

        // Filtered tracking-space poses
        private static Vector3 _filtRightPos, _filtLeftPos;
        private static Quaternion _filtRightRot, _filtLeftRot;
        private static bool _filtInitialized;

        // Filter instances (lazy-created)
        private static OneEuroQuaternionFilter? _rightRotFilter;
        private static OneEuroQuaternionFilter? _leftRotFilter;

        // XR Rig transform (lazy, refreshed periodically)
        private static Transform? _xrRig;
        private static int _rigSearchFrame;

        // Throttled diagnostic logging
        private static float _lastDiagLogTime = -100f;
        private const float DiagLogInterval = 1f;

        // --- Internal: cache/refresh poses once per frame ---
        private static void EnsureFrame()
        {
            if (Time.frameCount == _cachedFrame)
                return;
            _cachedFrame = Time.frameCount;

            EnsureActions();

            // Read raw tracking data
            _rightValid = TryReadRawPose(_rightAimPos, _rightAimRot, out _rawRightPos, out _rawRightRot);
            _leftValid = TryReadRawPose(_leftAimPos, _leftAimRot, out _rawLeftPos, out _rawLeftRot);
            _headValid = TryReadRawPose(_headPos, _headRot, out _rawHeadPos, out _rawHeadRot);
            _rawRightTrigger = TryReadFloat(_rightTrigger);
            _rawLeftTrigger = TryReadFloat(_leftTrigger);
            _uiFrame = UiState.Update(_rightValid, _leftValid, ModConfiguration.Instance.PointerHand.Value,
                _rawRightTrigger, TryReadFloat(_rightConfirm) > .5f, _rawLeftTrigger, TryReadFloat(_leftConfirm) > .5f,
                TryReadFloat(_rightGrip), TryReadFloat(_leftGrip));
            var rightBack = _rightValid && TryReadFloat(_rightBack) > .5f;
            var leftBack = _leftValid && TryReadFloat(_leftBack) > .5f;
            BackDown = _uiFrame.Hand == PointerHand.Right ? rightBack && !_rightBackWasHeld : _uiFrame.Hand == PointerHand.Left && leftBack && !_leftBackWasHeld;
            var menuHeld = (_rightValid && TryReadFloat(_rightMenu) > .5f) || (_leftValid && TryReadFloat(_leftMenu) > .5f);
            MenuDown = menuHeld && !_menuWasHeld; _menuWasHeld = menuHeld;
            _rightBackWasHeld = rightBack; _leftBackWasHeld = leftBack;
            if (!_rightValid) _rightRotFilter = null;
            if (!_leftValid) _leftRotFilter = null;

            if (!ModConfiguration.Instance.ControllerPoseSmoothing.Value)
            {
                _filtRightPos = _rawRightPos; _filtRightRot = _rawRightRot;
                _filtLeftPos = _rawLeftPos; _filtLeftRot = _rawLeftRot;
                _rightRotFilter = _leftRotFilter = null; _filtInitialized = false;
            }
            else
            {
            // Initialize filters on first valid data
            if (!_filtInitialized)
            {
                if (_rightValid)
                {
                    _rightRotFilter = new OneEuroQuaternionFilter(1.2f, 0.15f, 1f);
                    _rightRotFilter.Reset(_rawRightRot);
                    _filtRightRot = _rawRightRot;
                    _filtRightPos = _rawRightPos;
                }
                if (_leftValid)
                {
                    _leftRotFilter = new OneEuroQuaternionFilter(1.2f, 0.15f, 1f);
                    _leftRotFilter.Reset(_rawLeftRot);
                    _filtLeftRot = _rawLeftRot;
                    _filtLeftPos = _rawLeftPos;
                }
                _filtInitialized = true;
            }

            // Apply filters
            float dt = Time.unscaledDeltaTime;

            if (_rightValid)
            {
                if (_rightRotFilter == null)
                {
                    _rightRotFilter = new OneEuroQuaternionFilter(1.2f, 0.15f, 1f);
                    _rightRotFilter.Reset(_rawRightRot);
                    _filtRightRot = _rawRightRot;
                    _filtRightPos = _rawRightPos;
                }
                else
                {
                    _filtRightRot = _rightRotFilter.Filter(_rawRightRot, dt);
                    _filtRightPos = FilterPositionEma(_rawRightPos, _filtRightPos, dt);
                }
            }
            if (_leftValid)
            {
                if (_leftRotFilter == null)
                {
                    _leftRotFilter = new OneEuroQuaternionFilter(1.2f, 0.15f, 1f);
                    _leftRotFilter.Reset(_rawLeftRot);
                    _filtLeftRot = _rawLeftRot;
                    _filtLeftPos = _rawLeftPos;
                }
                else
                {
                    _filtLeftRot = _leftRotFilter.Filter(_rawLeftRot, dt);
                    _filtLeftPos = FilterPositionEma(_rawLeftPos, _filtLeftPos, dt);
                }
            }

            }

            // Refresh XR rig periodically
            RefreshRig();

            // Throttled diagnostic — logs raw vs filtered pose once per second
            // Gated behind VerboseDiagnostics to avoid string allocs and Camera.main queries.
            if (ModConfiguration.Instance != null && ModConfiguration.Instance.VerboseDiagnostics.Value)
            {
                float now = Time.unscaledTime;
                if (now - _lastDiagLogTime > DiagLogInterval)
                {
                    _lastDiagLogTime = now;
                    string msg = $"[VrControllerInput] R_valid={_rightValid} rawPos={_rawRightPos} filtPos={_filtRightPos} | L_valid={_leftValid} rawPos={_rawLeftPos} filtPos={_filtLeftPos} | headValid={_headValid} rawHead={_rawHeadPos} rig={(_xrRig != null ? _xrRig.name : "<null>")} camMain={(Camera.main != null ? Camera.main.name : "<null>")} camParent={(Camera.main != null && Camera.main.transform.parent != null ? Camera.main.transform.parent.name : "<null>")}";
                    if (NOVRPlugin.LogSource != null) NOVRPlugin.LogSource.LogMessage(msg);
                    else Debug.Log(msg);
                }
            }
        }

        private static Vector3 FilterPositionEma(Vector3 raw, Vector3 smoothed, float dt)
        {
            if (dt <= 0f) return raw;
            float cutoff = 8f;
            float alpha = 1f - Mathf.Exp(-dt * cutoff);
            return Vector3.Lerp(smoothed, raw, alpha);
        }

        private static bool TryReadRawPose(InputAction? posAction, InputAction? rotAction,
            out Vector3 pos, out Quaternion rot, bool aimPose = true)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            if (posAction == null || rotAction == null) return false;
            // Tracking flags determine validity, including a legitimate zero position.
            foreach (var control in posAction.controls)
            {
                if (!(control.device is TrackedDevice device) || device.isTracked.ReadValue() < .5f) continue;
                var aim = device.TryGetChildControl<PoseControl>("pointer");
                if (aimPose && aim != null && aim.isTracked.ReadValue() > .5f && (aim.trackingState.ReadValue() & 3) == 3)
                { pos = aim.position.ReadValue(); rot = aim.rotation.ReadValue(); return true; }
                if ((device.trackingState.ReadValue() & 3) != 3) continue;
                pos = device.devicePosition.ReadValue(); rot = device.deviceRotation.ReadValue(); return true;
            }
            return false;
        }

        private static float TryReadFloat(InputAction? action)
        {
            if (action == null) return 0f;
            try { return action.ReadValue<float>(); }
            catch { return 0f; }
        }

        private static void RefreshRig()
        {
            if (_xrRig != null && Time.frameCount - _rigSearchFrame <= 120)
                return;
            _rigSearchFrame = Time.frameCount;
            var cam = Camera.main;
            if (cam != null)
                _xrRig = cam.transform.parent ?? cam.transform;
        }

        // --- Existing methods (unchanged) ---

        private static void EnsureActions()
        {
            if (_actionsInitialized) return;
            _actionsInitialized = true;

            _rightAimPos = new InputAction(binding: "<XRController>{RightHand}/pointerPosition");
            _rightAimPos.AddBinding("<XRController>{RightHand}/devicePosition");
            _rightAimRot = new InputAction(binding: "<XRController>{RightHand}/pointerRotation");
            _rightAimRot.AddBinding("<XRController>{RightHand}/deviceRotation");
            _leftAimPos = new InputAction(binding: "<XRController>{LeftHand}/pointerPosition");
            _leftAimPos.AddBinding("<XRController>{LeftHand}/devicePosition");
            _leftAimRot = new InputAction(binding: "<XRController>{LeftHand}/pointerRotation");
            _leftAimRot.AddBinding("<XRController>{LeftHand}/deviceRotation");

            _rightTrigger = new InputAction(type: InputActionType.Button, binding: "<XRController>{RightHand}/triggerPressed");
            _rightTrigger.AddBinding("<XRController>{RightHand}/trigger");
            _leftTrigger = new InputAction(type: InputActionType.Button, binding: "<XRController>{LeftHand}/triggerPressed");
            _leftTrigger.AddBinding("<XRController>{LeftHand}/trigger");

            _headPos = new InputAction(binding: "<XRHMD>/centerEyePosition");
            _headPos.AddBinding("<XRHMD>/devicePosition");
            _headRot = new InputAction(binding: "<XRHMD>/centerEyeRotation");
            _headRot.AddBinding("<XRHMD>/deviceRotation");

            _rightGrip = CreateUiAction("UI_HAND_RIGHT", "RightHand", "grip", "gripPressed");
            _leftGrip = CreateUiAction("UI_HAND_LEFT", "LeftHand", "grip", "gripPressed");
            _rightConfirm = CreateUiAction("UI_SELECT_RIGHT", "RightHand", "primaryButton", "faceButtonBottom");
            _leftConfirm = CreateUiAction("UI_SELECT_LEFT", "LeftHand", "primaryButton", "faceButtonBottom");
            _rightBack = CreateUiAction("UI_BACK_RIGHT", "RightHand", "secondaryButton", "faceButtonOutside");
            _leftBack = CreateUiAction("UI_BACK_LEFT", "LeftHand", "secondaryButton", "faceButtonOutside");
            _rightMenu = CreateUiAction("VR_MENU_RIGHT", "RightHand", "menuButton", "menuButton");
            _leftMenu = CreateUiAction("VR_MENU_LEFT", "LeftHand", "menuButton", "viewButton");
            _rightScroll = new InputAction("UI_SCROLL_RIGHT", InputActionType.Value, "<XRController>{RightHand}/thumbstick", expectedControlType: "Vector2");
            _leftScroll = new InputAction("UI_SCROLL_LEFT", InputActionType.Value, "<XRController>{LeftHand}/thumbstick", expectedControlType: "Vector2");
            _rightScroll.AddBinding("<XRController>{RightHand}/primary2DAxis");
            _leftScroll.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            _rightScroll.Enable(); _leftScroll.Enable();

            _rightAimPos.Enable();
            _rightAimRot.Enable();
            _leftAimPos.Enable();
            _leftAimRot.Enable();
            _rightTrigger.Enable();
            _leftTrigger.Enable();
            _headPos.Enable();
            _headRot.Enable();

            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private static InputAction CreateUiAction(string semantic, string hand, string compatibility, string frame)
        {
            var action = new InputAction(semantic, InputActionType.Button, $"<XRController>{{{hand}}}/{compatibility}");
            action.AddBinding($"<SteamFrameController>{{{hand}}}/{frame}");
            action.Enable();
            return action;
        }

        private static void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
        {
            if (ModConfiguration.Instance != null && ModConfiguration.Instance.VerboseDiagnostics.Value)
                Debug.Log($"[VrControllerInput] DeviceChange: {change} name={device.name} layout={device.layout}");
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
                LogDiagnostics();
        }

        /// <summary>
        /// Reads the head pose from the cached frame data.
        /// Returns false if no head pose is available.
        /// </summary>
        public static bool TryGetHeadPose(out Vector3 position, out Quaternion rotation)
        {
            EnsureFrame();
            if (_headValid)
            {
                position = _rawHeadPos;
                rotation = _rawHeadRot;
                return true;
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        /// <summary>
        /// Returns the filtered controller pose in tracking space.
        /// </summary>
        public static bool TryGetPose(XRNode hand, out Vector3 position, out Quaternion rotation)
        {
            EnsureFrame();

            bool isLeft = hand == XRNode.LeftHand;
            bool valid = isLeft ? _leftValid : _rightValid;

            if (valid)
            {
                position = isLeft ? _filtLeftPos : _filtRightPos;
                rotation = isLeft ? _filtLeftRot : _filtRightRot;
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        /// <summary>
        /// Converts the selected raw/optionally filtered pose to NOVR's calibrated UI frame.
        /// </summary>
        public static bool TryGetPoseInWorldSpace(XRNode hand, Vector3 cameraWorldPos,
            out Vector3 worldPosition, out Quaternion worldRotation)
        {
            EnsureFrame();

            bool isLeft = hand == XRNode.LeftHand;
            bool valid = isLeft ? _leftValid : _rightValid;
            Vector3 trackingPos = isLeft ? _filtLeftPos : _filtRightPos;
            Quaternion trackingRot = isLeft ? _filtLeftRot : _filtRightRot;

            if (valid)
            {
                // Express the head-relative controller pose in the calibrated VR UI camera
                // frame. This preserves physical translation and the recenter orientation.
                var camera = APIBus.CockpitHudCamera;
                if (camera == null || !_headValid) { worldPosition = Vector3.zero; worldRotation = Quaternion.identity; return false; }
                var trackingToUi = camera.transform.rotation * Quaternion.Inverse(_rawHeadRot);
                worldPosition = camera.transform.position + trackingToUi * (trackingPos - _rawHeadPos);
                worldRotation = trackingToUi * trackingRot;
                return true;
            }

            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;
            return false;
        }

        // Render refresh reads poses only. Click/grip edges remain owned by the once-per-frame Update.
        internal static bool TryGetPointerPoseForRender(out Vector3 position, out Quaternion rotation)
        {
            EnsureFrame();
            position = Vector3.zero; rotation = Quaternion.identity;
            var selected = _uiFrame.Hand;
            if (selected == PointerHand.None) return false;
            if (ModConfiguration.Instance.ControllerPoseSmoothing.Value)
                return TryGetPoseInWorldSpace(selected == PointerHand.Left ? XRNode.LeftHand : XRNode.RightHand, Vector3.zero, out position, out rotation);
            return TryGetCurrentPoseInUi(selected == PointerHand.Left, true, out position, out rotation);
        }

        internal static bool TryGetCurrentPoseInUi(bool left, bool aim, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero; rotation = Quaternion.identity;
            if (!_actionsInitialized) return false;
            if (!TryReadRawPose(left ? _leftAimPos : _rightAimPos, left ? _leftAimRot : _rightAimRot, out var p, out var r, aim) ||
                !TryReadRawPose(_headPos, _headRot, out var headPosition, out var headRotation)) return false;
            var camera = APIBus.CockpitHudCamera;
            if (camera == null) return false;
            var trackingToUi = camera.transform.rotation * Quaternion.Inverse(headRotation);
            position = camera.transform.position + trackingToUi * (p - headPosition);
            rotation = trackingToUi * r;
            return true;
        }

        internal static void LogDiagnostics()
        {
            EnsureFrame();

            var sb = new StringBuilder();
            sb.AppendLine("=== VrControllerInput Diagnostics ===");

            sb.AppendLine("--- InputSystem.devices ---");
            foreach (var d in InputSystem.devices)
            {
                bool isXR = d is XRController;
                sb.AppendLine($"  {(isXR ? "[XR]" : "     ")} name={d.name} layout={d.layout} usages=[{string.Join(",", d.usages)}] desc.interface={d.description.interfaceName} desc.product={d.description.product}");
            }

            sb.AppendLine("--- InputSystem.GetUnsupportedDevices ---");
            foreach (var d in InputSystem.GetUnsupportedDevices())
                sb.AppendLine($"  interface={d.interfaceName} product={d.product} manufacturer={d.manufacturer}");

            sb.AppendLine("--- XRInputSubsystem ---");
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var s in subsystems)
                sb.AppendLine($"  running={s.running}");

            try
            {
                var openXRSettingsType = System.Type.GetType("UnityEngine.XR.OpenXR.OpenXRSettings, Unity.XR.OpenXR");
                if (openXRSettingsType != null)
                {
                    var instanceProp = openXRSettingsType.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (instanceProp != null)
                    {
                        var instance = instanceProp.GetValue(null);
                        var featuresProp = openXRSettingsType.GetProperty("features");
                        if (featuresProp != null && instance != null)
                        {
                            var features = featuresProp.GetValue(instance) as System.Collections.IList;
                            if (features != null)
                            {
                                sb.AppendLine("--- OpenXR Features ---");
                                foreach (var f in features)
                                {
                                    var nameProp = f.GetType().GetProperty("name");
                                    var enabledProp = f.GetType().GetProperty("enabled");
                                    string fName = nameProp?.GetValue(f)?.ToString() ?? "(no name)";
                                    bool fEnabled = enabledProp != null && (bool)(enabledProp.GetValue(f) ?? false);
                                    sb.AppendLine($"  {fName} enabled={fEnabled}");
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            sb.AppendLine("--- Filtered poses (tracking space) ---");
            sb.AppendLine($"  RightHand pos={_filtRightPos:F3} rot={_filtRightRot.eulerAngles:F3}");
            sb.AppendLine($"  LeftHand  pos={_filtLeftPos:F3} rot={_filtLeftRot.eulerAngles:F3}");
            sb.AppendLine($"  RightHand trigger={_rawRightTrigger:F2}");
            sb.AppendLine($"  LeftHand  trigger={_rawLeftTrigger:F2}");

            sb.AppendLine("--- Rig transform ---");
            if (_xrRig != null)
                sb.AppendLine($"  name={_xrRig.name} pos={_xrRig.position:F3} rot={_xrRig.rotation.eulerAngles:F3}");
            else
                sb.AppendLine("  (null)");

            sb.AppendLine("=== End Diagnostics ===");
            Debug.Log(sb.ToString());
        }

        public static bool GetTrigger(XRNode hand)
        {
            EnsureFrame();
            return hand == XRNode.LeftHand ? _rawLeftTrigger > 0.5f : _rawRightTrigger > 0.5f;
        }

        public static bool GetTriggerWasPressedThisFrame(XRNode hand)
        {
            EnsureTriggerEdgeCache();
            return hand == XRNode.LeftHand ? _leftTriggerPressedThisFrame : _rightTriggerPressedThisFrame;
        }

        public static bool GetTriggerWasReleasedThisFrame(XRNode hand)
        {
            EnsureTriggerEdgeCache();
            return hand == XRNode.LeftHand ? _leftTriggerReleasedThisFrame : _rightTriggerReleasedThisFrame;
        }

        private static void EnsureTriggerEdgeCache()
        {
            EnsureFrame();
            if (Time.frameCount == _triggerEdgeFrame)
                return;

            _triggerEdgeFrame = Time.frameCount;

            bool leftNow = _rawLeftTrigger > 0.5f;
            bool rightNow = _rawRightTrigger > 0.5f;

            _leftTriggerPressedThisFrame = leftNow && !_leftTriggerWasPressedCache;
            _rightTriggerPressedThisFrame = rightNow && !_rightTriggerWasPressedCache;
            _leftTriggerReleasedThisFrame = !leftNow && _leftTriggerWasPressedCache;
            _rightTriggerReleasedThisFrame = !rightNow && _rightTriggerWasPressedCache;

            _leftTriggerWasPressedCache = leftNow;
            _rightTriggerWasPressedCache = rightNow;
        }

        public static bool TryGetDominantHand(out Vector3 position, out Quaternion rotation, out bool triggerPressed)
        {
            EnsureFrame();

            Vector3 camWorldPos = APIBus.CockpitHudCamera != null
                ? APIBus.CockpitHudCamera.transform.position
                : Vector3.zero;

            position = Vector3.zero; rotation = Quaternion.identity;
            var hand = _uiFrame.Hand == PointerHand.Left ? XRNode.LeftHand : XRNode.RightHand;
            triggerPressed = _uiFrame.Pressed;
            return _uiFrame.Hand != PointerHand.None && TryGetPoseInWorldSpace(hand, camWorldPos, out position, out rotation);
        }
    }
}
