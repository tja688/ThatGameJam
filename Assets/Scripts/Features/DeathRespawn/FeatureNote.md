# Feature: DeathRespawn

## 1. Purpose
- Track player alive/dead state and broadcast death/respawn events.
- Provide death triggers (fall/light-depleted) and respawn logic.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/DeathRespawn/`
- Controllers:
  - `DeathController.cs` 〞 detects death conditions and calls system
  - `RespawnController.cs` 〞 handles respawn timing/position
  - `KillVolume2D.cs` 〞 trigger volume that kills on contact
- Systems:
  - `IDeathRespawnSystem`, `DeathRespawnSystem` 〞 sends death/respawn commands
- Models:
  - `IDeathRespawnModel`, `DeathRespawnModel` 〞 alive state + death count
- Commands:
  - `MarkPlayerDeadCommand`
  - `MarkPlayerRespawnedCommand`
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.DeathRespawn.Models.IDeathRespawnModel`
  - System: `ThatGameJam.Features.DeathRespawn.Systems.IDeathRespawnSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `DeathController` on player root (monitors fall/light-depleted)
  - `RespawnController` on player root (teleports and signals respawn)
  - `KillVolume2D` on trigger volumes that should kill the player
- Inspector fields (if any):
  - `DeathController.listenToLightDepleted` 〞 listen for `LightDepletedEvent`
  - `DeathController.useFallCheck` / `fallYThreshold` 〞 auto-kill below Y
  - `RespawnController.respawnPoint` / `respawnDelay` 〞 respawn target and delay
  - `RespawnController.respawnOnDeath` / `respawnOnRunReset` 〞 event-driven respawn
  - `RespawnController.resetVelocity` 〞 zero Rigidbody2D velocity on respawn

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct PlayerDiedEvent`
  - When fired: `MarkPlayerDeadCommand` executes
  - Payload: `Reason` (EDeathReason), `WorldPos` (Vector3)
  - Typical listener: Kerosene lamp spawner, audio/FX
  - Example:
    ```csharp
    this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct PlayerRespawnedEvent`
  - When fired: `MarkPlayerRespawnedCommand` executes
  - Payload: `WorldPos` (Vector3)
  - Typical listener: checkpoint/UI, LightVitality reset system

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `MarkPlayerDeadCommand`
  - What state it mutates: `IDeathRespawnModel.IsAlive`, `DeathCount`
  - Typical sender: `DeathRespawnSystem`
- `MarkPlayerRespawnedCommand`
  - What state it mutates: `IDeathRespawnModel.IsAlive`
  - Typical sender: `DeathRespawnSystem`

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<bool> IsAlive`
  - `IReadonlyBindableProperty<int> DeathCount`
  - Usage notes: HUD or analytics counters

## 5. Typical Integrations
- Example: On fall trigger ↙ call `IDeathRespawnSystem.MarkDead(...)` from a hazard controller.

## 6. Verify Checklist
1. Add `DeathController` + `RespawnController` to the player and a `KillVolume2D` trigger in the scene.
2. Enter the kill volume; expect `PlayerDiedEvent` to fire with `Reason=Fall`.
3. After `respawnDelay`, player teleports and `PlayerRespawnedEvent` fires.

## 7. UNVERIFIED (only if needed)
- None.
