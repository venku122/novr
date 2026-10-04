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

## Controller mode setting (5d46a98)
User requests explicit gamepad versus UI/HOTAS choice. Added enum Controller Mode in main config and two in-headset VR UI Settings buttons; UI + HOTAS default. Removed separate companion boolean as authority; legacy flag ignored. Selecting UI-only neutralizes/removes virtual device on next update; enabling retains release gates. HOTAS and pointer unchanged. Runtime build 79aac58062b24ad8a561e25be57c87a2 passed diagnostics/gamepad/deployment/launch/head capture tests (optional Windows Node skipped). Normal close before deploy, 34/34 hashes verified; launcher session 16121. Evidence controller-mode-build.txt/controller-mode-deploy.txt/controller-mode-integrity.json. Mode-switch/persistence headset test pending. Previous camera capture completed 98 cockpit samples, sequence 2/3 explicit manual+aircraft transitions, validated centerEye; no visual-fix claim.


# Native Frame gaze and Smart HUD review checkpoint

## What I found
Tester accepted centered head tracking and controller mode after completing a mission. Native Nuclear Option target selection uses its targeting cue and normal Select bindings; visibility/sensors remain separate. Valve documents native XR_EXT_eye_gaze_interaction. Native numeric visor visibility used screen-distance tests on NOVR world-space geometry.

## Root cause / current hypothesis
Weapon/status panels were aircraft-fixed by NOVR layout. Numeric visor proximity tests could hide speed/altitude incorrectly. Native gaze availability depends on runtime sharing/tracking and must be verified live.

## Changes made
104b7e7 adds native combined gaze cueing, Head/EyePreferred/EyeOnly, fresh head-following Helmet/Smart status, 25/20-degree hysteresis, 35/30-degree cockpit declutter, settings and read-only paired gaze/HUD evidence. 65f98fa adapts only four numeric visor predicates with exact-signature/count guard and excludes eye devices from head fallback. Directional symbols remain registered; no auto-select/fire or sensor bypass.

## Validation
Final Windows stage staging/20261004T042011924Z-438cb964, build f3c94cdc606f484d9b87ee92b3d8571e, source 65f98fa: zero errors with inherited warnings. Gaze/camera/controller/gamepad/HUD policy and restoration, exact-call guard, deployment/rollback/launch/capture safety checks pass. Optional Windows Node tests skipped (unavailable). Review found and fixed diagnostic filter mutation, NaN tracked checks and numeric visibility gap. Independent actual-reference review passed.
Stopped-game deployment verified 34/34 hashes. Managed XR identical. Previous accepted binaries preserved in BepInEx/NOVR/dev-deployments/0899aa9240a74251b4ef0bd1eb4871e8. Host config only Enable Eye Tracking=true and Helmet HUD Tracking=Head changed for review; prior config saved in evidence/gaze-hud-config-before.cfg. Product gaze default stays off. No graphics/HOTAS/Steam privacy/pairing changes.

## Remaining uncertainty
Actual native gaze sharing, eye-directed Select, optics/placement/readability, declutter and directional-symbol registration require headset acceptance. Native numeric threshold transitions may lag one update. Original full development-loop mission incomplete.

## Next action
Launched via Steam (session68136), bounded paired cockpit capture waiting (session15705, 600-second wait then 60 seconds). Review docs/development/frame-gaze-hud-review.md. Keep head still and move eyes between contacts; confirm Gaze tracked / Sight Eye, select normally; check Smart forward/away/down and Helmet numeric readability, recenter/HOTAS/controller regression. Read-only evidence/gaze-hud-live.json when runtime available.

Live checkpoint: process23300; native gaze extension enabled, feature enabled, requested=true, gazeTracked=false, device-unavailable:check-runtime-sharing. Menu source None is expected before cockpit cue updates. Smart status and Head helmet tracking active. Current log shows both Frame models loaded and read-only gaze/HUD tool registered; no numeric patch-skip warning. This does not establish rendered HUD or actual eye tracking acceptance. Paired recorder waits at playtest/sessions/20261004T042133680Z-seated-head.


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


# Pause-menu VR UI settings checkpoint

User requests live settings during a mission. Added VR UI SETTINGS alongside the pause-menu Recenter entry, independent of the optional recenter/native-frontend toggles. Reuses the existing settings panel in a dedicated world-space pause overlay, hides/restores only the stock pause CanvasGroup, leaves native pause state/timers/flight inputs intact. BACK/controller cancel returns to pause, Resume/Escape/menu replacement/scene exit cleans up, delayed recenter cancels on close, first button supports keyboard navigation.

