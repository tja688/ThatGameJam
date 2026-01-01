# Feature: LightVitality

## 1. Purpose
- 维护光量（当前/最大）并广播变化。
- 提供消耗/增加/读取光量的 Command/Query。
- 在 HardReset（`RunResetEvent`）与 `PlayerRespawnedEvent` 时回满。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/LightVitality/`
- Controllers:
  - `LightVitalityDebugController.cs` 〞 调试用光量按键
  - `LightVitalityResetController.cs` 〞 场景级重置监听（可选）
  - `FallLightDamageController.cs` 〞 挂在玩家身上，监听 `PlayerGroundedChangedEvent` 与爬墙状态，用坠落高度决定扣光并自动忽略抓墙后的落地
- Systems:
  - `ILightVitalityResetSystem`, `LightVitalityResetSystem` 〞 监听 HardReset/Respawn 回满
- Models:
  - `ILightVitalityModel`, `LightVitalityModel`
- Commands:
  - `AddLightCommand`
  - `ConsumeLightCommand`
  - `SetLightCommand`
  - `SetMaxLightCommand`
- Queries:
  - `GetCurrentLightQuery`
  - `GetMaxLightQuery`
  - `GetLightPercentQuery`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.LightVitality.Models.ILightVitalityModel`
  - System: `ThatGameJam.Features.LightVitality.Systems.ILightVitalityResetSystem`

### 3.2 Scene setup (Unity)
- Optional MonoBehaviours:
  - `LightVitalityDebugController`（调试）
  - `LightVitalityResetController`（旧场景兼容）
  - `FallLightDamageController`（挂在带 `PlatformerCharacterController` 的玩家上，调整安全坠落高度 / 单次最大扣光）

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct LightChangedEvent`
- `struct LightConsumedEvent`
- `struct LightDepletedEvent`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `AddLightCommand`
- `ConsumeLightCommand`
- `SetLightCommand`
- `SetMaxLightCommand`

### 4.4 Queries (Read Path, optional)
- `GetCurrentLightQuery`
- `GetMaxLightQuery`
- `GetLightPercentQuery`

### 4.5 Light consumption reasons
- `ELightConsumeReason.Fall` 〞 来自坠落后命中地面的光量损失

### 4.6 Model Read Surface
- `IReadonlyBindableProperty<float> CurrentLight`
- `IReadonlyBindableProperty<float> MaxLight`

## 5. Typical Integrations
- 示例：危险体积调用 `ConsumeLightCommand` 扣光。
- 给玩家添加 `FallLightDamageController`，配置 `minHeightForDamage` / `damagePerMeter` / `maxDamagePerFall`，利用 `ConsumeLightCommand` 通过 `ELightConsumeReason.Fall` 标记坠落扣光；爬墙过程中会跳过当前坠落。

## 6. Verify Checklist
1. 使用 `LightVitalityDebugController` 测试加/减光。
2. 光量归零后触发 `LightDepletedEvent`。
3. 触发 `RunResetEvent` 或 `PlayerRespawnedEvent`，光量回满。

## 7. UNVERIFIED (only if needed)
- None.
