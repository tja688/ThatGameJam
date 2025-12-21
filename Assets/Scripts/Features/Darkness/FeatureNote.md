# Darkness Feature Note

1) Capability
- Determines when the player is inside a Darkness trigger zone and drains light over time via CQRS commands. Emits a DarknessStateChangedEvent and a bindable IsInDarkness state.

2) Scene setup (Unity)
- Create a GameObject for each dark area and add `DarknessZone2D`.
- Ensure the GameObject has a `Collider2D` set to `isTrigger`.
- On the player, add `PlayerDarknessSensor`.
- Add `DarknessTickController` to a persistent GameObject (e.g., "GameSystems").

3) Configuration
- `PlayerDarknessSensor`:
  - `enterDelay`: delay before entering darkness.
  - `exitDelay`: delay before exiting darkness.
- `DarknessSystem` drain is currently hard-coded to 5 light per second.

4) Minimal verification steps
- Enter Play Mode and move the player into a DarknessZone2D.
- Observe light draining over time (via LightVitality logs or HUD).
- Move out of the zone and verify draining stops after exitDelay.

5) Common pitfalls
- The zone collider must be set to `isTrigger`.
- Ensure only Commands write the darkness state (no direct Bindable writes).
