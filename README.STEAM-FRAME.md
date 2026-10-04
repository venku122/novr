# NOVR Steam Frame support

Development fork of [InfernoSuperNova/novr](https://github.com/InfernoSuperNova/novr),
which derives from UUVR. This is an experimental Windows SteamVR/OpenXR development
branch, not an upstream release. Keep the original project's licensing and notices.

## What works and what is still under review

Tested by a Steam Frame user on Windows 11 with SteamVR, Nuclear Option 0.34.1,
BepInEx 5.x and HOTAS:

- Tracked controller UI pointer, trigger selection, grip hand switching,
  controller models and responsive motion.
- In-headset UI + HOTAS / UI + GAMEPAD setting and stable seated head tracking;
  a mission was completed with head tracking centered.
- Mission pause → **VR UI SETTINGS**, head-following kill feed and level-up notices.

The newest compact minimap and off-axis HUD layout need headset review.
Eye tracking remains **experimental and unverified**: previous builds did not
receive usable gaze. This branch fixes a Unity native PoseControl compatibility
problem and adds evidence collection; that is not proof native gaze now works.
Controller reconnect, dashboard/focus transitions, all aircraft and other
controllers still need the full regression matrix. Linux/Proton Frame behavior is
not validated here. No eye-tracking or foveated-rendering success is claimed.

## Controls and settings

Use SteamVR with its OpenXR runtime active. Controller discovery supports the
native Frame profile and compatible controller data; diagnostics report the actual
backend, devices, profile and tracking state rather than relying on device names.

- Controller ray → UI hover; trigger → select/drag; thumbstick → scroll.
- Press the desired hand's grip to switch the pointer. Models use the tracked pose.
- **UI + HOTAS** is the default: controllers handle UI, HOTAS handles flight.
- **UI + GAMEPAD** optionally outputs a virtual gamepad for Nuclear Option's normal
  gamepad bindings, including flight. See [gamepad prerequisites](NOVR.Gamepad/README.md).
  Mouse, keyboard and HOTAS remain available. Flight is never enabled implicitly.
- Recenter comfortably seated. Aircraft transitions preserve the manual physical
  reference rather than recentering while you lean during mission entry.
- In mission pause, open **VR UI SETTINGS**; BACK returns to pause, Resume closes it.

HUD modes include Aircraft, Helmet and Smart. Smart switches status with head angle
and clears it when looking down at instruments. New **Compact** detail puts weapon
and numeric readouts toward the lower-right, keeps secondary status aircraft-fixed,
and gives the minimized map its own small left/down placement. **Full** follows more
status. Targeting/directional symbols retain their game registration; HUD never
follows eye movement. Adjust minimap angular size, status scale and detail in pause
settings; full tactical map layout and geographic coverage are retained.

Eye tracking is optional and off by default. Enable it and use Eye Preferred (head
fallback), Eye Only (no invalid center selection), or Head. Frame must provide
application gaze through `XR_EXT_eye_gaze_interaction`; headset foveated streaming
alone does not prove gaze is shared with the game. Native gaze aims the targeting
cue; normal Select still selects. It does not bypass sensors, automatically spot,
select, fire, or change graphics. Check actual **Gaze / Sight** status.

## Debug and Clean builds

Prerequisites: Windows .NET SDK 8+, .NET Framework 4.8 reference assemblies, Nuclear
Option and existing BepInEx 5.x. These scripts build runtime projects without the
GUI installer or obsolete native XInput project. Set `NUCLEAR_OPTION_GAME_DIR` or
pass `-GameDirectory`; no username, Steam account or HOTAS model is required.

```powershell
$game = $env:NUCLEAR_OPTION_GAME_DIR
./scripts/build-dev.ps1 -GameDirectory $game -BuildFlavor Debug -StageDirectory ./staging/frame-debug
./scripts/build-dev.ps1 -GameDirectory $game -BuildFlavor Clean -StageDirectory ./staging/frame-clean
```

Each stage must be new. Builds are isolated from the installed game and can run
while playing; no build deploys automatically. Both run deterministic logic and
Windows safety tests. Debug uses Debug configuration, portable symbols and the
optional localhost MCP development bridge. Clean uses optimized Release, omits
symbols and the bridge, and keeps expensive instrumentation disabled by default.
Clean retains optional runtime diagnostics/settings; it does not rewrite existing
user configuration. The default Developer flavor retains the original development
Release + bridge workflow. Neither flavor bundles BepInEx or game reference DLLs.

Package a verified stage for sharing (payload and notices only; no sessions or logs):

```powershell
./scripts/package-dev.ps1 -StageDirectory ./staging/frame-debug -OutputZip ./artifacts/NOVR-SteamFrame-Debug.zip
./scripts/package-dev.ps1 -StageDirectory ./staging/frame-clean -OutputZip ./artifacts/NOVR-SteamFrame-Clean.zip
```

Legacy solution builds still have automatic deployment unless explicitly disabled:
`dotnet build NuclearOptionVirtualRealityMod.sln -c Release -p:NovrAutoDeploy=false`.
Prefer the staging scripts above.

## Install, deploy and rollback

Fork prerelease ZIPs contain a `game/` payload, `build.json`, this README, licensing,
and the guarded deployment helpers. Extract into a **new directory**, not directly
into the live game. Use the included `scripts/deploy-dev.ps1` with `-StageDirectory`
pointing at that extracted directory, or use the matching scripts from the source.

```powershell
# NuclearOption.exe must be stopped; deployment refuses otherwise.
./scripts/deploy-dev.ps1 -StageDirectory ./staging/frame-clean -GameDirectory $game
# Restore the preceding deployment, again with the game stopped.
./scripts/rollback-dev.ps1 -GameDirectory $game
```

Deployment validates payload hashes, backs up replaced files, records commit/build
ID, and installs matching NOVR-owned managed XR assemblies before Unity loads them.
It preserves unrelated mods. Switching an existing Debug/Developer installation to
Clean does not automatically remove an old `NOVR.McpBridge.dll` or old PDBs: back up
and remove that optional bridge/symbols while stopped if a strictly clean install
is required. The Clean ZIP itself contains neither. No version of these commands
should deploy during gameplay. Launch later through Steam; the known local Frame
renderer path is DirectX 11 (`-force-d3d11`), not a blanket graphics change.

## Diagnostics and hardware review

Verbose input and telemetry are off by default; use snapshots or bounded sessions.
Debug's local bridge exposes read-only head, gaze/HUD and UI-object snapshots;
network/AI work stays outside Unity's frame loop. Do not expose it to a network.

```powershell
./scripts/capture-head-dev.ps1 -IncludeGazeHud -WaitForCockpitSeconds 1800 -CaptureSeconds 60
```

Startup gaze inventory is sampled every five seconds into a separate file;
only actual cockpit samples count as cockpit evidence. Sessions/logs/audio/config
are local artifacts, not included in builds. [Playtest helper](tools/vr-playtest/README.md)
is optional and requires Node 22+; it is not a prerequisite for controller UI.

Review compact map readability and full-map clicks/zoom, off-axis center clearance,
eyes-only cue movement with head still and normal Select, then controller/HOTAS,
mouse/keyboard, seat and notification regressions. Report actual Gaze/Sight status
and evidence, not just the enabled checkbox. See [hardware review](docs/development/frame-gaze-hud-review.md)
and [gaze diagnostics](docs/development/frame-gaze-input.md).
