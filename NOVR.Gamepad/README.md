# Optional Steam Frame gamepad

This companion plugin feeds one standard Xbox 360 gamepad from the existing
Input System XR controller states. It is **disabled by default**. When enabled,
it participates in the game's normal controller/flight bindings and remapper;
it does not create action maps or override existing user mappings.

Choose **UI + HOTAS** (default) or **UI + GAMEPAD** in NOVR **VR UI SETTINGS → CONTROLLER MODE**. The choice applies immediately and persists. HOTAS remains available in both.

`BepInEx/config/deltawing.novr.cfg`:

```ini
[VR Input]
Controller Mode = UiHotas
```

Requires an **already-installed compatible ViGEmBus**. This project never
installs, updates, configures, or downloads a driver or updater. ViGEmBus is an
end-of-life optional compatibility dependency. Missing/incompatible bus or
native-client failures stop this feature with a warning; select UI + HOTAS then UI + GAMEPAD to retry. HOTAS and physical gamepads are retained, and no mouse or
keyboard events are injected. The virtual device is system-visible while
enabled, so other controller-aware applications may also see it.

Native Frame uses its physical layout: left D-pad, right ABXY, both sticks,
analog triggers, stick clicks, physical shoulders, left View and right Menu.
Touch compatibility uses AB on the right and XY on the left. Grips remain
dedicated to NOVR UI hand switching and are never substituted for shoulders.
Controls absent from the selected compatibility profile remain neutral.

The selected UI hand's trigger, face/D-pad, and Menu/View inputs are suppressed
while hovering interactive UI, pressing, dragging, or using the maximized map.
They remain suppressed until released after leaving the UI. Point away from
interactive UI while teaching the game's remapper a controller button. Focus
loss, pause, or invalid/missing tracking of **either hand** publishes a neutral
report for the combined gamepad and requires release before those buttons
return. Both hands must be tracked to drive this device. Disable, quit,
destruction, and failures neutralize and remove the device. Start/Back remain
reserved to NOVR navigation while its controller pointer is active, preventing
duplicate menu toggles; they require release before returning to the gamepad
when that navigation stops. Flight axes and the remaining buttons retain their
normal gamepad behavior outside interactive UI.

The pinned official NuGet package `Nefarius.ViGEm.Client` **1.21.256** contains a
single `lib/netstandard2.0/Nefarius.ViGEm.Client.dll`. Its Costura resources embed
both native architectures and preload the appropriate extracted client using
an absolute `LoadLibraryEx` path. No process-global DLL search path is changed.
Package that managed dependency alongside `NOVR.Gamepad.dll`; Unity, BepInEx,
and NOVR references must come from the existing installation.

Build into staging only with `-p:NovrAutoDeploy=false`. Pure policy/lifecycle
tests: `dotnet run --project tests/Gamepad/Gamepad.csproj -p:NovrAutoDeploy=false`.
Build/test success does not prove Unity Mono native loading, Rewired discovery,
mapping persistence, controller input, or headset flight. Those remain physical
acceptance gates. This plugin does not rename the Xbox device to a custom name.

`UiGamepad` enables normal flight bindings. Selecting `UiHotas` neutralizes and removes the virtual gamepad on the next update, without changing the VR pointer or HOTAS mappings. The earlier companion `Enable Gamepad` flag is retired and ignored; this single mode setting controls output.
