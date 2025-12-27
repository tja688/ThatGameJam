# Feature: SafeZone

## 1. Purpose
- 追踪玩家当前处于多少个安全区以及是否安全。
- 在安全区内恢复光量。
- 在 HardReset/Respawn 时刷新重叠检测，确保出生点内立即生效。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/SafeZone/`
- Controllers:
  - `PlayerSafeZoneSensor.cs`
  - `SafeZone2D.cs`
  - `SafeZoneTickController.cs`
- Systems:
  - `ISafeZoneSystem`, `SafeZoneSystem`
- Models:
  - `ISafeZoneModel`, `SafeZoneModel`
- Commands:
  - `SetSafeZoneCountCommand`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.SafeZone.Models.ISafeZoneModel`
  - System: `ThatGameJam.Features.SafeZone.Systems.ISafeZoneSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlayerSafeZoneSensor`（玩家根节点）
  - `SafeZone2D`（安全区触发体）
  - `SafeZoneTickController`（系统 Tick）

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct SafeZoneStateChangedEvent`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetSafeZoneCountCommand`

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- `IReadonlyBindableProperty<int> SafeZoneCount`
- `IReadonlyBindableProperty<bool> IsSafe`

## 5. Typical Integrations
- 示例：黑暗系统在安全区时暂停扣光。

## 6. Verify Checklist
1. 放置 `SafeZone2D`，进入/离开触发体，`SafeZoneStateChangedEvent` 正常切换。
2. `SafeZoneTickController` 激活时，安全区内光量回升。
3. 出生点在安全区内时，Respawn/HardReset 后状态能立即刷新。

## 7. UNVERIFIED (only if needed)
- None.
