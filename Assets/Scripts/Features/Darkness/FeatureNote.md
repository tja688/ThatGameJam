# Feature: Darkness

## 1. Purpose
- 追踪玩家是否处于黑暗区域，并广播状态变化。
- 在黑暗中持续扣光（安全区生效时不扣）。
- 在 HardReset/Respawn 时刷新重叠检测。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Darkness/`
- Controllers:
  - `PlayerDarknessSensor.cs`
  - `DarknessZone2D.cs`
  - `DarknessTickController.cs`
- Systems:
  - `IDarknessSystem`, `DarknessSystem`
- Models:
  - `IDarknessModel`, `DarknessModel`
- Commands:
  - `SetInDarknessCommand`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.Darkness.Models.IDarknessModel`
  - System: `ThatGameJam.Features.Darkness.Systems.IDarknessSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlayerDarknessSensor`（玩家根节点）
  - `DarknessZone2D`（黑暗触发体）
  - `DarknessTickController`（系统 Tick）

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct DarknessStateChangedEvent`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetInDarknessCommand`

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- `IReadonlyBindableProperty<bool> IsInDarkness`

## 5. Typical Integrations
- 示例：进入黑暗时播放氛围音效（监听 `DarknessStateChangedEvent`）。

## 6. Verify Checklist
1. 放置 `DarknessZone2D`，进入/离开后状态切换。
2. 黑暗中扣光，安全区内不扣光。
3. 出生点在黑暗区内时，Respawn/HardReset 后状态能立即刷新。

## 7. UNVERIFIED (only if needed)
- None.
