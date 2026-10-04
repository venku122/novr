# Local evidence index

Ignored evidence under evidence/ contains installed-baseline.json, baseline-build.txt, diagnostic-build.txt, windows-build-dev-final.txt, diagnostics-tests.txt, build-safety-tests.txt, deployment-tests.txt, launch-safety-tests.txt and negative reproducer logs. Public summary: reports/steam-frame-baseline-research.md. Staged payload build.json hashes every deployable file; source-files.json records source provenance. Reviewer disposition is captured in independent-review.txt. Historical preparation snapshot; subsequent real runs and user acceptance are indexed below.


Playtest preparation: playtest-tests-linux.txt (15 pass, one Windows fixture skipped), playtest-tests-windows.txt and playtest-stage-build-final.txt (all 16 Windows tests pass including synthesized speech through FFmpeg -> local transcript -> observation). playtest-gesture-tests.txt covers C# privacy/hold/release/timeout/lifecycle. playtest-build-safety.txt covers empty intermediate roots plus existing build safety. playtest-review.txt records repaired independent review findings. playtest-stage-integrity.json verifies final stage/source hash and unchanged live DLL/config. No real microphone or headset capture performed.

Seated candidate evidence: seated-camera-red.txt (old actual camera initialization fails mocked native boundary test); seated-camera-tests.txt and seated-camera-final-build.txt (final policy/initialization/nested serializer/build/stopped-game safety pass); seated-camera-deploy.txt (backup receipt); seated-camera-launch.txt (live startup); seated-camera-live-head.json (read-only menu HMD/transform snapshot, NOT cockpit). 30 installed payload hashes match build d24e6f64794e417e9d3cbd19570cca24. Native XR files hash-identical to previous deployed build. Independent reviewer confirms no important concrete issues; hardware effect unknown. Official reference: https://docs.unity3d.com/es/2020.2/ScriptReference/XR.XRDevice.DisableAutoXRCameraTracking.html documents disabling implicit tracking when transforms are set externally.

Head capture: initial enum-only live session 20261004T020354389Z collected menu because CameraMode defaults to cockpit. Corrected gate requires actual seat/root. head-capture-safety.txt is final mocked rejection/acceptance/bounded numeric serialization proof (initial test fixture scoping failed, then repaired). seated-camera-capture-final.txt tracks corrected pending cockpit session; do not claim acceptance or valid headset poses from untracked menu defaults.

## a7d82f4 seated/gamepad candidate
Live diagnosis: evidence/seated-rotation-live.json; compile/tests: evidence/head-gamepad-build-final.txt and seated-head-final-tests.txt, gamepad-final-tests.txt; stopped deploy/hash: head-gamepad-deploy.txt, head-gamepad-integrity.json; gamepad detection: head-gamepad-live.json; capture launcher: head-gamepad-capture.txt. Hardware yaw/flight acceptance pending.

## Controller mode setting (5d46a98)
User requests explicit gamepad versus UI/HOTAS choice. Added enum Controller Mode in main config and two in-headset VR UI Settings buttons; UI + HOTAS default. Removed separate companion boolean as authority; legacy flag ignored. Selecting UI-only neutralizes/removes virtual device on next update; enabling retains release gates. HOTAS and pointer unchanged. Runtime build 79aac58062b24ad8a561e25be57c87a2 passed diagnostics/gamepad/deployment/launch/head capture tests (optional Windows Node skipped). Normal close before deploy, 34/34 hashes verified; launcher session 16121. Evidence controller-mode-build.txt/controller-mode-deploy.txt/controller-mode-integrity.json. Mode-switch/persistence headset test pending. Previous camera capture completed 98 cockpit samples, sequence 2/3 explicit manual+aircraft transitions, validated centerEye; no visual-fix claim.

UI + HOTAS live: config persisted UiHotas; process36096, no companion bus-connected log and no present Windows Xbox 360 PnP endpoint. Game still sees Steam-associated XInput slot0 (handle32176874, unknown type); evidence/controller-mode-live.json. Test actual no-flight behavior in UI-only; external Steam device routing remains uncertain, not a bridge cleanup failure claim.

## Gaze/Smart HUD review candidate
Actual-reference stage: evidence/gaze-hud-review-build.txt. Deployed receipt/integrity: evidence/gaze-hud-deploy.json and gaze-hud-integrity.json. Pure HUD/guard: gaze-hud-numeric-tests.txt. Runtime: gaze-hud-live.json; bounded capture: gaze-hud-capture.txt. Hardware acceptance remains pending.

Live checkpoint: process23300; native gaze extension enabled, feature enabled, requested=true, gazeTracked=false, device-unavailable:check-runtime-sharing. Menu source None is expected before cockpit cue updates. Smart status and Head helmet tracking active. Current log shows both Frame models loaded and read-only gaze/HUD tool registered; no numeric patch-skip warning. This does not establish rendered HUD or actual eye tracking acceptance. Paired recorder waits at playtest/sessions/20261004T042133680Z-seated-head.

## First HUD feedback and revised retest
86sample source summary: evidence/hud-gaze-seat-feedback.json. Runtime type collision retained in hud-gaze-seat-feedback-player.log. Reviewed build: hud-gaze-seat-build-reviewed.txt; final live integrity/receipt: hud-gaze-seat-integrity.json and hud-gaze-seat-deploy.json. Source snapshot530/payload34verified. New startup inventory: hud-gaze-seat-live.json. Capture log: hud-gaze-seat-capture.txt. Earlier passes superseded before deployment. Actual headset retest pending.


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
