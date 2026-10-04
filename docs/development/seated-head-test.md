# Seated cockpit head tracking checkpoint

The tester accepts controller selection, grip hand switching, full static models and movement on code 0c266d9 (2026-10-03). This confirms the reported controller feature, while detailed drag/scroll/reconnect and simultaneous HOTAS/mouse/keyboard matrix checks remain separate.

The next reported defect: entering a mission and flying seated, moving the head moved the whole body and displaced the centered position.

Source investigation found that disabling Unity's automatic camera tracking ran on NOVRHeadsetData, attached to Core without a Camera. Actual tracked and overlay cameras continued receiving implicit tracking alongside manually driven or inherited HMD transforms. The candidate disables implicit tracking on the game-owned camera root and each NOVR stereo camera. This is a concrete ownership defect; attribution of the physical symptom still requires the test below. TrackIR was disabled in the host's saved settings. Controller tracking acquisition also used to recenter the headset; that is now restricted to head nodes.

No new aircraft-specific position offset or change to flight input bindings is included. Existing yaw calibration math, cockpit inertia and saved seat offsets are retained to isolate the tracking ownership test. Head tracking remains six degrees of freedom: leaning should move your viewpoint relative to the cockpit; the seat/body/cockpit must not follow the head or shift when you return to neutral.

## Manual test

1. Enter a mission and sit in your normal neutral flight position. Recenter once using the existing NOVR shortcut/configuration (currently F9 on this host) or its VR menu.
2. With the aircraft stationary, turn your head left/right without intentionally leaning. Check that the seat and cockpit remain fixed.
3. Lean left/right, forward/back and up/down by roughly 10–20 cm, then return to neutral. Your viewpoint should move inside the cockpit and return to the same center.
4. Fly briefly using HOTAS and repeat. Report whether the off-center shift occurs only in flight or also while stationary.
5. Point and select with each controller, switching with the grips. Check that models and rays remain aligned after the camera ownership change.
6. If the bug persists, describe whether it affects rotation, translation or both, whether returning to neutral restores center, and which aircraft is affected. Do not compensate with additional seat offsets during this comparison.

## Recorded evidence

`scripts/capture-head-dev.ps1` waits for cockpit mode through the existing local read-only MCP bridge, then records a bounded 60-second window at 2 Hz. It captures raw HMD pose validity, calibrated pose, calibration sequence/reason, seat offsets, camera local/world transforms and parent chain, game camera root, pivot, cockpit, seat reference, aircraft/pilot transforms, head position relative to seat/aircraft and stereo view matrices. It does not recenter, change game settings or control aircraft.

Snapshots also accompany F10 input captures and optional playtest observations. Snapshot JSON now uses cached public-field contracts to preserve nested devices/camera data while excluding Unity properties and native getters. Instrumentation remains off by default; hierarchy/render-matrix reads happen only on requested/bounded snapshots.

The source regression test fails on old camera initialization and passes on the candidate, using a substitute for Unity's native camera API. Nested serialization tests and the real Windows build pass. These are preparation evidence, not seated hardware acceptance.

After seated behavior is accepted: validate optional Frame gaze availability and head-following helmet HUD, then finish the safe build/deploy/rollback and evidence capture loop. Eye tracking, HUD tracking and the original full development mission remain open.

## Stable tracking reference candidate

The preceding headset test still translated the cockpit during head rotation. Live evidence then showed calibration sequence 9490 with `head-tracking-acquired`, while Unity's `XRNode.Head` returned an untracked `(0, 1.6, 0)` placeholder and the actual Input System HMD remained tracked.

The next candidate removes tracking-event recentering entirely. It reads a validated `XRHMD.centerEye` pose (actual tracked Unity XR HMD fallback), initializes its seat reference once, and freezes the last pose during tracking loss. Position and orientation share the calibrated yaw; seat offset is applied outside the tracking rotation. Explicit recenter and aircraft transition remain available. Before-render pose reading precedes camera application and controller drawing.

Test in the cockpit: sit neutral, recenter once, slowly yaw left/right without leaning, return neutral, lean a little left/right, return neutral. Capture must show a stable calibration sequence during rotation; legitimate tracked HMD position changes still translate the view. Test another aircraft and one dashboard open/close. Do not infer a cockpit fix from menu-camera telemetry: capture requires an actual seat and game camera root.
