# Feature: Darkness

## 1. Purpose
- Track whether the player is inside darkness zones and publish state changes.
- Drain Light Vitality while in darkness (unless a Safe Zone is active).

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Darkness/`
- Controllers:
  - `PlayerDarknessSensor.cs` 〞 detects zone enter/exit and sends `SetInDarknessCommand`
  - `DarknessZone2D.cs` 〞 trigger volume that reports to the sensor
  - `DarknessTickController.cs` 〞 ticks `IDarknessSystem` each frame
- Systems:
  - `IDarknessSystem`, `DarknessSystem` 〞 drains light on tick
- Models:
  - `IDarknessModel`, `DarknessModel` 〞 `IsInDarkness` bindable state
- Commands:
  - `SetInDarknessCommand` 〞 updates model + broadcasts event
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.Darkness.Models.IDarknessModel`
  - System: `ThatGameJam.Features.Darkness.Systems.IDarknessSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlayerDarknessSensor` on the player root (tracks overlapping darkness zones)
  - `DarknessZone2D` on each darkness trigger volume (Collider2D marked as Trigger)
  - `DarknessTickController` on any active GameObject (ticks the system)
- Inspector fields (if any):
  - `PlayerDarknessSensor.enterDelay` / `exitDelay` 〞 debounce for zone enter/exit

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct DarknessStateChangedEvent`
  - When fired: `SetInDarknessCommand` changes the darkness state
  - Payload: `IsInDarkness` (bool)
  - Typical listener: UI/HUD, audio/FX
  - Example:
    ```csharp
    this.RegisterEvent<DarknessStateChangedEvent>(e => { /* react */ })
        .UnRegisterWhenDisabled(gameObject);
    ```

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetInDarknessCommand`
  - What state it mutates: `IDarknessModel.IsInDarkness`
  - Typical sender: `PlayerDarknessSensor`

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<bool> IsInDarkness`
  - Usage notes: use for UI or gameplay gating

## 5. Typical Integrations
- Example: On darkness enter ↙ play a sound (listen to `DarknessStateChangedEvent`)

## 6. Verify Checklist
1. Add `PlayerDarknessSensor` to the player and `DarknessZone2D` to a trigger volume.
2. Enter/exit the trigger; expect `DarknessStateChangedEvent` to fire and `IsInDarkness` to toggle.
3. With `DarknessTickController` active and Light Vitality enabled, light should drain while in darkness.

## 7. UNVERIFIED (only if needed)
- None.
