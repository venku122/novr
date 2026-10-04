# Current hardware checkpoint

Overall native goal remains active and incomplete. Branch feat/steam-frame-diagnostics. Baseline input diagnostics, safe staging/deploy/rollback and external capture preparation are tested. Actual controller profile selection/UI pointer behavior, HOTAS coexistence, full cockpit/origin/canvas telemetry, actual Codex execution and complete in-headset loop remain unverified. No live installation changed and no real microphone recorded during development.

Prepared stage: staging/20261004T001621908Z-f401af08; build dc5af227d69b4941b42b80dbf43455e6; code commit 7d44cf52f982eb1f446fc9d8f3dc6b524979be21. All 28 payload hashes/source provenance verified. Runtime build succeeds with 427 warnings and zero errors; patcher/bridge builds clean. C# gesture, deployment/rollback, mocked launch and all 16 native Windows playtest tests pass. Synthetic speech through FFmpeg/local recognizer produces a durable camera observation; that does not prove microphone/headset behavior.

Next exact manual test: follow docs/development/steam-frame-baseline.md using the stage above, with Playtest capture left disabled for the original controller baseline. Return COLLECTED session directory and actual ray/hover/click, both-hand/raw-button, dashboard/reconnect and HOTAS/mouse/keyboard results. No input/profile/offset/graphics changes should obscure this trace.

After collecting that baseline, optionally perform the separate voice test at the end of that guide and tools/vr-playtest/README.md: helper before game, explicit microphone, unused key/button, hold/speak/release two observations, exit game/quit helper, return SESSION directory with artifact/error states. No continuous recording. No voice text executes source changes or deployment. Capture requests are not microphone acknowledgments.

A SHA256-verified portable Windows Node v24.21.0 was downloaded for local testing outside this repo. In PowerShell from the NOVR root, its local executable can be resolved as `(Resolve-Path '../../../../.tools/node24-win/node-v24.21.0-win-x64/node.exe').Path`; use that executable in place of `node` in the helper commands if Node is not on PATH. Product tools use configured executable paths/PATH, never this machine-specific location.

Deployment and rollback are explicit stopped-game scripts. The staging build never deploys. Preserve actual input evidence before selecting controller architecture or changing VR UI.
