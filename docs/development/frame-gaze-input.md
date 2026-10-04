# Native Frame gaze evidence and read path

Steam Frame application gaze uses [`XR_EXT_eye_gaze_interaction`](https://partner.steamgames.com/doc/steamhardware/steamframe/engines/custom). NOVR requests Unity's existing eye-gaze feature when the user enables eye tracking, and reads the native combined gaze pose. Eye tracking never supplies the headset pose or bypasses Nuclear Option's contact visibility and detection rules.

The first headset capture contained 86 `device-unavailable` samples. The runtime log also showed an actual native `Eye Tracking OpenXR` device exposing `gazePosition`, `gazeRotation`, `gazeIsTracked`, and `gazeTrackingState`, followed by an Input System layout error:

```text
Expected control 'pose' to be of type 'PoseControl' but is of type 'PoseControl' instead!
```

The identically named controls are different managed types. A registered layout or enabled extension alone therefore does not prove that Input System created a readable device.

NOVR now tries Input System's eye device first, then the same native gaze through Unity XR. Unity documents these [gaze feature usages](https://docs.unity.cn/Packages/com.unity.xr.openxr%401.13/api/UnityEngine.XR.OpenXR.Features.Interactions.EyeTrackingUsages.html). The fallback requires an actual device with `EyeTracking` characteristics, native tracking-valid flags, finite position and a nonzero finite quaternion. It does not substitute head direction for unavailable eye data. Explicit gaze-specific invalid flags override generic device validity. Device discovery runs at most twice per second using a reused list; detailed inventory is collected only on request.

The native XR fallback alone did not establish eye tracking in the next run. Saved inventory snapshots came from the early menu before native eye discovery; that run again logged the Input System `PoseControl` creation error later. The missing cockpit capture means gaze validity and runtime sharing could not be determined from those snapshots.

The updated eye-gaze feature now accepts both native `PoseControl` parent implementations by caching their common typed children (`isTracked`, `trackingState`, `position`, and `rotation`). It preserves the existing global `Pose` layout and controller registration. Giving the eye pose a separate annotation alone would be ineffective: the installed Unity XR layout builder overwrites native pose parents with the global `Pose` layout. The existing public typed `pose` property remains available when its expected implementation matches; with the alternate implementation it is null, and callers use `TryReadNativePose` instead. Missing or wrongly typed pose children still fail setup. This bounded change requires the rebuilt managed OpenXR DLL to be staged and deployed alongside the plugin; there is no library/version upgrade.

Runtime gaze availability/sharing remains a separate hardware acceptance gate. A native eye device can exist while its pose remains untracked. Native action/profile evidence is read-only and does not grant permissions or enable sharing.

Use the read-only `get_gaze_hud_state` bridge command to inspect `gazeDevices`:

- `source`: `InputSystem/nativeGaze`, `UnityXR/nativeGaze`, or `unavailable`.
- `eyeLayouts` and `inputSystemDevices`: layout registration versus successfully created devices.
- `unityXrDevices`: eye device name/characteristics, available native features, generic and gaze-specific tracking flags, raw gaze pose.
- `poseLayoutType` and `inputSystemDevices[].poseImplementation`: actual managed layout/parent types, so identically named `PoseControl` classes can be distinguished.
- `allUnityXrDevices`: distinguishes a pre-initialization snapshot from a tracked session missing eyes.
- `eyeInteractionProfile`, `gazePositionActionRegistered`/`gazeRotationActionRegistered` and corresponding `ActionActive` flags: distinguish device/layout creation from native gaze action binding/activity. An inactive action alone does not prove privacy denial; focus, tracking and runtime support also matter.

A successful end-to-end eye test requires a valid native gaze source and a cue that follows eye movement while the headset remains still. `Eye Preferred` falls back to the accepted head aim if eyes are unavailable; `Eye Only` cancels selection. No automatic clicks or firing occur. Steam sharing/privacy settings are left under the user's control.
