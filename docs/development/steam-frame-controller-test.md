# Steam Frame controller / eye gaze hardware checkpoint

Source commit: `7c6fb1ef0301edcdb6e1075a14330ec753771e7a`. Stage: `staging/20261004T005037717Z-5f81d568`; build ID: `f566a5807c724c90ad714496b82ddac9`. This is an opt-in, reviewed, compiled candidate, not hardware acceptance. Controller UI is the immediate priority; voice/video tooling is deferred. No live files were changed during this build.

## Preserve the failing-session baseline

If no raw failing-session evidence has been collected, first follow [the baseline guide](steam-frame-baseline.md) using diagnostic-only stage `staging/20261004T001621908Z-f401af08`. Keep Frame input and eye tracking disabled. Return its collected session before activating the candidate. The absence of enabled controller action profiles and pointer lifecycle defects are source-level findings; they are not proof of the cause in your specific headset session.

## Activate the candidate after baseline capture

In Windows PowerShell from this checkout, with Nuclear Option stopped:

```powershell
$env:NUCLEAR_OPTION_GAME_DIR = 'D:\SteamLibrary\steamapps\common\Nuclear Option' # Replace if installed elsewhere.
./scripts/deploy-dev.ps1 -StageDirectory 'staging/20261004T005037717Z-5f81d568'
```

The script validates build integrity and the stopped-game gate, backs up replaced files and records rollback information. It refuses a running game. Back up `BepInEx/config/deltawing.novr.cfg` separately; deployment/rollback does not replace configuration. Add/update these entries in the existing sections:

```ini
[VR Input]
Enable Steam Frame Input = true
Pointer Hand = Auto
Scroll Speed = 8
UI Haptics = true
Enable Eye Tracking = true
Show Gaze Reticle = true
Helmet HUD Tracking = Head

[Diagnostics]
Log XR Startup Diagnostics = true
Raw Input Mode = Session
```

Keep `[Experimental] Cursor Input Mode = Auto`; do not create a second conflicting entry. Leave legacy experimental profiles, offsets, graphics and flight bindings unchanged. Restart is required for Frame/gaze feature registration. Eye gaze also requires the headset/runtime's calibration and app sharing permissions; unavailable gaze is reported explicitly. Do not change pairing to test this.

Native Frame and Oculus Touch compatibility profiles are enabled together. The runtime chooses the active profile; diagnostics records which one actually binds per hand. Compatibility may omit Frame-specific shoulder/touch controls. The SteamVR system/dashboard button is reserved and is not an app menu action.

## Exact headset test

1. Power both controllers, keep HOTAS connected, and run `./scripts/launch-dev.ps1`. During the first minute move each hand, squeeze triggers and press face/menu/view controls. Inspect logs for active profile per hand, valid tracking, resolved actions and gaze status.
2. Right controller points by default. Hover a front-end button, press/release trigger once, then repeat with the bottom/primary face button. Each should activate once with a short haptic pulse if impulse feedback is supported. Disabled buttons and mouse clicks should not pulse. Use the thumbstick vertically to scroll. Outside/secondary face button requests Back. Test NOVR settings and at least one in-flight menu. Menu/View opens or closes the in-flight pause menu where allowed; settings must be closed first.
3. Drag a slider across another UI element. Release outside its original element. Then click a button inside a scrollable list without moving. Test losing focus/opening the dashboard while holding trigger: return still holding, confirm no fresh click, release, then click again.
4. Turn off the right controller and use the left alone. Reconnect right while holding its trigger: release before the next click. Repeat with `Pointer Hand = Left` on a later run. No second controller is required.
5. In flight verify HOTAS pitch/roll/yaw/throttle remain unchanged. Move VR thumbsticks and confirm no aircraft control. Mouse click switches pointer source; controller trigger/confirm returns to controller. Verify keyboard fallback and dashboard recovery.
6. Keep your head still and move only your eyes across the menu. The small green gaze ring should move if supported/shared, without selecting or aiming anything. Blink/lose tracking and verify the ring disappears. Press F10 for gaze status/pose evidence, including failure status if absent.
7. In the cockpit turn/lean your head: helmet HUD should follow without the old smoothing lag. Aircraft HUD remains cockpit-fixed. Recenter with the existing shortcut and verify both HUD and controller pointing. Compare another aircraft; note clipping or floating UI separately.
8. Exit normally. Return the `COLLECTED:` session path, plus pass/fail for each item and whether gaze sharing/calibration was enabled. Required files: raw snapshots, NOVR configuration, BepInEx log and runtime log tail. Snapshots also include the cached pointer canvas/camera/parent, world transform/scale, ray, hovered/pressed/dragged object names and pointer coordinates. Screenshots are useful for HUD alignment; compilation cannot validate it.

For normal play disable raw session diagnostics. To undo the candidate, stop the game and run `./scripts/rollback-dev.ps1`, then restore the saved config. Toggle eye/reticle options off if gaze is unavailable; it does not block controller UI.

## Scope and limits

UI input uses Unity EventSystem events, not Windows mouse injection or aircraft axis mappings. Both hands, hysteresis, reconnect hold safety and pointer cancellation have deterministic policy tests. Hover/drag/scroll/navigation, HOTAS coexistence, gaze validity, UI coordinate calibration and headset performance require the physical test above. Activation haptics check device capability; physical feedback still requires verification. Eye tracking is gaze pose/diagnostics/reticle only; foveation and gaze-based selection/weapon targeting are outside this change.

Valve sources: [Frame custom-engine input and eye gaze](https://partner.steamgames.com/doc/steamhardware/steamframe/engines/custom?l=english), [Unity integration](https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Documentation~/index.md). The native profile is reproduced with attribution and its BSD license from pinned Valve commit `329c81f5a97a7f9e7740cf4307f1bfa9ce090b3a`.
