# SafeZone Feature Note

1) Capability
- Tracks whether the player is inside any SafeZone trigger and regenerates light while safe. Emits SafeZoneStateChangedEvent and bindable SafeZoneCount/IsSafe.

2) Scene setup (Unity)
- Add `SafeZone2D` to a GameObject with a `Collider2D` set to `isTrigger` (commonly a child of a lamp prefab).
- Add `PlayerSafeZoneSensor` to the player GameObject.
- Add `SafeZoneTickController` to a persistent GameObject (e.g., "GameSystems").

3) Configuration
- SafeZone regen is currently hard-coded at 6 light per second in `SafeZoneSystem`.

4) Minimal verification steps
- Enter Play Mode and move into the SafeZone2D trigger.
- Observe light value increase over time (via LightVitality logs or HUD).
- Exit the zone and confirm regeneration stops.

5) Common pitfalls
- Ensure SafeZone colliders are marked `isTrigger`.
- Avoid writing model state directly; always use Commands.
