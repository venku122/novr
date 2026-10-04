# Steam Frame development milestones

The original development goal remains incomplete. STATE.json and this checklist track resumed work; no native todo tool is exposed.

- [ ] M0: Baseline architecture, provenance and actual failing-session input evidence
- [ ] M1: Raw left/right input and profile selection verified on Frame
- [ ] M2: Functional tracked UI pointer in game and NOVR menus
- [ ] M3: HOTAS, mouse and keyboard coexistence verified
- [ ] M4: Safe staging, stopped-game deployment, rollback and launch
- [ ] M5: Push-to-talk voice observation with transcript and evidence session
- [ ] M6: Camera/origin, controller, canvas and rendering snapshot telemetry
- [ ] M7: Codex evidence consumption with explicit command mutation boundaries
- [ ] M8: Complete in-headset capture/diagnose/build/deploy/retest demonstration

Prepared substeps (not hardware milestone acceptance):
- [x] Installed/upstream architecture and public docs/issues inventoried.
- [x] Read-only raw/profile/action diagnostics compile.
- [x] Real Windows runtime staging build produced and payload hashes verified.
- [x] Diagnostic policy and build/deployment/rollback/launch safety tests pass.
- [x] Independent review findings repaired.
- [x] Bounded external recording/session/evidence and prepared Codex command boundaries tested.
- [x] Real Windows synthetic FFmpeg/speech/session roundtrip (no headset microphone).
- [x] Opt-in legacy key/HOTAS capture bridge compiles; pure gesture safety tests pass.
- [x] Physical baseline input/session capture (456 baseline / 407 controller retest snapshots; startup native profiles on both hands).
- [ ] Live deployment/rollback/SteamVR launch acceptance.

Current goal phase: fix seated cockpit origin/head movement, validate eye/HUD tracking, then finish the safe development loop. User accepts controller selection, grip switching, models and movement on 0c266d9. Detailed reconnect/drag/scroll/coexistence matrix remains to document. Voice/video remains behind cockpit and tracking fixes.
- [x] Native Valve Frame profile and Touch compatibility registered behind opt-in.
- [x] Hand policy, trigger hysteresis, cancellation/reconnect hold safety tested.
- [x] EventSystem hover/click/drag/scroll and back/menu routing built and independently reviewed.
- [x] Optional gaze snapshots/reticle and head-following helmet HUD built.
- [x] Capability-gated UI activation haptics and cached canvas/pointer snapshots built/reviewed.
- [ ] Physical haptic eligibility/feedback and UI snapshot accuracy.
- [ ] FRAME-EYE: actual Frame eye gaze/sharing/blink/reticle behavior.
- [ ] FRAME-HUD: head-following helmet HUD/recenter/multiple aircraft acceptance.


- [x] Controller selection, hand switching, meshes and movement accepted by actual Frame user.
- [ ] SEATED-HEAD: head movement changes viewpoint relative to fixed seat, without moving the cockpit/body/origin or drifting off center.
- [ ] SEATED-HEAD: seated yaw/lean/return, recenter, flight and controller regression retest with camera/seat evidence.
- [ ] Integrate stopped-game managed XR replacement and its backups into product deploy/rollback.

Next physical checkpoint: docs/development/seated-head-test.md. No cockpit fix, gaze or HUD acceptance is claimed from compilation.

- [x] Optional Frame normal Xbox gamepad bridge built, tested, deployed and enabled as requested.
- [x] Actual game detects assigned XInput Gamepad 1 alongside both VKB HOTAS devices.
- [ ] FRAME-GAMEPAD: actual controls/remapping/flight, UI trigger suppression, single pause, focus/reconnect neutralization and exit cleanup acceptance.
- [x] Stable validated seated pose implementation and regression tests (including delayed aircraft-change calibration).
- [ ] Previous cockpit yaw failure retested against a7d82f4; no fix acceptance claimed yet.

- [x] In-headset UI + HOTAS / UI + GAMEPAD mode selector, persisted main config, default UI + HOTAS.
- [ ] Physical live mode-switch/removal/reconnect, pointer/HOTAS and persistence acceptance.

Current user acceptance: controller mode setting worked, head tracking stayed centered, and a complete mission succeeded.
- [x] SEATED-HEAD: actual centered mission acceptance reported by tester.
- [x] In-headset controller mode setting accepted by tester; exhaustive reconnect/cleanup matrix remains pending.
- [x] Native combined OpenXR gaze cueing and Smart helmet status implementation prepared.
- [ ] Eye-directed cue/Select, sharing fallback, Smart forward/away/down placement and numeric visor visibility headset review.
Review: docs/development/frame-gaze-hud-review.md.
