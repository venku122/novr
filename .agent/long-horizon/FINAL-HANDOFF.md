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
