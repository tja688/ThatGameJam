# Feature: Mechanisms

## 1. Purpose
- 提供机关/环境交互的基础脚本与可扩展基类。
- 统一 HardReset 回滚入口，并提供区域进出钩子。
- 内置示例：Spike、Vine、Door 机关。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Mechanisms/`
- Controllers:
  - `MechanismControllerBase.cs` 〞 机关基类（HardReset/AreaEnter/AreaExit）
  - `SpikeHazard2D.cs` 〞 碰撞即死亡
  - `VineMechanism2D.cs` 〞 持续光照生长/无光回退
  - `DoorMechanism2D.cs` 〞 门的表现层（监听 DoorGate 事件）
- Systems/Models/Commands/Queries: None.

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours (按需):
  - `SpikeHazard2D`（触发碰撞）
  - `VineMechanism2D`（藤蔓平台）
  - `DoorMechanism2D`（门模型/碰撞体）
- Inspector fields (if any):
  - `MechanismControllerBase.areaId` 〞 区域进入/离开钩子目标区域
  - `VineMechanism2D.lightAffectRadius` / `growthSpeed` / `decaySpeed`
  - `DoorMechanism2D.doorId` / `startOpen`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- None.（机关主要监听事件）

### 4.2 Request Events (Inbound write requests, optional)
- 通过 `DoorGate` 监听 `DoorStateChangedEvent`。
- 通过 `RunResetEvent`（HardReset）回滚状态。
- 可选监听 `AreaChangedEvent` 进行区域性能策略。

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：藤蔓读取 KeroseneLamp GameplayEnabled 灯列表，持续光照生长。

## 6. Verify Checklist
1. 放置 `SpikeHazard2D`，触碰后应触发死亡。
2. 放置 `VineMechanism2D`，在光源范围内逐步生长，无光逐步回退。
3. 触发 `RunResetEvent`（HardReset），藤蔓回到初始状态。
4. 放置 `DoorMechanism2D` 并配置 `doorId`，当 DoorGate 打开事件触发时门碰撞/视觉关闭。

## 7. UNVERIFIED (only if needed)
- None.
