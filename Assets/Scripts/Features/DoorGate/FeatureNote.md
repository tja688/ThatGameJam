# Feature: DoorGate

## 1. Purpose
- 管理 doorId 的开关条件与开门状态。
- 接收花激活事件计数，满足条件后开门。
- 对外广播门状态变化事件。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/DoorGate/`
- Controllers:
  - `DoorGateConfig2D.cs` 〞 绑定 doorId 与条件
- Models:
  - `IDoorGateModel`, `DoorGateModel`
  - `DoorGateState.cs`
- Commands:
  - `RegisterDoorConfigCommand`
  - `UnregisterDoorCommand`
  - `UpdateDoorProgressCommand`
  - `SetDoorStateCommand`
  - `ResetDoorsCommand`
- Systems:
  - `IDoorGateSystem`, `DoorGateSystem` 〞 监听 `FlowerActivatedEvent`
- Events:
  - `DoorStateChangedEvent`
  - `DoorOpenEvent`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.DoorGate.Models.IDoorGateModel`
  - System: `ThatGameJam.Features.DoorGate.Systems.IDoorGateSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `DoorGateConfig2D`（配置 doorId 与 requiredFlowerCount）
  - 门的表现层脚本建议使用 `Mechanisms/DoorMechanism2D`
- Inspector fields (if any):
  - `DoorGateConfig2D.doorId`
  - `DoorGateConfig2D.requiredFlowerCount`
  - `DoorGateConfig2D.allowCloseOnDeactivate`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct DoorStateChangedEvent`
  - Payload: `DoorId`, `IsOpen`
- `struct DoorOpenEvent`
  - Payload: `DoorId`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `RegisterDoorConfigCommand`
- `UpdateDoorProgressCommand`
- `SetDoorStateCommand` 〞 StoryTasks 可直接调用
- `ResetDoorsCommand` 〞 HardReset 归位

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None（如需读取请补 Query）。

## 5. Typical Integrations
- 示例：BellFlower 发送 `FlowerActivatedEvent`，DoorGate 计数满足后开门。

## 6. Verify Checklist
1. 配置 `DoorGateConfig2D`（required=2），门物体挂 `DoorMechanism2D`。
2. 两朵 `BellFlower2D` 使用同一 `doorId`，点亮后门开启。
3. 若 `allowCloseOnDeactivate=true`，熄灭后门可关闭。
4. 触发 `RunResetEvent`（HardReset），门状态回到初始值。

## 7. UNVERIFIED (only if needed)
- None.
