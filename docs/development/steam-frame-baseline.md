# Steam Frame baseline checkpoint

This is a diagnostic build, not a controller fix. The existing controller/profile/UI/camera behavior is unchanged. M0 needs an actual failing-session trace before selecting native Frame, Touch compatibility or another input path. The overall goal and milestones remain open.

## Setup

Open Windows PowerShell at this repository. In this environment the source checkout is:

```powershell
Set-Location '\\wsl.localhost\Ubuntu\home\tjt\src\llm\mayor\projects\nuclear-option\mods\NOVR'
$env:NUCLEAR_OPTION_GAME_DIR = 'D:\SteamLibrary\steamapps\common\Nuclear Option'
```

For other machines, use your checkout and game folder. Development runtime builds need .NET SDK 8+ and .NET Framework 4.8 references. This loop builds NOVR, NOVR.Patcher and NOVR.McpBridge, not the .NET 9 installer or legacy native XInput project. The original full solution release build remains a separate workflow.

Build safely, including while the game is running:

```powershell
./scripts/build-dev.ps1
```

The command prints a new staging directory and build ID. It records the commit, complete source file hashes (including untracked files), tracked diff, compiler output, payload hashes and successful test gates. Failed builds have no successful build manifest. It never deploys. The original release build still auto-deploys unless passed `-p:NovrAutoDeploy=false`; use the development script for iteration.

When ready to test, close Nuclear Option and explicitly deploy the staged build:

```powershell
./scripts/deploy-dev.ps1 -StageDirectory '<READY staging directory>'
```

Deployment verifies stopped-game state, hashes, allowed paths, directory ancestry and a deployment lock, then preserves exact old files. Existing extra plugins/assets/configs are left in place. Rollback restores overwritten files and removes files introduced by this deployment. Stage payloads update patcher XR files; the normal NOVR patcher copies these into NuclearOption_Data on the next startup. Do not start the game externally during deploy or rollback.

## Enable this one baseline capture

With the game stopped, back up `BepInEx/config/deltawing.novr.cfg`. In its existing `[Diagnostics]` section add/update:

```ini
Log XR Startup Diagnostics = true
Raw Input Mode = Session
Raw Input Snapshot Shortcut = F10
```

Keep `Verbose Diagnostics = false`. Do not enable experimental controller profiles, change cursor mode, change cockpit offsets or adjust graphics for this first baseline. Those changes would obscure the failing behavior. Unknown raw mode values fail closed to Off. Modes are selected at startup; restart to switch Snapshot/Session.

Session mode samples at up to 10 Hz for the first 60 seconds after startup, capped at 600 automatic samples. Startup/device/profile changes and F10 produce additional snapshots, with a process-wide total cap of 720 files. These limits survive Core recreation. Snapshot mode omits continuous sampling. Off adds no diagnostic component or polling. Diagnostics read raw input and existing action bindings without sending input or changing actions. Feature/control names come from the APIs; unsupported values are `unavailable`, not false or zero. Unity XR exposes no public device ID in this version; hand identity comes from characteristics and InputSystem usages.

## Exact physical test (about three minutes)

1. Connect HOTAS, mouse and keyboard. Turn on both Frame controllers and stream through SteamVR. Have the headset ready before launch.
2. Run `./scripts/launch-dev.ps1`. This launches Steam app 2168680 using NOVR's existing OpenXR path, tails BepInEx logs and collects the evidence when the game exits. `-Attach` watches an already running game. Actual Steam/headset launch remains unverified until this test.
3. During the first minute, move each controller separately. Hold each trigger for two seconds; release. Move each thumbstick fully in X/Y, click it, squeeze each grip, press each available face/shoulder/menu/view control, and touch controls where supported. State aloud which hand/control you test if taking your own optional recording; the voice capture subsystem is not implemented yet.
4. Attempt to point at a front-end menu button and select it. Record whether a ray/cursor/hover/click appears, separately. Confirm mouse and keyboard fallback still work. Press F10 for a snapshot while the failure is visible.
5. Disconnect/reconnect one controller and press F10 after reconnect. Open/close the SteamVR dashboard and press F10 again. Keep the other controller powered on for this part.
6. Enter the Revoker cockpit if practical. Confirm HOTAS still flies the aircraft. Describe the aft/clipping problem, and take a normal Steam screenshot if useful. This build records aircraft and input/render data; complete camera/seat transform telemetry is pending M6.
7. Exit normally. The launcher prints `COLLECTED:` with a durable `playtest/sessions/<timestamp>-steam-frame-baseline` directory. It contains session metadata, NOVR config, BepInEx log, SteamVR log tail and raw input snapshots. No new snapshots means diagnostics did not run; historical logs alone do not satisfy M0.
8. Restore the saved configuration or set `Raw Input Mode = Off` and `Log XR Startup Diagnostics = false` for normal play. To restore previous binaries: `./scripts/rollback-dev.ps1` while the game is stopped. The deployment receipt/backup is preserved locally under BepInEx/NOVR/dev-deployments.

Send Codex the collected session path and the observed ray/hover/click results. The required evidence is: actual loader/runtime, native extension availability, profile per hand, device identities/tracking flags/poses, button/axis values while held, existing NOVR action bindings/resolved controls, reconnect transitions, and fallback/HOTAS observations. Missing Frame-specific buttons under a compatibility profile are an explicit limitation, not a reason to guess bindings.

## Acceptance boundary

Automated tests cover diagnostic limits and build/deploy safety. Actual controller pointing, menu operation, flight coexistence, reconnect and headset performance are not yet verified. Voice capture, full camera/UI telemetry, Codex orchestration and the complete iteration loop remain queued behind the relevant evidence gates. This checkpoint does not claim the project is complete.
