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

## Controller mode setting (5d46a98)
User requests explicit gamepad versus UI/HOTAS choice. Added enum Controller Mode in main config and two in-headset VR UI Settings buttons; UI + HOTAS default. Removed separate companion boolean as authority; legacy flag ignored. Selecting UI-only neutralizes/removes virtual device on next update; enabling retains release gates. HOTAS and pointer unchanged. Runtime build 79aac58062b24ad8a561e25be57c87a2 passed diagnostics/gamepad/deployment/launch/head capture tests (optional Windows Node skipped). Normal close before deploy, 34/34 hashes verified; launcher session 16121. Evidence controller-mode-build.txt/controller-mode-deploy.txt/controller-mode-integrity.json. Mode-switch/persistence headset test pending. Previous camera capture completed 98 cockpit samples, sequence 2/3 explicit manual+aircraft transitions, validated centerEye; no visual-fix claim.
