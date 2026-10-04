# Mission pause VR UI settings

Pause a mission and select **VR UI SETTINGS**. This opens the same settings panel used by the frontend, without changing the native pause state or displaying the frontend environment. BACK/controller cancel returns to the stock pause menu; Resume/Escape, menu replacement, leaving the mission and scene destruction close the overlay and restore the stock UI. The entry is independent of Show Recenter In Pause Menu and Native Menu Enabled.

Controller/HUD/sight choices and panel scale/distance/height apply live and persist through the existing settings panel. Enabling the OpenXR eye feature still requires restart; the existing panel explains this. RECENTER uses a two-second unscaled delay while paused and cancels if the settings overlay closes. No new flight bindings or OS mouse events are added.

Hardware check: pause in a mission, open VR UI SETTINGS using the controller, change Status/Sight/Controller Mode, press BACK and verify the pause menu remains open, then Resume. Repeat opening; verify one settings button, existing recenter button, mouse/keyboard fallback, no hidden blocker after Resume, and persistence on reopen. Repeat with native frontend menus disabled and recenter-button option disabled.
