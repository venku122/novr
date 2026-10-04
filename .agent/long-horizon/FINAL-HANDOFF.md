# HUD readability, native gaze and seated-reference retest

## What I found
86 paired cockpit samples show manual sequence2 replaced by aircraft-change sequence3 while user leaned forward; returning neutral produced approximately18cm aft calibrated position. All gaze samples lacked InputSystem eye device. Player.log confirms native Eye Tracking OpenXR device and PoseControl type collision. HUD retained high aircraft offsets/small scale; MessageUI was fixed, KillDisplay owns rank/new-aircraft/credit notifications.

## Root cause / current hypothesis
Confirmed source defects: automatic aircraft recenter overwrites manual reference; InputSystem type collision hides a native XR read path; inherited status geometry and missing notification adapters. Contribution to seat clipping/actual optics and native gaze sharing requires physical retest.

## Changes made
be22886 preserves physical seat reference across aircraft changes and applies saved offsets separately; adds headInSeatMeters. ef38a2c reads valid native Unity XR gaze and inventories devices on request. dc111a3 guards quaternion magnitude overflow. d9deae3 lowers/enlarges helmet status, compacts CM/power/weapons, lowers numbers, and follows MessageUI/KillDisplay roots without moving gameplay canvas/directional markers. 57f8b11/fac0f64 fix notification startup/restore ordering; first capture deferred until LateUpdate, native parents/visibility/timers retained. Existing HOTAS/controller routes unchanged.

## Validation
Final reviewed Windows stage staging/20261004T044721799Z-0fb6d9c0, build c1ce53d7ed3d40b1a7e461fe851d8003, commit fac0f64: zero errors with inherited warnings. Diagnostic/gaze/head/controller/gamepad/HUD placement/lifecycle and deployment/rollback/launch/capture checks pass. Optional Windows Node skipped. Independent actual-reference HUD/gaze builds and seated review passed; overflow review defect fixed/tested. Earlier passing stages superseded during lifecycle review, never deployed. Final530source hashes verified before deployment; stopped-game gate passed,34/34live payload hashes verified. ManagedXR identical. No live configuration, privacy, graphics or HOTAS changes. Rollback backup BepInEx/NOVR/dev-deployments/bd2c44ab1a744ce5b2517a31280a4402.

## Remaining uncertainty
Actual readable layout/notifications, mission-entry neutral seat and eye cue selection pending. Startup requested/extension gaze enabled but no native eye device present yet; do not infer hardware support from feature registration. Existing unrelated NuclearOptionVWS.Plugin.Update exceptions still occur in startup log; unrelated mod left unchanged.

## Next action
Game process55468 launched (session29458). Recorder session10872 waits600seconds then records60seconds of actual cockpit head+gaze/HUD to playtest/sessions/20261004T044900065Z-seated-head. Sit comfortably and recenter once; briefly lean during mission entry and return neutral, yaw/lean/recenter; inspect lower HUD, killfeed/chat/mission and rank marks. Keep head still and move eyes, report actual gaze source/status if unavailable. Review docs/development/frame-gaze-hud-review.md and frame-gaze-input.md. Original full development-loop goal remains incomplete.
