# Feature: RunFailHandling

## 1. Purpose
- 已废弃的失败处理占位，不再监听 `RunFailedEvent`。
- 仅保留手动调用 HardReset 的测试入口（不推荐放在正式场景）。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/RunFailHandling/`
- Controllers:
  - `RunFailHandlingController.cs` 〞 提供 `RequestHardResetFromTest()` 手动触发接口
- Systems/Models/Commands/Queries: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules: None.

### 3.2 Scene setup (Unity)
- **不建议**在场景中保留该组件；请从关卡中移除。

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- None.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- None（仅测试手动调用）。

## 6. Verify Checklist
1. 确认场景中已移除 `RunFailHandlingController` 组件。
2. 如需调试，编写临时脚本调用 `RequestHardResetFromTest()`，应触发 `RunResetEvent`。

## 7. UNVERIFIED (only if needed)
- None.
