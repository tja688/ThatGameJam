# Feature: RunFailReset

## 1. Purpose
- 提供 HardReset/TestReset 的统一入口（`RunResetEvent`）。
- 不再基于死亡次数触发失败或世界回滚。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/RunFailReset/`
- Systems:
  - `IRunFailResetSystem`, `RunFailResetSystem` 〞 发送 `RunResetEvent`
- Controllers: None（测试入口在 `Assets/Scripts/Features/Testing/Controllers/RunResetController.cs`）
- Models/Commands/Queries: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - System: `ThatGameJam.Features.RunFailReset.Systems.IRunFailResetSystem`

### 3.2 Scene setup (Unity)
- Optional MonoBehaviours:
  - `RunResetController`（仅编辑器/开发构建触发 HardReset）

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct RunResetEvent`
  - When fired: `IRunFailResetSystem.RequestResetFromTest()`
  - Payload: none
  - Typical listener: 机关/灯/临时物体 HardReset 归位

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：机关监听 `RunResetEvent` 进行 HardReset 归位。

## 6. Verify Checklist
1. 场景中加入 `Testing/RunResetController`，运行后按 `R`。
2. 确认触发 `RunResetEvent`，相关监听逻辑执行。
3. 连续死亡 100 次不会触发失败/重置事件。

## 7. UNVERIFIED (only if needed)
- None.