Source5055a39 +e06d398. Final actual-reference staging build18d08c4b7fa2453f89ce0aac5a3e620f, staging/20261004T050444579Z-6b159f1c passed zero errors with inherited warnings. Existing diagnostic/controller/camera/gaze/gamepad/HUD and deployment/launch/capture checks passed; optional Windows Node unavailable/skipped. Source snapshot hashes verified. Independent read-only review found no important lifecycle/routing defects. No headset acceptance from compile/review.

Not deployed: NuclearOption process55468 is still running. User asked to exit before safe installation/relaunch; no process killed and no live files replaced. Prior deployedfac0f64 candidate remains. Manual test after installation: mission pause→VR UI SETTINGS, change HUD/sight/controller mode, BACK returns to pause, Resume removes overlay, reopen verifies persistence/one button, check pointer/mouse/keyboard/HOTAS. Review docs/development/pause-vr-ui-settings.md.

Pause settings installed after explicitly authorized clean window close;34/34hashes verified; process39840 relaunched. New physical checkpoint in PAUSE-SETTINGS-HANDOFF.md.


# Compact HUD and gaze pose-layout compatibility checkpoint

## What I found
User accepted pause settings, killfeed/level-up following and seated position. Eye tracking still failed; map inherited an enlarged helmet status panel and blocked center. InputSystem EyeGaze FinishSetup required one PoseControl implementation while the native layout builder supplied another. Latest old recorder timed out before cockpit: no paired cockpit evidence from that run.

## Root cause / current hypothesis
Confirmed source defects: incompatible native pose parent cast and whole LowerLeftPanel map magnification. Whether corrected eye registration yields valid Frame gaze remains hardware-dependent. No claim eye sharing is the cause without native flags/action evidence.

## Changes made
079b3f3 caches the four common typed pose children for either native implementation without replacing global controller layout. Adds read-only profile/action/layout inventory. 7a18cea stages and stopped-game deploys only NOVR-owned OpenXR/Management managed assemblies together with matching CopyToGame payload; backup/rollback includes both. Capture records separate startup gaze samples every5s, never counts them as cockpit evidence; cockpit wait now1800seconds for this run. a08c075 separates minimized map at12deg nominal footprint left28/down18 from status; preserves native full tactical map and parents/visibility. Smart defaults Compact weapon/numeric lower-right cluster, secondary panels aircraft-fixed. Shared pause panel provides map size/status scale/detail controls. Notifications, seat, controller and flight code unchanged.

## Validation
Final actual-game-reference Windows staging build37daa10fa0ce4b7b8b6e910d85d9bc3f, staging/20261004T054626162Z-943a79de, commit a08c075: zero errors with existing warnings. Gaze adapter variants/missing children/cache/tracking loss, HUD bounds/map lifecycle/1000mode transitions, diagnostics/camera/gamepad and deployment/rollback/launch/capture tests pass. Windows optional Node suite skipped (unavailable). Independent HUD actual-reference build0errors441warnings. Source540file hashes verified before deploy; stopped-game deployment succeeded,36/36live hashes verified. Backup839af7da21374211a5adfa6477607ad5 includes actual managed XR. Game20724 launched via Steam, recorder45620 waiting1800seconds then60seconds actual paired cockpit capture. No runtime privacy, pairing, graphics, HOTAS or unrelated mod changes.

## Remaining uncertainty
Actual tracked gaze/eye-directed Select, minimap readability/click geometry, full-map transitions and off-axis HUD acceptance pending physical retest. Whole original development-loop mission incomplete.

## Next action
Mission→pause→VR UI SETTINGS; Smart+Compact review map peripheral12deg, resize if needed. Open/close full tactical map and check zoom/clicks. Look forward/side/down and assess clear center/readable status. Keep head still, move eyes between contacts, press normal Select; report exact Gaze/Sight status if unavailable. Verify accepted seat/controllers/HOTAS/notifications stay intact. Capture startup and paired cockpit evidence automatically; do not infer hardware acceptance from tests.

Startup checkpoint frame4563: EyePreferred, extension enabled/requested, global Pose layout OpenXR.Input.PoseControl, correct EyeGaze layout registered. Only HMD connected; head currently untracked and no native eye/controller device yet. Therefore this snapshot cannot validate corrected eye device setup or sharing. Initial read-only RPC timed out during loading; subsequent probe succeeded. Existing NuclearOptionVWS.Plugin.Update exceptions remain unrelated and unchanged. Recorder continues waiting for cockpit.
