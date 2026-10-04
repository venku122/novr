# Decisions

- Base the isolated checkout on upstream dev 2a0f789e7674 to match the installed development build, not newer main. Preserve the existing downloaded source.
- Extend VrControllerInput and XrStartupDiagnostics rather than create a competing input abstraction. M0 instrumentation is read-only; profile/pointer/flight/camera changes wait for raw hardware evidence.
- Existing SteamVR source project is empty; evaluate native Frame then existing Touch compatibility before a SteamVR rewrite. Valve source is research-only at this checkpoint.
- Windows runtime-only iteration needs SDK 8; full net9 installer/legacy native XInput solution is outside this runtime build. Keep legacy release build compatible; development passes NovrAutoDeploy=false globally.
- Reject reparse-point ancestry on both game and stage paths, and on deployment metadata/payloads. Repeated source/glob issue got a focused reproducer rather than retrying identical builds.
- Reviewer found five build/deploy/launch safety defects; fixes and regression tests precede final stage. No live deployment, configuration change or actual game launch performed.
- Native goal created. Native todo API is absent in this tool session; use STATE.json plus the durable checklist, explicitly report this limitation.

- Ruling: prepare external M5/M7 tooling while M0 awaits a real headset trace. These are independent interfaces and local tests; no controller/profile/UI implementation or hardware milestone is advanced. The earlier sequential milestone graph described acceptance order, not a prohibition on independent tooling preparation.
- Windows exposes Steam Streaming Microphone, FFmpeg and an en-US local speech recognizer. Use configured device selection and local transcription; no microphone is recorded during unattended development. Node Windows test runtime is downloaded from official nodejs.org and SHA256 checked in Mayor's local tool directory; product code contains no machine-specific path.
