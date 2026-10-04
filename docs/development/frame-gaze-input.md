# Native Frame gaze evidence and read path

Steam Frame application gaze uses [`XR_EXT_eye_gaze_interaction`](https://partner.steamgames.com/doc/steamhardware/steamframe/engines/custom). NOVR requests Unity's existing eye-gaze feature when the user enables eye tracking, and reads the native combined gaze pose. Eye tracking never supplies the headset pose or bypasses Nuclear Option's contact visibility and detection rules.

The first headset capture contained 86 `device-unavailable` samples. The runtime log also showed an actual native `Eye Tracking OpenXR` device exposing `gazePosition`, `gazeRotation`, `gazeIsTracked`, and `gazeTrackingState`, followed by an Input System layout error:

```text
Expected control 'pose' to be of type 'PoseControl' but is of type 'PoseControl' instead!
```

The identically named controls are different managed types. A registered layout or enabled extension alone therefore does not prove that Input System created a readable device.

NOVR now tries Input System's eye device first, then the same native gaze through Unity XR. Unity documents these [gaze feature usages](https://docs.unity.cn/Packages/com.unity.xr.openxr%401.13/api/UnityEngine.XR.OpenXR.Features.Interactions.EyeTrackingUsages.html). The fallback requires an actual device with `EyeTracking` characteristics, native tracking-valid flags, finite position and a nonzero finite quaternion. It does not substitute head direction for unavailable eye data. Explicit gaze-specific invalid flags override generic device validity. Device discovery runs at most twice per second using a reused list; detailed inventory is collected only on request.

The Input System `PoseControl` creation error may remain in the log: this change bypasses the failed layout through Unity's other native read API rather than replacing XR libraries or changing controller registration. Runtime gaze availability/sharing is a separate hardware acceptance gate. A native eye device can exist while its pose remains untracked.

Use the read-only `get_gaze_hud_state` bridge command to inspect `gazeDevices`:

- `source`: `InputSystem/nativeGaze`, `UnityXR/nativeGaze`, or `unavailable`.
- `eyeLayouts` and `inputSystemDevices`: layout registration versus successfully created devices.
- `unityXrDevices`: eye device name/characteristics, available native features, generic and gaze-specific tracking flags, raw gaze pose.

A successful end-to-end eye test requires a valid native gaze source and a cue that follows eye movement while the headset remains still. `Eye Preferred` falls back to the accepted head aim if eyes are unavailable; `Eye Only` cancels selection. No automatic clicks or firing occur. Steam sharing/privacy settings are left under the user's control.
