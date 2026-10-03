# Steam Frame / NOVR baseline research — 2026-10-03

## Observed local state

Installed NOVR version is `0.4.4-dev+2a0f789e7674`, matching upstream dev `2a0f789e76741f307a33b2d28c2753b32fb47a64`. Latest main inspected was `990f3db`. A clean clone and dedicated `feat/steam-frame-diagnostics` branch preserve existing downloaded source and all unrelated Mayor changes. Initial runtime compilation passed with 413 pre-existing warnings. Windows SDK 8 is available; full solution installer projects require net9 and were not built.

The installed configuration uses Auto controller cursor mode and disables experimental SteamVR controller profiles and startup diagnostics. NOVR dev has `VrControllerInput`, `VrControllerLaser`, `VrUiCursor`, native menu adapters and direct EventSystem dispatch. VrControllerInput uses Unity InputSystem XR pose/trigger actions with generic XRController paths; it currently treats nonzero pose values as validity. That heuristic and coordinate transforms need later review against actual tracking flags; no behavior change is made in this baseline.

VrTogglerManager chooses XrPluginOpenXrToggler. NOVR.SteamVR has a project but no source implementation. NOVR.XR.OpenVR has provider code but is not selected by this manager. The existing optional OpenXR profile bootstrap only lists Index, Vive and Simple Controller. Vendored Oculus Touch profile code exists but is absent from that list. Actual runtime settings/action bindings require an in-process trace; presence of profile source does not establish that it is enabled.

Windows OpenXR registry selects SteamVR's `steamxr_win64.json`. Running vrserver's saved startup log identifies SteamVR 2.17.10. September 30 logs show the Frame controller driver, left/right models, input profile and streaming client version `0.4.2 (20260928.6175029)`. These are historical host/streaming observations, not a new actual NOVR session or a verified Frame OS version. Saved NOVR logs show locked XR assembly copy failures; payload/assembly provenance matters when diagnosing runtime drift. Installed plugin/patcher/config SHA256 baselines are preserved locally and rechecked after builds.

## Hypothesis, not established cause

The active NOVR configuration may leave the needed compatibility action profile unavailable, or runtime input may exist while NOVR's generic actions fail to resolve. Current saved logs cannot distinguish these. There is also a known pose-validity heuristic that can mistake stale/nonzero values for tracking. Do not enable profiles or fix pointer behavior until raw/profile/action evidence identifies the failing layer.

## Current primary upstream sources

- [NOVR dev source](https://github.com/InfernoSuperNova/novr/tree/dev): controller adapter, direct pointer, startup diagnostics, native UI and build targets inspected directly.
- [NOVR controller issue 47](https://github.com/InfernoSuperNova/novr/issues/47): similar no-menu-input report on Quest/WiVRn; not Steam Frame evidence.
- [NOVR cockpit drift issue 49](https://github.com/InfernoSuperNova/novr/issues/49): repeated aft/left spawn drift report; supports investigating recenter/transform handling, not applying a magic Revoker offset.
- [NOVR open PR 50](https://github.com/InfernoSuperNova/novr/pull/50): HOTAS UI-navigation drift, waypoint projection and map cursor depth changes. Scope overlaps UI coexistence; inspect before changing that layer. No PR code copied or merged here.
- [Valve Unity integration documentation](https://partner.steamgames.com/doc/steamhardware/steamframe/engines/unity): documents native Frame profile package and default Oculus Touch emulation. Runtime verification is still necessary for the user's streamed PC session.
- [Valve SteamFrameControllerProfile source](https://github.com/ValveSoftware/Unity/blob/main/com.valvesoftware.openxr.utils/Runtime/Interactions/SteamFrameControllerProfile.cs): defines `XR_VALVE_frame_controller_interaction`, `/interaction_profiles/valve/frame_controller_valve`, and a SteamFrameController layout. Native profile is a plausible small extension to NOVR's existing feature bootstrap; exact runtime support and ABI must be tested first. Research source saved under ignored evidence only, not vendored into runtime.
- [Valve SteamVR Input architecture](https://valvesoftware.github.io/steamvr_unity_plugin/articles/SteamVR-Input.html): action manifests and controller bindings provide an alternative when needed; the NOVR.SteamVR project alone is not an implementation.

## Prepared change

Read-only JSON snapshots report runtime/loader, both profiles, extension availability/enabled state, Unity XR devices/feature values/haptics, InputSystem devices/control values and existing NOVR binding resolution. Expensive instrumentation defaults Off. Session mode has 10 Hz/60-second/600-sample bounds plus a total 720-file cap. Existing pointer, HOTAS, mouse, keyboard and cockpit behavior remains untouched.

Safe PowerShell runtime staging builds explicitly suppress the shared DeployToGame target, preserve source provenance and require automated tests before publishing build.json. Explicit deploy/rollback checks process state, payload integrity, path ancestry and deployment locks, preserves old files, and journals recovery. Launch holds the deployment lock until game-process detection. Live deployment and actual Steam launch are intentionally not exercised in this preparation.
