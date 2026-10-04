# Frame eye sight and Smart HUD — review checklist

The preceding controller-mode and seated head fix were accepted by the tester after completing a mission. This candidate changes cueing/HUD only; it does not drive head/body position or replace HOTAS flight input.

## What changes in the headset

Open **VR UI SETTINGS → SIGHT AND HELMET**:

| Setting | Behavior |
| --- | --- |
| Eye tracking | Requests native Frame/standard OpenXR gaze; enabling requires a restart and runtime sharing |
| Sight: Head | Head-directed cue; eyes never move it |
| Sight: Eye Preferred | Tracked gaze directs cue; unavailable gaze immediately uses head |
| Sight: Eye Only | Tracked gaze required; missing/invalid/unfocused gaze hides cue and blocks native target picking |
| Gaze feedback | Optional noninteractive marker on UI surfaces; no dwell selection |
| Status: Aircraft | Existing fixed NOVR status layout |
| Status: Helmet | Existing weapon/status panels follow helmet |
| Status: Smart | Aircraft forward, helmet when looking away, cockpit declutter looking down |
| Helmet: Head | Fresh calibrated head pose before rendering |
| Helmet: Smoothed | Legacy filtered display reference |

Normal Nuclear Option **Select** (tap/paint) bindings remain authoritative. Looking never auto-selects, locks or fires. Visual detection, range, occlusion, faction exclusions and targeting priority remain game-owned. This is an eye-directed selection cue, not an eye-tracking replacement for the game's sensor detection. Velocity **numbers** and weapon status are readable status; flightpath **vector**, pitch, boresight, lead, bombs and target markers remain registered to their real directions. HUD placement never follows eye movement.

The native interface exposes combined gaze. It does not provide this implementation with eyelid openness, per-eye raw cameras, gaze confidence or eye calibration controls. A blink might or might not invalidate the runtime pose; validity/sharing checks are used, not an invented blink signal. Use Frame's calibration/sharing UI if required. Foveated streaming alone is not proof applications receive gaze.

## In-headset review

1. Verify status **Gaze: tracked / Sight: Eye**. If absent, enable the runtime's eye-data sharing and calibrate there; report the exact displayed status. Do not infer availability from a configured checkbox.
2. Keep head still and look between two visible contacts. Cue should follow your eyes; pressing your existing Select binding should choose the contact under it. Aiming should not fire or move the aircraft.
3. Try Head: eyes alone do not move cue. Try Eye Preferred with sharing disabled/eyes not tracked: cue uses head. Try Eye Only: absent tracking prevents selecting a phantom center contact. Existing locked targets should remain selected.
4. In Smart HUD, look forward, then yaw beyond ~25°, then return below ~20°. Weapon/countermeasure/status panels should follow only when looking away, without duplicates or boundary chatter. Look down at instruments: Smart should keep visor clear by restoring aircraft placement.
5. In Helmet, speed/altitude/weapon status should remain readable during head movement. Flightpath, pitch and target symbols must not stick to your eyes/head. Roll/pitch the aircraft to check registration; test another aircraft.
6. Recenter, lean and return neutral. Confirm the already-accepted centered head tracking, HOTAS flight, controller UI and optional gamepad mode remain intact.

Read-only `get_gaze_hud_state` reports actual native gaze/aim validity, source, calibrated world/UI rays, status mode, head angle and cached panel transforms. `capture-head-dev.ps1 -IncludeGazeHud` waits for a real cockpit, then records a bounded 60-second head + gaze/HUD pair for review. No high-frequency permanent logging or source/flight/config mutation occurs during capture.

For source review: [design and rationale](frame-gaze-hud-design.md). The review candidate still requires actual Frame gaze and rendered HUD acceptance; green tests cover policies and mocked restoration, not headset optics or target selection.
