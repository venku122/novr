# Progress

2026-10-03: goal registered; branch created from installed upstream dev revision. Architecture/research/config/log inventory and baseline runtime build completed. Read-only bounded raw input/current profile/binding evidence added. Safe Windows staging, guarded deploy/rollback, launch and evidence collection implemented. Policy, real MSBuild target, disposable game-tree and mocked launch safety checks pass. Independent review fixes covered junction paths, metadata writes, launch race and receipt recovery. Hardware acceptance remains untested. See acceptance-matrix.md and baseline test guide.

2026-10-04 UTC: independent external M5/M7 preparation committed. Bounded local voice recording/transcription, fresh telemetry/log/complete-PNG association, durable sessions and prepared-only Codex jobs implemented; optional capture-only legacy shortcut defaults off/None. Fifteen Linux tests plus all sixteen Windows tests pass; synthetic Windows roundtrip produces a camera observation. Nine review findings repaired and regression-tested. C# gesture/build/deploy/launch gates pass. Final stage dc5af227d69b4941b42b80dbf43455e6 from 7d44cf52; live install unchanged. Hardware and full transform telemetry/UI/controller/Codex execution acceptance remain open.

2026-10-04T00:47:11.825700+00:00 Controller-first steering: compiled/reviewed native Frame+Touch UI candidate and optional gaze/head HUD; stage c1f35a62263f4764a1480041291f0b9c. Physical baseline/acceptance next. No live mutation.

2026-10-04T00:51:57.733540+00:00 Controller feedback: 7c6fb1e adds bounded optional haptics and cached UI snapshots; review disabled-button pulse issue fixed. Build f566a5807c724c90ad714496b82ddac9 and payload integrity pass; no physical data or live mutation.

2026-10-04T00:53:21.245543+00:00 Blocked audit: previous turn was progress (haptics/snapshots); same physical-evidence gate persists over three goal turns. Current audit: clean worktree, 29 staged hashes valid, no live Nuclear Option process, no real local input records. Stop speculative input work; require documented physical baseline/candidate acceptance. Goal incomplete.

2026-10-04T01:38:21.798491+00:00 User confirmed translated native Frame pointer/trigger; 0c266d9 adds raw before-render visuals, grip Auto hand switching, async installed static models. Policy and actual installed assets tests pass; independent review cleanup fixed; Windows stage b1b23c6b6bc24ad18775e3dc8c88bcda hashes valid, installed stopped-game. Physical retest underway.

2026-10-04 UTC: user accepts controllers/switching/models/movement; priority becomes seated cockpit/body-origin drift, then eye/HUD. 8301429 corrects actual camera implicit tracking ownership and controller acquisition recenter, adds read-only cockpit transform chain and reliable nested snapshot serialization. Windows build 0 errors/471 warnings, regression camera test red->green, serializer/safety tests pass; independent review no important findings. Installed while stopped, 30 live hashes verified; managed XR unchanged. Relaunched process 19792. Menu snapshots serialize correctly; physical cockpit still pending. Capture gate corrected after live menu default enum observation; first new mocked safety test failed from fixture scope and final rerun passes. Preliminary enum-only session is front-end data, not cockpit acceptance.

# Stable seated reference + optional Frame gamepad checkpoint

## What I found
User rejected previous seated candidate: head yaw translated cockpit view. Live diagnostic showed calibration sequence 9490, head-tracking-acquired recenter reason, and untracked Unity Head placeholder versus tracked actual HMD. Earlier corrected cockpit recorder timed out before mission; no failing cockpit transform chain was recorded. Controllers remain user accepted.

## Root cause / current hypothesis
Tracking acquisition recenter floods and inconsistent raw pose sources are confirmed defects. Their exact contribution to visual yaw translation requires physical retest. Nuclear Option's stock mapper excludes Rewired custom controllers; a standard virtual Xbox device uses existing joystick/flight bindings.

## Changes made
4403e26: stable validated HMD pose/reference, shared calibrated coordinates, freeze invalid tracking, remove acquisition recenter, retain pending explicit aircraft transition.
a7d82f4: disabled-by-default isolated gamepad companion (existing ViGEm driver only), Frame/Touch mappings, UI hold/release gates, Menu/View reservation, lifecycle cleanup, read-only Rewired/XInput discovery, staging/deployment allowlist and rollback tests. User explicitly requested full gamepad flight input when enabled; enabled only this host's companion config for test.

## Validation
Final Windows staging build 62f0f548c7b243fca8a694e361696b68, stage staging/20261004T030833115Z-156761a9, commit a7d82f49ecd54e4fe524021c7464bfe7a3b31d9e passed with inherited warnings, 0 errors. Diagnostic/gamepad/safety/head-capture tests passed. Optional Windows Node tests skipped (unavailable); prior playtest acceptance unchanged. First new head build failed ambiguous CommonUsages (fixed); first combined staging refused companion folder (specific allowlist and tests fixed); failed stages preserved and not deployed.
Game exited normally before deploy. 34 deployed payload hashes verified, managed XR unchanged. Rollback backup BepInEx/NOVR/dev-deployments/dc608d0d213540378209ef59de5d283b. No driver installation/update, backend switching, unrelated config changes, HOTAS remapping or OS mouse injection.
Live gamepad native loading/device creation succeeded. Rewired RawInput sees assigned XInput Gamepad 1, both VKB Gladiator EVO sticks and Keychron devices. Only XInput slot 0 connected. Evidence: evidence/head-gamepad-live.json, head-gamepad-integrity.json, head-gamepad-build-final.txt, head-gamepad-deploy.txt.

## Remaining uncertainty
Headset absent/untracked at initial menu snapshot; new camera reference not physically tested. Gamepad physical inputs/flight/remapping, UI duplicate routing, focus/reconnect/quit cleanup require actual test. ViGEm is archived optional compatibility dependency. Formal controller matrix, eye gaze and HUD tracking still pending.

## Next action
Game relaunched via Steam, launcher session 51855 tails logs. Head recorder session 57397 waits up to 10 minutes for cockpit then records 60 seconds at 2Hz to playtest/sessions/20261004T031014308Z-seated-head. Sit/recenter/yaw without lean, lean/return; check Xbox input in Controls (point away during binding), fly with HOTAS, verify UI trigger/pause. Exact tests: docs/development/seated-head-test.md and frame-gamepad.md. No overall goal completion claimed.
