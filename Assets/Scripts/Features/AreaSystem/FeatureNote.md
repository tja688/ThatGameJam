# Feature: AreaSystem

## 1. Purpose
- 维护玩家当前所在 `areaId`。
- 区域切换时触发事件。
- 驱动跨区域视觉策略（如灯的 VisualEnabled 关闭）。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/AreaSystem/`
- Controllers:
  - `AreaVolume2D.cs` 〞 区域触发体
  - `PlayerAreaSensor.cs` 〞 玩家区域检测与选区逻辑
- Models:
  - `IAreaModel`, `AreaModel`
- Commands:
  - `SetCurrentAreaCommand`
- Queries:
  - `GetCurrentAreaIdQuery`
- Events:
  - `AreaChangedEvent`
- Systems:
  - `IAreaSystem`, `AreaSystem`（预留）

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.AreaSystem.Models.IAreaModel`
  - System: `ThatGameJam.Features.AreaSystem.Systems.IAreaSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlayerAreaSensor`（挂在玩家根节点）
  - `AreaVolume2D`（每个区域触发体）
- Inspector fields (if any):
  - `AreaVolume2D.areaId` / `priority`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct AreaChangedEvent`
  - When fired: `SetCurrentAreaCommand`
  - Payload: `PreviousAreaId`, `CurrentAreaId`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetCurrentAreaCommand`

### 4.4 Queries (Read Path, optional)
- `GetCurrentAreaIdQuery`

### 4.5 Model Read Surface
- `IReadonlyBindableProperty<string> CurrentAreaId`

## 5. Typical Integrations
- 示例：`KeroseneLampManager` 监听 `AreaChangedEvent`，批量切换灯的 VisualEnabled。

## 6. Verify Checklist
1. 给玩家挂 `PlayerAreaSensor`，并放置多个 `AreaVolume2D`。
2. 进入/离开不同区域，`AreaChangedEvent` 正常触发。
3. 切换区域后，非当前区域灯光视觉关闭（玩法仍有效）。

## 7. UNVERIFIED (only if needed)
- None.
