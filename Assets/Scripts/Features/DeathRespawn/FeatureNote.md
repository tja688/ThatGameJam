# Feature: DeathRespawn

## 1. Purpose
- 追踪玩家生死状态并广播死亡/复活事件。
- 维护死亡次数（仅统计/提示，不再用于失败判定）。
- 复活点通过 Checkpoint 查询获取（Mailbox/LevelNode）。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/DeathRespawn/`
- Controllers:
  - `DeathController.cs` 〞 检测死亡条件并通知系统
  - `RespawnController.cs` 〞 处理复活时机与位置
  - `KillVolume2D.cs` 〞 触发即死亡体积
- Systems:
  - `IDeathRespawnSystem`, `DeathRespawnSystem`
- Models:
  - `IDeathRespawnModel`, `DeathRespawnModel`
- Commands:
  - `MarkPlayerDeadCommand`
  - `MarkPlayerRespawnedCommand`
  - `ResetDeathCountCommand`
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.DeathRespawn.Models.IDeathRespawnModel`
  - System: `ThatGameJam.Features.DeathRespawn.Systems.IDeathRespawnSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `DeathController`（玩家根节点）
  - `RespawnController`（玩家根节点）
  - `KillVolume2D`（可致死的触发器）
- Inspector fields (if any):
  - `RespawnController.respawnDelay`
  - `RespawnController.respawnOnDeath` / `respawnOnRunReset`
  - `RespawnController.respawnPoint` 〞 仅在无 Checkpoint 时作为兜底

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct PlayerDiedEvent`
  - When fired: `MarkPlayerDeadCommand`
  - Payload: `Reason`, `WorldPos`
- `struct PlayerRespawnedEvent`
  - When fired: `MarkPlayerRespawnedCommand`
  - Payload: `WorldPos`
- `struct DeathCountChangedEvent`
  - When fired: death count changes
  - Payload: `Count`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `MarkPlayerDeadCommand`
- `MarkPlayerRespawnedCommand`
- `ResetDeathCountCommand` 〞 HardReset 时归零

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- Bindables:
  - `IReadonlyBindableProperty<bool> IsAlive`
  - `IReadonlyBindableProperty<int> DeathCount`

## 5. Typical Integrations
- 示例：死亡时灯生成（监听 `PlayerDiedEvent`）。

## 6. Verify Checklist
1. 添加 `DeathController` + `RespawnController`，设置 `respawnDelay`。
2. 触发 `KillVolume2D`，确认 `PlayerDiedEvent` 触发。
3. 触发复活后，角色位置来自 Checkpoint（Mailbox 最新 nodeId），速度清零、攀爬状态重置。
4. 触发 `RunResetEvent`（HardReset），死亡次数归零且可选复活。

## 7. UNVERIFIED (only if needed)
- None.
