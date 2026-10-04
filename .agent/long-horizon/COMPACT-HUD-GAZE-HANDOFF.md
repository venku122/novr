

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
