# Optional Frame gamepad

The separate `NOVR.Gamepad` BepInEx companion combines two tracked Frame (or Touch-compatible) controllers into one standard Xbox 360 gamepad. Nuclear Option's existing joystick controls/remapping UI handles flight bindings. No game input backend, HOTAS configuration, Steam binding, or Windows cursor is changed.

The companion requires a compatible **already installed** ViGEmBus driver. It installs/updates no drivers. ViGEm is archived; this is an optional compatibility path, not a required NOVR dependency. The managed client is pinned to 1.21.256. Driver/client failure logs a warning and removes the virtual device; toggle off/on to retry.

Set `[Gamepad] Enable Gamepad = true` in `BepInEx/config/deltawing.novr.gamepad.cfg` to opt in. Default is false. Both hands must be valid/tracked for flight output; otherwise the whole gamepad becomes neutral. Focus loss, disabling, errors, and exit neutralize/remove its output. Other real gamepads remain untouched.

| Frame input | Xbox input |
| --- | --- |
| Left/right sticks | Left/right sticks |
| Left/right triggers | LT/RT |
| Right A/B/X/Y | A/B/X/Y |
| Left directional face buttons | D-pad |
| Left/right shoulders | LB/RB |
| Stick clicks | L3/R3 |
| View/Menu | Back/Start when NOVR navigation is inactive |

Grips retain pointer-hand switching. While NOVR controller navigation is active, View/Menu remain NOVR navigation to prevent double pause toggles. Touch compatibility supplies its available face controls; it cannot supply native Frame shoulders/all four right face buttons.

When the pointer owns interactive UI, that hand's trigger/face/D-pad controls are consumed by the UI and remain gated until released. Point at empty space before capturing a gamepad binding in Nuclear Option. Flight axes/shoulders remain normal gamepad output when tracked and focused; enabling gamepad mode therefore allows the game's normal flight bindings as requested.

Read-only `get_gamepad_state` captures actual Rewired joystick identity/assignment/axes/buttons and all four native XInput slots. This distinguishes a loaded bridge from a gamepad actually detected by the game. No successful hardware acceptance is implied by a compiled plugin.

## Hardware acceptance

1. In Nuclear Option Controls, verify a new Xbox 360 controller appears alongside HOTAS.
2. Point away from UI; move each stick and press each trigger/face/shoulder/click control. Verify binding capture and intended normal flight actions.
3. Select UI with trigger; verify no simultaneous weapon activation, and require trigger release before returning to flight.
4. Verify Menu toggles pause once; grip changes pointer hand; HOTAS, keyboard and mouse still work.
5. Lose tracking/disconnect one controller and switch focus: virtual flight output must be neutral. Reconnect and release held buttons before resuming.
6. Quit the game: the virtual controller must disappear; real controllers remain.
