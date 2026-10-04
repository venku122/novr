# Local evidence index

Ignored evidence under evidence/ contains installed-baseline.json, baseline-build.txt, diagnostic-build.txt, windows-build-dev-final.txt, diagnostics-tests.txt, build-safety-tests.txt, deployment-tests.txt, launch-safety-tests.txt and negative reproducer logs. Public summary: reports/steam-frame-baseline-research.md. Staged payload build.json hashes every deployable file; source-files.json records source provenance. Reviewer disposition is captured in independent-review.txt. No actual hardware session recorded yet.


Playtest preparation: playtest-tests-linux.txt (15 pass, one Windows fixture skipped), playtest-tests-windows.txt and playtest-stage-build-final.txt (all 16 Windows tests pass including synthesized speech through FFmpeg -> local transcript -> observation). playtest-gesture-tests.txt covers C# privacy/hold/release/timeout/lifecycle. playtest-build-safety.txt covers empty intermediate roots plus existing build safety. playtest-review.txt records repaired independent review findings. playtest-stage-integrity.json verifies final stage/source hash and unchanged live DLL/config. No real microphone or headset capture performed.
