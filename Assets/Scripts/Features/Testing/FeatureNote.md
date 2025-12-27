# Feature: Testing

## 1. Purpose
- 提供编辑器/开发构建的测试入口。
- 通过测试输入触发 HardReset（`RunResetEvent`）。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Testing/`
- Controllers:
  - `RunResetController.cs` 〞 测试用 HardReset 输入
- Systems/Models/Commands/Queries: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules: None.

### 3.2 Scene setup (Unity)
- Optional MonoBehaviours:
  - `RunResetController`（仅编辑器/开发构建使用）
- Inspector fields (if any):
  - `RunResetController.resetKey` 〞 触发 HardReset 的按键

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- None.（事件由 `RunFailResetSystem` 发送）

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：开发场景按 `resetKey` 触发 `RunResetEvent`，用于 HardReset 测试。

## 6. Verify Checklist
1. 在编辑器或开发构建场景中挂载 `RunResetController`。
2. 按下 `resetKey`，确认触发 `RunResetEvent`。

## 7. UNVERIFIED (only if needed)
- None.
