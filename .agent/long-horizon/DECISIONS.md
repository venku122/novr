# Decisions

- Base the isolated checkout on upstream dev 2a0f789e7674 to match the installed development build, not newer main. Preserve the existing downloaded source.
- Extend VrControllerInput and XrStartupDiagnostics rather than create a competing input abstraction. M0 instrumentation is read-only; profile/pointer/flight/camera changes wait for raw hardware evidence.
- Existing SteamVR source project is empty; evaluate native Frame then existing Touch compatibility before a SteamVR rewrite. Valve source is research-only at this checkpoint.
- Windows runtime-only iteration needs SDK 8; full net9 installer/legacy native XInput solution is outside this runtime build. Keep legacy release build compatible; development passes NovrAutoDeploy=false globally.
- Reject reparse-point ancestry on both game and stage paths, and on deployment metadata/payloads. Repeated source/glob issue got a focused reproducer rather than retrying identical builds.
- Reviewer found five build/deploy/launch safety defects; fixes and regression tests precede final stage. No live deployment, configuration change or actual game launch performed.
- Native goal created. Native todo API is absent in this tool session; use STATE.json plus the durable checklist, explicitly report this limitation.
