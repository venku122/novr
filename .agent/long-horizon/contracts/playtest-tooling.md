# Independent playtest tooling preparation

Outcome: a TypeScript external session/capture utility with bounded FFmpeg microphone capture, local Windows speech transcription, associated Unity snapshot/screenshot/log evidence, and explicit observation/investigation/implementation/deploy boundaries. Add an optional read-only HOTAS/keyboard capture bridge only after service tests pass; do not change controller/profile/UI/flight behavior.

This prepares M5/M7 components independently of the physical M0 gate, rather than treating the whole campaign as blocked. M0/M1/M2/M3 and hardware integration acceptance remain unsatisfied. No live deployment or actual microphone capture during preparation. Test recording against synthesized WAV data, local recognizer against synthesized speech, and session associations against fixtures. Do not call a paid AI service during tests.

Files: tools/vr-playtest/{package.json,tsconfig.json,src/{session,commands,logs,telemetry,audio,capture,codex,service,cli}.ts,test/*.test.ts}; Windows transcription helper; optional NOVR/Diagnostics/PlaytestCaptureBridge.cs and existing configuration/Core integration. Source edits only on isolated branch.

Invariants: mic never starts at service startup; each recording <=30 seconds; release/exit/failure terminates recorder; transcripts are evidence, never executable instructions; no shell interpolation; captured artifacts remain under configured game/session roots with freshness checks and explicit missing states. Observation-only commands never invoke source-edit/AI/deploy actions. Codex invocation is user-commanded and isolated; read-only investigation sandbox, workspace implementation, explicit deployment only through existing stopped-game script. Off has no Unity component.

Rollback: revert this preparation commit; installed game untouched. Tests first for session persistence, malformed/stale requests, incomplete artifacts, recorder failure/reconnect, transcripts containing instructions, command classification and prepared Codex argv. Maximum three unchanged attempts per failure signature; change strategy on deterministic errors.
