# NOVR Steam Frame development contract

The user directive is the binding specification. Build an upstream-quality branch that permits fly → speak → capture evidence → diagnose → patch → staging build → quit → deploy → relaunch → retest. Steam Frame controllers operate UI while HOTAS flight input, mouse and keyboard continue working.

## Deliverables and gates
M0 records actual runtime/backend, both controller identities/poses/raw controls/profiles before changing behavior. M1 selects native Frame OpenXR if clean, otherwise verified Touch compatibility, otherwise SteamVR actions, then a bridge only as last resort. M2 extends existing controller adapter/EventSystem UI path with semantic point/select/back/scroll/context/menu/recenter/capture actions and Right/Left/Auto selection. M3 verifies every protected input path. M4 stages builds separately and tests stopped-game deploy and rollback. M5 stores spoken observations, audio/transcript, screenshot, log tail and session metadata. M6 records camera transform chain, aircraft/seat/origin, controller profiles, canvas/ray hits and rendering/frame data. M7 gives out-of-process Codex full evidence with observation/investigation/implementation/deployment boundaries. M8 demonstrates the complete workflow on actual hardware.

## Constraints
No live installation writes during build. Deployment checks NuclearOption.exe stopped, successful integrity-checked stage and rollback before writing. Preserve unrelated changes and existing plugins/configuration. No OS-global cursor injection, automatic flight bindings, paid AI calls, secrets in Git, upstream pushes/merges or speculative magic camera offsets. Debug costs disabled by default; bounded snapshots/sampling; no AI calls on Unity thread. Native Frame API availability must be established from actual runtime, not device names. Hardware success cannot be inferred from unit tests or compilation.

## Current attempt
The baseline lacks device/profile/raw input evidence. Extend the existing diagnostic and controller code, without changing bindings, profiles, pointer behavior or cockpit offsets. Independently establish safe build/deploy tooling so this evidence package is reviewable and reproducible. Stop at the physical baseline checkpoint before dependent UI implementation. Stretch goals remain deferred.

## Authority and resource limits
User explicitly authorizes autonomous local inspection, research, diagnostics, edits, tests and staging; manual headset testing at the final checkpoint. No token/time budget requested. Use a single executor and deterministic verification; max three unchanged attempts per task. Rollback is preserved by immutable build packages and file manifests, with live backups only when deployment is requested.
