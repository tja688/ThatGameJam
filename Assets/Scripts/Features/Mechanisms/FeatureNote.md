# Feature: Mechanisms (Updated 2025-12)

## 1. Purpose

* 提供机关与环境交互的基础框架。
* **核心能力**：支持区域管理（Area System）和可选的关卡重置（Run Reset）回滚。
* **设计哲学**：状态持久化可控。策划可决定某个机关在玩家死亡后是“保持现状”还是“完全重置”。

## 2. Folder & Key Files

* Root: `Assets/Scripts/Features/Mechanisms/`
* Controllers:
* `MechanismControllerBase.cs` —— 抽象基类，提供 `areaId` 钩子和 `shouldResetOnRunReset` 开关。
* `VineMechanism2D.cs` —— 光照累积生长的藤蔓平台。
* `GhostMechanism2D.cs` —— 随机路径巡逻并持续抽离玩家光量的幽灵。
* `SpikeHazard2D.cs` —— 触碰即死的陷阱。
* `DoorMechanism2D.cs` —— 监听特定 ID 事件开关的门。



## 3. Runtime Wiring

### 3.1 核心配置 (Inspector)

* **Should Reset On Run Reset**:
* `True`: 玩家死亡或按 R 重开时，执行 `OnHardReset`（如藤蔓瞬间缩回、门恢复初始开关状态）。
* `False`: 机关状态跨越死亡重开而保留（实现“一次开启，终生受益”）。


* **Area Id**: 填入区域名称（如 `Forest_01`）。当玩家进出该区域时，触发 `OnAreaEnter/Exit` 钩子。

## 4. 现有脚本与旧版 Note 的详细出入对比

| 功能点 | 旧版 Note 描述 (已废弃) | 现有脚本真实实现 (以代码为准) |
| --- | --- | --- |
| **藤蔓回退逻辑** | 无光时会逐步回退。 | **不自动回退**。只要光照累积达标即激活，除非触发 `HardReset` 且开关开启。 |
| **重置机制** | 统一强制回滚。 | **可选回滚**。由基类 `shouldResetOnRunReset` 布尔值决定。 |
| **藤蔓激活条件** | 只要在光照范围内即生长。 | **需持续照射**。需满足 `lightRequiredSeconds` 计时。 |
| **幽灵机关** | 未提及此 Feature。 | **复杂移动模型**。使用 Catmull-Rom 样条线和 Perlin 噪声实现漂浮感，并持续扣除光量。 |
| **死亡判定依赖** | 仅提到碰撞死亡。 | **依赖 DeathController**。Spike 和 Ghost 都会检查碰撞体父级是否有 `DeathController`。 |

## 5. Typical Integrations

* **藤蔓 + 煤油灯**：玩家需持灯照射藤蔓一定时间使其长出，随后作为平台通过。
* **幽灵区域**：在 `GhostMechanism2D` 的 `boundsCollider` 范围内，玩家需快速通过以防光量被抽干。
* **门禁解谜**：通过发送 `DoorStateChangedEvent` 并匹配 `doorId` 来开启路径。

## 6. Verify Checklist

1. **重置开关测试**：勾选/取消勾选 `shouldResetOnRunReset`，观察玩家死亡后藤蔓是否保留。
2. **幽灵碰撞测试**：进入幽灵触发区，验证 `ConsumeLightCommand` 是否持续发送。
3. **藤蔓方向测试**：调整 `growthDirection`，确保视觉 `Sprite` 和 `Collider` 同时向正确方向延伸。
4. **区域钩子测试**：通过 Log 验证 `OnAreaEnter/Exit` 是否在跨越区域时正确触发。

---
