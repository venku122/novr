# NOVR external playtest helper

This is development tooling, not a replacement for NOVR input. Audio, local transcription, session persistence and Codex preparation run outside Unity. Controller/UI behavior and real-headset acceptance are still pending the baseline trace.

Requirements: Windows Node.js 22+, FFmpeg on PATH (or `--ffmpeg`), an explicitly selected Windows DirectShow microphone, and a local System.Speech recognizer for English. No API key, upload or paid transcription is required. Windows recognition quality is not yet measured on Steam Frame audio; original WAVs and recognizer confidence are retained for correction. Install/build once from this directory:

```powershell
npm ci --ignore-scripts
npm test
# Optional native Windows synthetic audio validation; does not record a microphone:
$env:NOVR_TEST_WINDOWS_AUDIO = '1'
npm test
Remove-Item Env:NOVR_TEST_WINDOWS_AUDIO
```

`scripts/build-dev.ps1` also compiles/tests this helper when Windows Node is on PATH or supplied via `-NodeExecutable` / `NOVR_NODE_EXECUTABLE`. Without optional Node it explicitly reports the skipped helper suite in `test-summary.json`; runtime C# and deployment safety checks still run. Set `NOVR_TEST_WINDOWS_AUDIO=1` to include the synthetic Windows audio fixture.

From the repository root, start the helper **before** pressing any capture button:

```powershell
$env:NUCLEAR_OPTION_GAME_DIR = '<your Nuclear Option directory>'
node tools/vr-playtest/dist/src/cli.js serve --device 'Microphone (Steam Streaming Microphone)' --requests 'BepInEx/NOVR/playtest/requests'
```

Select the microphone by its actual FFmpeg device name; the streaming microphone name above was enumerated on this test machine. Run `ffmpeg -list_devices true -f dshow -i dummy` to list local devices. Merely starting `serve` never opens a microphone. The helper prints the session directory and accepts `start`, `stop`, `note TEXT`, `snapshot`, and `quit`. Ctrl+C/quit releases any recorder and drains pending observations. The recorder stops on release or after at most 30 seconds; startup/failure watchdogs are also bounded. Transcription runs after release and does not block request polling. At most eight observations may be awaiting finalization.

For the opt-in in-game shortcut, deploy the staged development build while the game is stopped, then set these in `BepInEx/config/deltawing.novr.cfg` and restart:

```ini
[Playtest]
Enable Capture = true
Capture Shortcut = F11
Capture Screenshot = true
```

Use an unused key or Unity legacy joystick KeyCode, or map an unused HOTAS button to that key using your existing input setup. NOVR only reads it: it does not consume or remap HOTAS/keyboard input. A held key at startup cannot begin recording; release it first. Hold to capture and speak; release to finalize. Wait briefly for microphone initialization before speaking. No controller binding has been added pending the physical input baseline. The shortcut defaults to None and capture defaults off. Keep it off during the original controller baseline test.

Each gesture requests one raw XR/device/render snapshot, one game-rendered PNG and a log tail. The screenshot is a game image, not a guaranteed compositor/eye image. The external helper waits briefly for a complete PNG. The snapshot currently contains raw HMD/controller poses, aircraft and rendering state; the full cockpit/XR-origin/canvas transform chain remains M6 work. No original cockpit offset or graphics configuration changes are made.

Sessions contain `session.json`, append-only `observations.jsonl`, WAV/transcript files, screenshots, telemetry, log/config copies and `diffs`. Artifacts have SHA256 hashes and explicit present/missing/error status. Fresh snapshots update the session's runtime/backend/profile/render metadata. Configured runtime and installed binary versions are labelled separately from observed runtime. Undetectable commit/Frame OS/backend fields remain unknown, never inferred from old logs. Raw sessions are ignored by Git and should be reviewed before sharing.

You can add a typed observation or capture a single explicitly bounded clip:

```powershell
node tools/vr-playtest/dist/src/cli.js note --text 'The head is too far aft' --telemetry '<game-relative fresh JSON>' --screenshot '<game-relative PNG>'
node tools/vr-playtest/dist/src/cli.js record --device '<actual microphone name>' --seconds 10
```

Observation text never launches Codex or changes source. Exact investigation/implementation/deployment phrases in a transcript become proposals only. Prepare an evidence bundle explicitly:

```powershell
node tools/vr-playtest/dist/src/cli.js bundle --session '<session directory>' --intent investigate --repository '<NOVR checkout>'
```

This verifies artifact hashes, incorporates transcripts/logs/telemetry, and prepares a prompt/job with screenshot arguments. It does **not** run Codex. Review the prepared prompt and invoke Codex explicitly, supplying that prompt on stdin using the structured job argv. Investigation arguments enforce `read-only` with user config/rules ignored. `--intent implement` prepares a `workspace-write` job with `--worktree`; it requires an isolated Git checkout and an explicit operator invocation. These argument forms were checked against the installed Codex CLI. End-to-end agent execution and headset voice command authorization remain M7/M8 validation, not completed by preparing a file.

Deployment is exclusively `scripts/deploy-dev.ps1` with a validated staging directory and the game stopped; rollback is `scripts/rollback-dev.ps1`. This helper never deploys, launches the game, modifies graphics, or injects OS mouse input.

The capture bridge publishes version-1 JSON requests atomically into `BepInEx/NOVR/playtest/requests`: UUID `id`, UUID `captureId`, UTC `timestamp`, `kind` (`start`, `stop`, `snapshot`), optional game-relative `evidence` paths. Requests predating service startup or older than 15 seconds are ignored, UUIDs are deduplicated, late starts cannot resurrect released captures, and malformed requests cannot discard later release requests. Requests are capped at 16 KiB; the bridge caps requests at 250 per game process. Archive old request/evidence files between long campaigns; the helper refuses excessive backlog. A queued request is not an acknowledgment that microphone recording succeeded. Status overlay/acknowledgment feedback is future work.

Validation so far: logic tests on Linux and Windows, real Windows FFmpeg with synthetic audio, local recognition of synthesized speech, C# gesture tests, and isolated NOVR staging builds. Steam Frame microphone, Unity screenshots, actual HOTAS shortcut, and complete in-headset capture are unverified. See [the hardware checkpoint](../../docs/development/steam-frame-baseline.md) before controller behavior changes.

Primary references: [FFmpeg DirectShow input](https://www.ffmpeg.org/ffmpeg-devices.html#dshow), [Microsoft System.Speech recognition](https://learn.microsoft.com/en-us/dotnet/api/system.speech.recognition.speechrecognitionengine?view=netframework-4.8.1), [Codex non-interactive mode](https://learn.chatgpt.com/docs/non-interactive-mode), [Codex security](https://learn.chatgpt.com/docs/security).
