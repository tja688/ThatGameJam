# Feature: HUD

## 1. Purpose
- 显示光量、黑暗/安全区状态、灯数量等基础 HUD 信息。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/HUD/`
- Controllers:
  - `HUDController.cs` 〞 读取模型 Bindable 并更新 UI
- Systems/Models/Commands/Queries: None.

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules: None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `HUDController` 挂在 HUD 画布或 UI 管理对象上
- Inspector fields (if any):
  - `lightText`, `lightFill`
  - `darknessText`, `safeZoneText`
  - `lampText`

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
- Bindables:
  - `ILightVitalityModel.CurrentLight` / `MaxLight`
  - `IDarknessModel.IsInDarkness`
  - `ISafeZoneModel.IsSafe` / `SafeZoneCount`
  - `IKeroseneLampModel.LampCount`

## 5. Typical Integrations
- 示例：在 HUD 中展示当前灯数量和安全区状态。

## 6. Verify Checklist
1. 场景中挂载 `HUDController` 并绑定 UI 文本/图片。
2. 进入黑暗/安全区、生成灯，确认 HUD 文本与填充实时更新。

## 7. UNVERIFIED (only if needed)
- None.
