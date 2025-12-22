# Feature: KeroseneLamp

## 1. Purpose
- Track kerosene lamp count/max and broadcast spawn/count events.
- Optionally instantiate lamp prefabs on player death.
- If lamp count is already at max, the next death sends `MarkRunFailedCommand`. `RunFailHandling` handles the delayed reset.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/KeroseneLamp/`
- Controllers:
- `KeroseneLampManager.cs` 〞 spawns lamp prefab and issues commands
- Models:
  - `IKeroseneLampModel`, `KeroseneLampModel` 〞 lamp count + max
- Commands:
- `RecordLampSpawnedCommand`
- `SetLampMaxCommand`
- `ResetLampsCommand`
- Systems: None
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.KeroseneLamp.Models.IKeroseneLampModel`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `KeroseneLampManager` on a scene manager object
- Inspector fields (if any):
  - `lampPrefab` 〞 prefab to instantiate (optional)
  - `lampParent` 〞 parent transform for spawned lamps (optional)
  - `maxLamps` 〞 sets lamp max on enable (if `applyMaxOnEnable`)
  - `applyMaxOnEnable` 〞 whether to send `SetLampMaxCommand` on enable

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct LampSpawnedEvent`
  - When fired: `RecordLampSpawnedCommand` executes
  - Payload: `LampId` (int), `WorldPos` (Vector3)
  - Typical listener: VFX/audio or analytics
  - Example:
    ```csharp
    this.RegisterEvent<LampSpawnedEvent>(OnLampSpawned)
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct LampCountChangedEvent`
  - When fired: lamp count/max changes
  - Payload: `Count` (int), `Max` (int)
  - Typical listener: HUD

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `RecordLampSpawnedCommand`
  - What state it mutates: `IKeroseneLampModel.LampCount`, `NextLampId`
  - Typical sender: `KeroseneLampManager`
- `SetLampMaxCommand`
  - What state it mutates: `IKeroseneLampModel.LampMax`
  - Typical sender: `KeroseneLampManager` or setup code
- `ResetLampsCommand`
  - What state it mutates: lamp count and next id
  - Typical sender: `KeroseneLampManager` on run reset

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<int> LampCount`
  - `IReadonlyBindableProperty<int> LampMax`
  - Usage notes: HUD display or gating logic

## 5. Typical Integrations
- Example: Set lamp cap on scene load ↙ send `SetLampMaxCommand`.
  ```csharp
  this.SendCommand(new SetLampMaxCommand(3));
  ```

## 6. Verify Checklist
1. Add `KeroseneLampManager` and assign `lampPrefab` (optional) and `lampParent`.
2. Trigger a `PlayerDiedEvent`; expect `LampSpawnedEvent` and `LampCountChangedEvent`.
3. When lamp count reaches max, trigger another `PlayerDiedEvent`; expect `RunFailedEvent`, then a delayed `RunResetEvent` and lamp count reset to 0.

## 7. UNVERIFIED (only if needed)
- None.
