# NOVR Steam Frame acceptance matrix

Status as of diagnostic preparation: **overall incomplete**. Automated safety tests do not verify headset input or VR behavior. Milestone authority is `.agent/long-horizon/STATE.json`.

| Gate | Status / evidence required |
|---|---|
| Installed version, upstream dev architecture, config and historical logs inventoried | Verified locally; research report and installed hash manifest |
| Failing Steam Frame NOVR session reproduced | Pending physical baseline test |
| Runtime and actual NOVR loader/profile evidence | Diagnostic code built; pending actual session JSON |
| Both controllers detected; stable left/right identity | Pending device/usage/profile snapshots |
| Valid poses using runtime tracking flags | Pending raw state; old nonzero-pose heuristic untouched |
| Trigger, grip, thumbsticks/clicks, face/menu/view/shoulder/touch states | Pending held-control snapshots; unavailable controls explicitly reported |
| Native Frame versus Touch compatibility capability selection | Pending actual extension/profile/action evidence; no profile change yet |
| Haptic capability and successful actuation | Capability capture built; physical actuation pending |
| Stable UI ray, hover, reliable single click | Existing adapter untouched; pending M1/M2 |
| Drag without repeated clicks; scrolling; back/confirm | Pending semantic actions and physical verification |
| NOVR settings / game front-end / in-flight UI operation | Pending physical UI matrix |
| Right/Left/Auto, one-controller operation, reconnect | Pending abstraction changes and real tests |
| HOTAS flight controls unchanged | No routing changes; real simultaneous flight test pending |
| Mouse and keyboard coexistence | No routing changes; real simultaneous UI test pending |
| Controllers never bind to flight controls automatically | No new flight mappings; verify real game controller emulation behavior |
| No OS mouse effects after exit | No OS-global injection added; actual exit test pending |
| Existing controller compatibility | Pending native/Touch/other-controller tests |
| Staging build succeeds without live writes | Verified real Windows runtime/patcher/bridge build, installed hashes and MSBuild fixture test |
| Process guard, integrity, allowed paths and junction safety | Verified disposable-tree tests, including inverse game alias |
| Deployment/rollback recovery and unrelated files preserved | Verified disposable-tree tests and injected late receipt failure |
| Successful actual stopped-game deployment / rollback | Not exercised on live game |
| Steam launch/attach, dashboard transitions and log collection | Lock/collector tests pass; actual Steam launch/headset pending |
| Speak observation → audio → transcript → durable structured record | Pending M5; no microphone/transcription provider configured |
| Observation-only commands cannot edit source | Pending out-of-process command policy tests |
| Screenshot associated with spoken observation | Pending M5 |
| Camera local/world, XR origin, parent, cockpit and seat chain | Pending M6; no camera magic offsets changed |
| Canvas/ray hit identification and wrapper transform | Pending M6 |
| Render scale/resolution plus refresh/frame/GPU settings | Partial snapshot fields built; full performance evidence pending |
| Codex receives complete session and distinguishes investigate/implement/deploy | Pending M7 |
| Codex builds while game runs; explicit deploy after exit; rollback retained | Build safety verified, full voice/in-headset loop pending M8 |
| Revoker clipping, floating UI, graphics quality integration cases | Pending recorded hardware cases |
| Disabled telemetry overhead and bounded enabled cost | Off has no component/polling; limits tested; in-headset frame-time check pending |
| Eye tracking/foveation/gesture/controller models | Stretch goals deferred |
