# NOVR Steam Frame implementation plan

**Goal:** satisfy the user directive with actual hardware evidence.
**Architecture:** extend upstream dev's VrControllerInput, VrUiCursor, profile bootstrap and startup diagnostics. External TypeScript playtest/Codex orchestration consumes versioned snapshots. MSBuild stages; explicit PowerShell deployment owns live writes.
**Spec:** SPEC.md. Execute inline; user authorized execution without intermediate approval requests.

## Current bounded tasks
- [x] Inspect upstream main/dev, public issues/PRs, Valve current docs, installed commit/config/logs and build baseline.
- [ ] Add a runtime-safe current-profile query in NOVR.XR.OpenXR/OpenXRInputDiagnostics.cs using existing native helpers, without registering a new profile.
- [x] Add NOVR/Diagnostics/InputDiagnosticSnapshot.cs plus a bounded capture component; enumerate Unity XR feature values, InputSystem controls and existing resolved action bindings, distinguish unavailable from zero, serialize snapshots with UTC/frame identity.
- [x] Add Off/Snapshot/Session diagnostics configuration and lifecycle integration; no behavior changes.
- [x] Add opt-in suppression of DeployToGame in NOVR.Build/NOVR.Build.targets and build-dev.ps1 producing clean versioned staging plus hashes. Default legacy behavior documented separately.
- [x] Add deploy-dev.ps1 and rollback-dev.ps1 with stopped-game checks, manifest/hash/path validation, exact previous-file backups and recovery; test temporary game trees, never live installation.
- [x] Build modified runtime/patcher; verify live binary/config hashes unchanged; document baseline headset test with exact artifacts.
- [ ] Run hardware baseline checkpoint; interpret profiles, resolved bindings and raw controls before M1/M2.

## Later dependency-gated work
M1/M2: native Valve profile versus Touch compatibility versus SteamVR actions, based on M0. Test action mapping, handedness, reconnect, click/drag/scroll state before UI changes. M3 tests simultaneous HOTAS/mouse/keyboard in menus and flight. M4 launch attaches a session and tails logs. M5 TypeScript audio/capture/session/classification with local configured transcription provider, bounded push-to-talk and screenshots. M6 camera/seat/UI/render snapshots validate clipping and floating UI. M7 command policy keeps capture read-only and deploy explicit. M8 records complete integration matrix.

## Review focus
Unavailable native session yields unknown profile; zeros never prove missing hardware. Unity XR and InputSystem may disagree. Session sampling caps persist across Core recreation. Builds with reference paths must never deploy transitively. Deploy/rollback rejects tampered/path-traversal/symlink payloads and preserves extra user plugin files.

## Controller-first checkpoint (latest steering)
- [x] Register pinned Valve Frame profile and Touch compatibility behind restart-required opt-in.
- [x] Extend existing EventSystem pointer; hand preference, pose validity/calibration, hysteresis, balanced click/drag cancellation, scroll and Back/Menu.
- [x] Optional gaze diagnostics/reticle and Head helmet HUD mode compiled/reviewed.
- [x] Capability-gated short controller activation pulse and cached pointer/canvas snapshots prepared; final build validation in evidence.
- [ ] Physical untouched failing-session baseline, then actual controller UI/HOTAS/fallback/reconnect/dashboard matrix.
- [ ] Physical eye gaze sharing/blink/calibration and helmet HUD/recenter/multiple-aircraft matrix.

Voice/video development is deferred. Opt-in source changes stay staged pending baseline evidence; compilation and review do not accept M0-M3, gaze or HUD. Follow docs/development/steam-frame-controller-test.md.
