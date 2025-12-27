# Feature: StoryTasks

## 1. Purpose
- 使用“触发器 + 动作序列 + 一次性标记”承载剧情/组合逻辑。
- 对话播放通过事件请求，由外部 Dialogue 插件消费。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/StoryTasks/`
- Controllers:
  - `StoryTaskTrigger2D.cs` 〞 触发并执行动作序列
- Models:
  - `IStoryFlagsModel`, `StoryFlagsModel` 〞 一次性标记
- Commands:
  - `SetFlagCommand`
- Queries:
  - `HasFlagQuery`
- Events:
  - `DialogueRequestedEvent`
  - `StoryFlagChangedEvent`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.StoryTasks.Models.IStoryFlagsModel`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `StoryTaskTrigger2D`（触发器）
- Inspector fields (if any):
  - `triggerFlagId` / `triggerOnce`
  - Action 列表：PlayDialogue / SpawnLamp / SetGateState / SetFlag / RequireFlag

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `DialogueRequestedEvent`
  - Payload: `DialogueId`, `Priority`
- `StoryFlagChangedEvent`
  - Payload: `FlagId`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetFlagCommand`
- `SetDoorStateCommand`（DoorGate）
- `RequestSpawnLampEvent`（KeroseneLamp）

### 4.4 Queries (Read Path, optional)
- `HasFlagQuery`

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：触发器播放对话 + 生成灯 + 开门 + 标记一次性完成。

## 6. Verify Checklist
1. 场景中放置 `StoryTaskTrigger2D`，配置 PlayDialogue 与 SetFlag 动作。
2. 触发后发送 `DialogueRequestedEvent`，再次触发若 `DialogueOnce=true` 不再重复。
3. 配置 SpawnLamp 动作，触发后灯生成。
4. 配置 SetGateState 动作，门状态按预期变化。

## 7. UNVERIFIED (only if needed)
- UNVERIFIED: 对话系统的实际播放接口需由外部插件实现。
- NEXT_SEARCH: 检查项目内 Dialogue 插件的触发事件/接口（Assets/Plugins/ 或 Packages 目录）。
