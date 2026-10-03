# Steam Frame development milestones

Native goal is active; no native todo tool is exposed in this session. STATE.json is the status authority.

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
- [ ] Physical baseline input/session capture.
- [ ] Live deployment/rollback/SteamVR launch acceptance.

Next: docs/development/steam-frame-baseline.md. M1/M2 await actual runtime data; M5-M8 remain queued.
