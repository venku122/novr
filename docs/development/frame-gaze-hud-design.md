# Frame gaze cueing and hybrid helmet HUD — review design

Intent: retain the accepted centered cockpit and controller/HOTAS modes, add native Frame gaze-directed targeting, and make status readable while looking away without breaking aiming or world registration.

## Evidence and reference behavior

Valve documents `XR_EXT_eye_gaze_interaction` as the Frame application gaze interface: https://partner.steamgames.com/doc/steamhardware/steamframe/engines/custom . Use the existing NOVR Unity OpenXR EyeGazeInteraction feature; no proprietary bridge, eye-camera capture or private SteamVR API.

DCS F-16 JHMCS includes flight data, head-directed cueing and selectable HUD/cockpit blanking: https://www.digitalcombatsimulator.com/upload/iblock/6f7/joywm66pwdgg4iacljy8zolons4dz8dh/DCS%20F-16C%20Early%20Access%20Guide%20EN.pdf . VTOL VR's developer describes a simplified visor HUD when the cockpit HUD is out of view: https://store.steampowered.com/news/posts/?appids=667970&enddate=1512087325 . These are reference behaviors, not evidence these simulators use Frame gaze for spotting.

Actual Nuclear Option 0.34.1 CombatHUD.TargetSelect compares its targeting cue to already-visible marker positions, then runs normal prioritization/exclusions and Select binding. TargetDetector and DetectorManager separately enforce sensors, range and line of sight. Eye-directed cueing must not bypass detection or visibility, change flight/weapon input, or silently select/fire.

## Implementation

- Native combined OpenXR gaze, opt-in feature registration before XR starts. No claim that streaming's foveated eye data is automatically shared with applications.
- Spotting aim: Head, EyePreferred, EyeOnly. EyePreferred uses tracked gaze; unavailable gaze falls back to head and reports why. EyeOnly invalid gaze suppresses target picking rather than selecting an invisible center cue. Keep normal Select/paint bindings, faction filters, exclusions and prioritization.
- Validate tracking, focus and finite poses; cache device/control discovery. Short configurable direction filtering, reset on invalid data/source/calibration changes. Never reuse stale gaze. No dwell selection or automatic weapon fire.
- Map gaze through the same validated head/calibrated UI coordinates as cameras and controllers. Refresh cue immediately before native TargetSelect as well as before rendering. Do not move the camera/body/origin with gaze.
- Separate status panels (weapon/loadout, power/countermeasure/fuel/status) from aircraft/world registered pitch, flightpath, boresight, lead, bomb/target markers. Numeric velocity is status; the velocity-vector symbol remains directional.
- Status placement: Aircraft (legacy), Helmet (head following), Smart (aircraft ahead, helmet when looking away). Smart uses a configurable 25-degree view cone with 5-degree hysteresis and returns status to aircraft placement looking down 35 degrees (resumes below 30), keeping instruments clear; move one existing panel, avoid duplicated symbols and oscillation. Never reparent directional symbols to the visor.
- Helmet follows the fresh head pose before rendering; optional legacy smoothed tracking remains selectable. HUD never follows eyes. Cache known transforms once; retain native visibility and exact legacy restoration when switching.
- In-headset settings for gaze request (restart needed), aim source, optional noninteractive cue feedback, status placement and helmet tracking. Read-only diagnostics expose requested/actual gaze source, validity/unavailability, current cue, status mode and known panel transforms.

## Validation and checkpoints

Pure tests: focus/tracking/nonfinite rejection, source fallback/strict eye-only invalid cancellation, calibration/source transition filter reset, timing/filter behavior, Smart hysteresis and placement restoration policy. Real Windows reference build and existing controller/camera/gamepad/deployment checks. No actual headset acceptance from mocks or compilation.

Hardware review: gaze sharing/calibration enabled by tester in Frame/SteamVR if required; keep head still and move eyes between visible contacts, verify cue moves and native Select chooses that contact; cover/close eyes or disable sharing to verify safe fallback/cancellation. Test aircraft/head mode, yaw/lean/recenter, Smart threshold jitter, look down at instruments, pitch/flightpath/target registration, multiple aircraft, HOTAS/gamepad/controller UI co-existence. Capture read-only snapshots/screenshots. Current permission allows implementation/build/rollback-safe deployment; no upstream push/merge.

Review found and fixed diagnostic interference: on-request telemetry now reads cached cue state plus sample-frame metadata rather than advancing the aim filter. Runtime positive tracked checks reject NaN; coordinate tests cover 90-degree recenter, 10-degree eye offset and one-time lean contribution.
