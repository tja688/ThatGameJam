# Feature: KeroseneLamp

## 1. Purpose
- Maintain a registry of lamp instances (id → area/position/state).
- Provide a 4-state lamp state machine with UnityEvents on state entry.
- Toggle lamp light visuals by player area via a dedicated controller.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/KeroseneLamp/`
- Controllers:
  - `KeroseneLampManager.cs` 〞 spawn/restore registry + save integration
  - `KeroseneLampInstance.cs` 〞 4-state machine + physics/collider handling
  - `LampRegionLightController.cs` 〞 area-based light visibility (independent of state)
  - `KeroseneLampPreplaced.cs` 〞 preplaced lamp marker
- Models:
  - `IKeroseneLampModel`, `KeroseneLampModel`
  - `LampInfo`, `KeroseneLampSaveState`, `KeroseneLampState`
- Commands:
  - `RecordLampSpawnedCommand`
  - `SetLampGameplayStateCommand`
  - `SetLampInventoryFlagsCommand`
  - `ConvertHeldLampToDroppedCommand`
  - `ResetLampsCommand`
- Events:
  - `LampSpawnedEvent`, `LampCountChangedEvent`
  - `LampGameplayStateChangedEvent`
  - `RequestSpawnLampEvent`
- Queries:
  - `GetGameplayEnabledLampsQuery`
  - `GetAllLampInfosQuery`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.KeroseneLamp.Models.IKeroseneLampModel`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `KeroseneLampManager` 〞 registry + spawn/restore
  - Lamp prefab must include:
    - `KeroseneLampInstance`
    - `LampRegionLightController`
- Preplaced lamps:
  - Add `KeroseneLampPreplaced` + `KeroseneLampInstance` + `LampRegionLightController`
- Inspector fields:
  - `KeroseneLampManager.lampPrefab`
  - `KeroseneLampManager.playerHoldPoint` (for held state)
  - `KeroseneLampInstance` UnityEvents for each state

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct LampSpawnedEvent`
  - When fired: `RecordLampSpawnedCommand`
  - Payload: `LampId`, `WorldPos`
- `struct LampGameplayStateChangedEvent`
  - When fired: gameplay enabled toggles

### 4.2 Request Events (Inbound write requests, optional)
- `struct RequestSpawnLampEvent`
  - Who sends it: story tasks, triggers
  - Who handles it: `KeroseneLampManager`

### 4.3 Commands (Write Path)
- `RecordLampSpawnedCommand`
  - Mutates: registry entry + area counts
- `SetLampGameplayStateCommand`
  - Mutates: gameplay enabled flag
- `SetLampInventoryFlagsCommand`
  - Mutates: inventory flags + lamp count
- `ConvertHeldLampToDroppedCommand`
  - Mutates: area/position + count when dropped

### 4.4 Queries (Read Path, optional)
- `GetGameplayEnabledLampsQuery`
  - Returns: gameplay-enabled lamps for light checks

### 4.5 Model Read Surface
- `IReadonlyBindableProperty<int> LampCount`

## 5. Typical Integrations
- Bugs/vines query `GetGameplayEnabledLampsQuery` to detect light sources.
- UI listens to `LampCount` for HUD updates.

## 6. Verify Checklist
1. Pickup lamp → state enters `InBackpack` and UnityEvent fires.
2. Hold lamp → state enters `Held`, lamp attaches to hold point.
3. Drop lamp → state enters `Dropped`, physics re-enabled.
4. Disable lamp (area limit) → state enters `Disabled`.
5. Area change toggles lamp light via `LampRegionLightController` only.

## 7. UNVERIFIED (only if needed)
- None.
