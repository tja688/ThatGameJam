# Feature: SafeZone

## 1. Purpose
- Track how many Safe Zones the player is inside and whether the player is safe.
- Regenerate Light Vitality while safe.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/SafeZone/`
- Controllers:
  - `PlayerSafeZoneSensor.cs` 〞 counts overlapping safe zones and sends commands
  - `SafeZone2D.cs` 〞 trigger volume that reports to the sensor
  - `SafeZoneTickController.cs` 〞 ticks `ISafeZoneSystem` each frame
- Systems:
  - `ISafeZoneSystem`, `SafeZoneSystem` 〞 applies light regen while safe
- Models:
  - `ISafeZoneModel`, `SafeZoneModel` 〞 safe flag + count
- Commands:
  - `SetSafeZoneCountCommand`
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.SafeZone.Models.ISafeZoneModel`
  - System: `ThatGameJam.Features.SafeZone.Systems.ISafeZoneSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlayerSafeZoneSensor` on the player root (tracks overlapping safe zones)
  - `SafeZone2D` on each safe zone trigger volume (Collider2D marked as Trigger)
  - `SafeZoneTickController` on any active GameObject (ticks the system)
- Inspector fields (if any):
  - None.

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct SafeZoneStateChangedEvent`
  - When fired: `SetSafeZoneCountCommand` changes count/safe flag
  - Payload: `SafeZoneCount` (int), `IsSafe` (bool)
  - Typical listener: Darkness system, HUD
  - Example:
    ```csharp
    this.RegisterEvent<SafeZoneStateChangedEvent>(OnSafeZoneChanged)
        .UnRegisterWhenDisabled(gameObject);
    ```

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetSafeZoneCountCommand`
  - What state it mutates: `ISafeZoneModel.SafeZoneCount`, `IsSafe`
  - Typical sender: `PlayerSafeZoneSensor`

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<int> SafeZoneCount`
  - `IReadonlyBindableProperty<bool> IsSafe`
  - Usage notes: gate darkness drain or regen UI

## 5. Typical Integrations
- Example: Pause darkness drain while safe ↙ listen to `SafeZoneStateChangedEvent`.

## 6. Verify Checklist
1. Add `PlayerSafeZoneSensor` to the player and `SafeZone2D` to a trigger volume.
2. Enter/exit the trigger; expect `SafeZoneStateChangedEvent` and `IsSafe` to toggle.
3. With `SafeZoneTickController` active, light should regenerate while safe.

## 7. UNVERIFIED (only if needed)
- None.
