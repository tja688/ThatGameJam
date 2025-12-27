# Feature: IceBlock

## 1. Purpose
- 触发融化时立即消耗 1/4 光量。
- 5 秒后自动恢复冻结。
- HardReset 强制回到冻结状态。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/IceBlock/`
- Controllers:
  - `IceBlock2D.cs`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `IceBlock2D`（触发器物体）
- Inspector fields (if any):
  - `lightCostRatio`（默认 0.25）
  - `recoverSeconds`（默认 5）
  - `frozenVisual` / `meltedVisual`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- None.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `ConsumeLightCommand`（融化时触发）

### 4.4 Queries (Read Path, optional)
- `GetMaxLightQuery`（计算 1/4 光）

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：冰块消耗光量后融化，恢复后再次可触发。

## 6. Verify Checklist
1. 玩家进入 `IceBlock2D` 触发器后立即扣光并融化。
2. 等待 5 秒，冰块自动恢复冻结。
3. 触发 `RunResetEvent`（HardReset）后立即恢复冻结。

## 7. UNVERIFIED (only if needed)
- None.
