# Feature: Hazard

## 1. Purpose
- 统一危险/伤害规则入口（InstantKill / 扣光）。
- 提供通用触发体：`DamageVolume2D`（带 cooldown）与 `HazardVolume2D`（InstantKill/DrainFast）。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Hazard/`
- Controllers:
  - `DamageVolume2D.cs` 〞 扣光（有 cooldown）
  - `HazardVolume2D.cs` 〞 InstantKill / DrainFast
- Systems:
  - `IHazardSystem`, `HazardSystem`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - System: `ThatGameJam.Features.Hazard.Systems.IHazardSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours (按需):
  - `DamageVolume2D` / `HazardVolume2D`
- Inspector fields (if any):
  - `DamageVolume2D.costRatio` / `cooldownSeconds`
  - `HazardVolume2D.mode` / `drainRatioPerSecond` / `deathReason`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- None.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `ConsumeLightCommand`（由 `HazardSystem` 统一发送）
- `MarkPlayerDeadCommand`（通过 `DeathRespawnSystem`）

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：`FallingRockProjectile` 通过 `IHazardSystem.ApplyInstantKill` 造成统一伤害。

## 6. Verify Checklist
1. 放置 `DamageVolume2D`，玩家进入后按 cooldown 扣光。
2. 放置 `HazardVolume2D`：InstantKill 模式触发死亡，DrainFast 模式持续扣光。

## 7. UNVERIFIED (only if needed)
- None.
