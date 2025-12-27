# Feature: Checkpoint

## 1. Purpose
- 维护当前复活点的唯一事实来源（nodeId）。
- LevelNode 注册自身的 `nodeId/areaId/spawnPoint`。
- Mailbox 只负责写入 `nodeId`。
- 对外提供查询当前复活点位置与 nodeId。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Checkpoint/`
- Controllers:
  - `LevelNode2D.cs` 〞 注册节点数据
  - `Mailbox2D.cs` 〞 触发写入 nodeId
- Models:
  - `ICheckpointModel`, `CheckpointModel`
  - `CheckpointNodeInfo.cs`
- Commands:
  - `RegisterCheckpointNodeCommand`
  - `UnregisterCheckpointNodeCommand`
  - `SetCurrentCheckpointCommand`
- Queries:
  - `GetCurrentCheckpointQuery`
  - `GetCurrentCheckpointNodeIdQuery`
- Events:
  - `CheckpointChangedEvent`
- Systems:
  - `ICheckpointSystem`, `CheckpointSystem`（预留）

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.Checkpoint.Models.ICheckpointModel`
  - System: `ThatGameJam.Features.Checkpoint.Systems.ICheckpointSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `LevelNode2D` 放在复活点位置或节点对象上
  - `Mailbox2D` 放在触发器上，关联对应 `LevelNode2D`
- Inspector fields (if any):
  - `LevelNode2D.nodeId` / `areaId` / `spawnPoint`
  - `Mailbox2D.node` 或 `Mailbox2D.nodeId`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct CheckpointChangedEvent`
  - When fired: `SetCurrentCheckpointCommand`
  - Payload: `NodeId`, `AreaId`, `SpawnPoint`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `RegisterCheckpointNodeCommand`
- `UnregisterCheckpointNodeCommand`
- `SetCurrentCheckpointCommand`

### 4.4 Queries (Read Path, optional)
- `GetCurrentCheckpointQuery` 〞 返回当前复活点快照
- `GetCurrentCheckpointNodeIdQuery` 〞 返回当前 nodeId

### 4.5 Model Read Surface
- `IReadonlyBindableProperty<string> CurrentNodeId`

## 5. Typical Integrations
- 示例：`DeathRespawn` 在复活时调用 `GetCurrentCheckpointQuery` 获取位置。

## 6. Verify Checklist
1. 场景中放置多个 `LevelNode2D` 和对应 `Mailbox2D`。
2. 触发不同 Mailbox，确认 `CheckpointChangedEvent` 与 `CurrentNodeId` 更新。
3. 死亡后复活到最新 Mailbox 的 spawnPoint。

## 7. UNVERIFIED (only if needed)
- None.
