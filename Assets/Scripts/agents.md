# agents.md — “煤油灯”核心机制落地任务清单（Features 02–08）

> 目标：让 AI 代理在 **严格遵从** `Assets/Scripts/QFramework_Playbook`（PLAYBOOK/RECIPES/TEMPLATES）约束的前提下，按垂直切片方式逐步实现以下 7 个核心 Features，并做到可在 Unity 场景中快速配置与验证。
>
> 重要：本文件 **不复述** 架构书内容；AI 必须自行阅读并遵从（含：分层、CQRS 写路径、生命周期订阅、日志、禁止区等）。

---

## 0. 必读与输出格式（AI MUST）

1. 阅读顺序（必须）

   1) `Assets/Scripts/QFramework_Playbook/PLAYBOOK.md`
   2) `Assets/Scripts/QFramework_Playbook/RECIPES.md`
   3) `Assets/Scripts/QFramework_Playbook/TEMPLATES.md`
2. 实现任何代码前：必须输出 `PLAN`（使用 RECIPES 的 Preflight Plan 格式）。
3. 实现完成后：必须输出 `CHANGES / VERIFY / NOTES`（按 PLAYBOOK 的要求）。
4. 禁止：修改 `Assets/QFramework/**` 与任何第三方包。若必须修改，输出 `MANUAL_ACTIONS` 清单。

---

## 1. 本轮要做的 Features（范围）

- **Feature 02 — 光=生命（Light Vitality）**
- **Feature 03 — 黑暗判定与黑暗伤害（Darkness）**
- **Feature 04 — 死亡与重生（Death & Respawn）**
- **Feature 05 — 煤油灯系统（Kerosene Lamps）**
- **Feature 06 — 失败条件与关卡重开（Run Fail & Reset）**
- **Feature 07 — 核心HUD（HUD）**
- **Feature 08 — 灯安全区规则（Safe Zone & Regen）**

---

## 2. 目录与命名（MUST）

为避免互相污染、便于拆分验证，每个 Feature 使用独立根目录（垂直切片）：

- `Assets/Scripts/Features/LightVitality/`
- `Assets/Scripts/Features/Darkness/`
- `Assets/Scripts/Features/DeathRespawn/`
- `Assets/Scripts/Features/KeroseneLamp/`
- `Assets/Scripts/Features/RunFailReset/`
- `Assets/Scripts/Features/HUD/`
- `Assets/Scripts/Features/SafeZone/`

共享类型（尽量少）：

- `Assets/Scripts/Features/_Shared/`（仅放事件 structs、枚举、纯数据 DTO；不得放具体业务逻辑）

> 注意：跨 Feature 通信必须用 **Events / Bindables / Queries**，禁止直接引用对方 Controller；状态写入必须走 **Commands**。

---

## 3. 跨 Feature 的“契约”（必须先统一）

> AI 在开始写每个 Feature 前，先创建 `_Shared` 契约，避免后续反复改名导致返工。
> 只要满足需求即可，不要过度设计成“通用框架”。

### 3.1 共享枚举（建议）

- `ELightConsumeReason`：Darkness / ShadowContact / Debug / Script
- `EDeathReason`：LightDepleted / Fall / Debug / Script

### 3.2 共享事件（建议最小集合）

- `LightChangedEvent { float Current; float Max; }`
- `LightDepletedEvent { }`
- `DarknessStateChangedEvent { bool IsInDarkness; }`
- `SafeZoneStateChangedEvent { int SafeZoneCount; bool IsSafe; }`
- `PlayerDiedEvent { EDeathReason Reason; UnityEngine.Vector3 WorldPos; }`
- `PlayerRespawnedEvent { UnityEngine.Vector3 WorldPos; }`
- `LampSpawnedEvent { int LampId; UnityEngine.Vector3 WorldPos; }`
- `LampCountChangedEvent { int Count; int Max; }`
- `RunFailedEvent { }`
- `RunResetEvent { }`

> 说明：事件 payload 尽量是值类型/DTO；不要把 MonoBehaviour/Transform 引进事件里。

---

## 4. 交付要求（每个 Feature 都必须做）

每个 Feature 完工后必须在该 Feature 根目录新增：

- `FeatureNote.md`

内容至少包含：

1) 这个 Feature 提供什么能力（简述）
2) 需要在 Unity 场景中放哪些 GameObject/组件/Prefab（步骤化）
3) 必需的配置项（例如：SO 参数、Collider 触发器范围、UI 引用等）
4) 最小验证步骤（如何在编辑器里触发并看到结果）
5) 常见坑/排查方式（可选，但推荐）

---

## 5. 全局实现顺序建议（强建议按此顺序）

1) Feature 02 LightVitality
2) Feature 03 Darkness
3) Feature 08 SafeZone
4) Feature 04 DeathRespawn
5) Feature 05 KeroseneLamp
6) Feature 06 RunFailReset
7) Feature 07 HUD

原因：Light 是全局依赖；Darkness/SafeZone 都是 Light 的“写入者”；Death 依赖 LightDepleted；Lamp 依赖 PlayerDied；RunFail 依赖 LampCount；HUD 最后接线最省事。

---

# Feature 02 — LightVitality（光=生命）

## 实现功能目标

- 提供一个全局“光值=生命值”的状态源：`CurrentLight` 与 `MaxLight`。
- 光值变更 **必须**通过 Commands；UI/其他 Feature 只能读或订阅变化。
- 对外输出：Bindable（用于 HUD）+ 事件（用于其他 Feature，如 Death）。
- 支持最小化配置：Max/Initial（可用 ScriptableObject 或硬编码默认值；但不要做持久化）。

## 任务清单

- [ ] 02.1 创建目录与基础骨架（按 Playbook 的 Feature Skeleton）
  - `Assets/Scripts/Features/LightVitality/{Commands,Queries,Events,Models,Systems,Controllers,Views}/`
- [ ] 02.2 在 `_Shared` 中创建 `ELightConsumeReason`（若不存在）
- [ ] 02.3 创建 Model：`ILightVitalityModel` + `LightVitalityModel`
  - 只暴露 **只读** Bindables：`BindableProperty<float> CurrentLight`、`MaxLight`
  - `OnInit()` 设置默认 Max/Initial（可从 Config 读取）
- [ ] 02.4 创建 Commands（写路径唯一入口）
  - `SetMaxLightCommand(float max, bool clampCurrent)`
  - `SetLightCommand(float value)`（调试/重置用）
  - `AddLightCommand(float amount)`
  - `ConsumeLightCommand(float amount, ELightConsumeReason reason)`
- [ ] 02.5 创建 Events（或复用 `_Shared`）
  - LightChangedEvent、LightDepletedEvent
  - 在 Commands 内触发（或 Model 内触发，按 Playbook 允许方式；但写入必须由 Command 发起）
- [ ] 02.6 创建 Queries（读路径）
  - `GetCurrentLightQuery`
  - `GetMaxLightQuery`
  - `GetLightPercentQuery`
- [ ] 02.7 Root 注册（如需）：在 `GameRootApp.Init()` 注册 `ILightVitalityModel`
- [ ] 02.8 写一个最小 Controller（仅用于测试/调试）
  - `LightVitalityDebugController`：按键加光/扣光（用 SendCommand）
  - 注意生命周期与日志规则
- [ ] 02.9 写 `FeatureNote.md`（配置与验证步骤）

**验收标准（DoD）**

- 任意 Feature 可通过 Query 读取光值；HUD 可通过 Bindable 绑定；
- 扣光到 0 会触发 `LightDepletedEvent` 且只触发一次（不反复抖动）；
- 没有任何 Controller/System 直接写 `BindableProperty.Value`（写入必须在 Commands）。

---

# Feature 03 — Darkness（黑暗判定与黑暗伤害）

## 实现功能目标

- 使用 **Trigger Zone** 做黑暗判定（逻辑判定不依赖渲染结果）。
- 玩家处于黑暗时，以 `DarknessDrainPerSec` 持续 `ConsumeLightCommand`。
- 提供滞后/缓冲（EnterDelay / ExitDelay 或 hysteresis），避免边界抖动。
- 对外输出：`IsInDarkness`（Bindable 或事件）供 HUD 显示；并供 SafeZone/其他规则参考。

## 任务清单

- [ ] 03.1 创建目录骨架：`Assets/Scripts/Features/Darkness/...`
- [ ] 03.2 在 `_Shared` 创建/复用 `DarknessStateChangedEvent`
- [ ] 03.3 创建 Model：`IDarknessModel` + `DarknessModel`
  - 只读 Bindable：`BindableProperty<bool> IsInDarkness`
  - 只存“是否在黑暗”与计数/延迟状态（不要存 Unity 引用）
- [ ] 03.4 创建 Commands（写路径）
  - `SetInDarknessCommand(bool isInDarkness)`
- [ ] 03.5 创建 System：`IDarknessSystem` + `DarknessSystem`
  - `Tick(float dt)`：若 `IsInDarkness == true` 且不在 SafeZone（参考 Feature 08 的“门控”），则 `ConsumeLightCommand(DarknessDrainPerSec * dt, Darkness)`
  - 注意：System 不能直接写 Model（必须 SendCommand）
- [ ] 03.6 创建 Unity 侧触发器组件（Controller/Views）
  - `DarknessZone2D`：挂 `Collider2D isTrigger`，只负责 OnTriggerEnter/Exit 发送“进入/离开”信号（不要写 Model）
  - `PlayerDarknessSensor`：维护进入黑暗 zone 的计数，并做滞后处理；最终通过 `SendCommand(SetInDarknessCommand)`
- [ ] 03.7 创建一个“Tick 驱动器”
  - `DarknessTickController`（MonoBehaviour, IController）：`Update()` 调用 `this.GetSystem<IDarknessSystem>().Tick(Time.deltaTime)`
  - 放在场景里（例如挂在一个 `GameSystems` 空物体上）
- [ ] 03.8 Root 注册（如需）：注册 `IDarknessModel`、`IDarknessSystem`
- [ ] 03.9 写 `FeatureNote.md`（如何在关卡里放 DarknessZone、如何给 Player 加 Sensor、如何验证掉光）

**验收标准（DoD）**

- 玩家进入黑暗区后稳定开始掉光；离开后稳定停止；边缘不频闪；
- 黑暗状态变化会发送事件并可被 HUD 读取；
- 掉光的写路径只来自 `ConsumeLightCommand`。

---

# Feature 08 — SafeZone（灯安全区规则：免伤 + 回血）

> 放在 Feature 03 之后实现，因为它会影响黑暗扣光逻辑的“门控”。

## 实现功能目标

- 玩家在任意“灯安全区”内：
  1) **禁止黑暗扣光**（DarknessSystem 必须尊重该门控）
  2) 按 `SafeRegenPerSec` 持续 `AddLightCommand`
- 安全区判定来自 **Trigger Zone**（通常由煤油灯 Prefab 提供 SafeZoneCollider）。
- 输出：`IsSafe` / `SafeZoneCount` 给 HUD。

## 任务清单

- [ ] 08.1 创建目录骨架：`Assets/Scripts/Features/SafeZone/...`
- [ ] 08.2 在 `_Shared` 创建/复用 `SafeZoneStateChangedEvent`
- [ ] 08.3 创建 Model：`ISafeZoneModel` + `SafeZoneModel`
  - `BindableProperty<int> SafeZoneCount`
  - `BindableProperty<bool> IsSafe`（可由 Count>0 推导，但仍可暴露只读）
- [ ] 08.4 创建 Commands（写路径）
  - `SetSafeZoneCountCommand(int count)` 或 `EnterSafeZoneCommand/ExitSafeZoneCommand`（二选一，保持简单）
- [ ] 08.5 创建 System：`ISafeZoneSystem` + `SafeZoneSystem`
  - `Tick(float dt)`：若 `IsSafe==true`，则 `AddLightCommand(SafeRegenPerSec * dt)`
- [ ] 08.6 创建 Unity 侧触发器组件
  - `SafeZone2D`（Collider2D isTrigger）：作为区域标识（通常挂在灯 Prefab 的子物体上）
  - `PlayerSafeZoneSensor`：维护计数（enter++/exit--），并通过 Command 更新 Model
- [ ] 08.7 创建 Tick 驱动器
  - `SafeZoneTickController`：Update 调 `ISafeZoneSystem.Tick(dt)`
- [ ] 08.8 与 Darkness 的门控集成（关键）
  - DarknessSystem.Tick 内：若 `this.GetModel<ISafeZoneModel>().IsSafe.Value == true` 则不执行黑暗扣光
  - 注意：这里只是 **读** SafeZoneModel（允许），但不得跨层持有引用；每帧 Query/Model 读取即可
- [ ] 08.9 Root 注册（如需）：注册 `ISafeZoneModel`、`ISafeZoneSystem`
- [ ] 08.10 写 `FeatureNote.md`（如何在灯 Prefab 上加 SafeZone2D，如何在 Player 上加 Sensor）

**验收标准（DoD）**

- 站在安全区内：不再黑暗扣光，且光值缓慢回升；离开立即恢复；
- SafeZone 状态变化可被 HUD 显示；
- 回血写路径只来自 `AddLightCommand`。

---

# Feature 04 — DeathRespawn（死亡与重生）

## 实现功能目标

- 统一死亡入口：`Kill(EDeathReason reason)`（Unity侧触发）→ 发送 `PlayerDiedEvent`（带世界坐标）。
- 死亡触发源：
  - LightDepletedEvent（来自 Feature 02）
  - Fall / KillVolume（可选：Y 阈值或触发器）
- 重生：回到 `RespawnPoint`（关卡放置），并触发 `PlayerRespawnedEvent`。
- 不做复杂动画/演出；重生需要复位“必要状态”（例如重新启用控制器、可选重置速度）。

## 任务清单

- [ ] 04.1 创建目录骨架：`Assets/Scripts/Features/DeathRespawn/...`
- [ ] 04.2 在 `_Shared` 创建/复用 `EDeathReason`、`PlayerDiedEvent`、`PlayerRespawnedEvent`
- [ ] 04.3 创建 Model：`IDeathRespawnModel` + `DeathRespawnModel`
  - 只存：`BindableProperty<bool> IsAlive`（可选）/ `BindableProperty<int> DeathCount`（如果你想用“死亡次数”而不是“灯数量”来失败）
- [ ] 04.4 创建 Commands（写路径）
  - `MarkPlayerDeadCommand(EDeathReason reason, Vector3 pos)`
  - `MarkPlayerRespawnedCommand(Vector3 pos)`
  - （可选）`IncrementDeathCountCommand()`
- [ ] 04.5 创建 System：`IDeathRespawnSystem` + `DeathRespawnSystem`
  - 负责：在 MarkDead/MarkRespawned 时做“状态写入 + 事件广播”（仍须 Command 写入 Model）
- [ ] 04.6 创建 Unity 侧 Controller
  - `DeathController`：
    - OnEnable 订阅 `LightDepletedEvent`（自动注销）→ `Kill(LightDepleted)`
    - 监听 FallDetector / KillVolume → `Kill(Fall)`
    - `Kill()` 内：SendCommand(MarkPlayerDead...) + `PlayerDiedEvent`（由 System 或事件系统触发，二选一，但要一致）
  - `RespawnController`：
    - 响应 RunReset / PlayerDied（按你的流程）执行实际的 Transform 移动到 RespawnPoint
    - 复位必要组件后 SendCommand(MarkPlayerRespawned...)
- [ ] 04.7 设计“死亡→重生”的时序（必须写在 PLAN 的交互图里）
  - 建议：Kill 立即触发 DiedEvent；Respawn 可以延迟 0.1s（可选）但原型可立即
- [ ] 04.8 Root 注册（如需）：注册 Model/System
- [ ] 04.9 写 `FeatureNote.md`（如何放 RespawnPoint、如何配置 KillVolume、如何验证死亡与重生）

**验收标准（DoD）**

- 光耗尽必定触发死亡并重生；
- 死亡事件包含正确世界坐标（用于灯生成）；
- 订阅无泄漏（反复 enable/disable 不会重复触发）。

---

# Feature 05 — KeroseneLamp（煤油灯系统：死亡留灯 + 上限）

## 实现功能目标

- 玩家死亡时，在死亡位置生成一盏 **煤油灯**（Prefab）。
- 灯在本次关卡挑战内“永久存在”（直到 RunReset/关卡重开）。
- 灯数量上限（默认 3）：超过则触发失败（由 Feature 06 处理）。
- 灯 Prefab 提供：
  - 可视化灯对象
  - （必须）SafeZone2D 触发器子物体（供 Feature 08 使用）

## 任务清单

- [ ] 05.1 创建目录骨架：`Assets/Scripts/Features/KeroseneLamp/...`
- [ ] 05.2 在 `_Shared` 创建/复用 `LampSpawnedEvent`、`LampCountChangedEvent`
- [ ] 05.3 创建 Model：`IKeroseneLampModel` + `KeroseneLampModel`
  - 只存：当前灯列表的“数据视图”（例如 count、max、nextId）
  - 不要在 Model 存 GameObject 引用（Unity 引用放 Controller/Utility 或 Manager 里）
- [ ] 05.4 创建 Commands（写路径）
  - `SetLampMaxCommand(int max)`
  - `RecordLampSpawnedCommand(int lampId, Vector3 pos)`（更新计数、广播事件）
  - `ResetLampsCommand()`（用于 RunReset 清理状态）
- [ ] 05.5 创建 Unity 侧 LampManager（建议放 Controllers 或 Utilities）
  - 责任：真正 Instantiate Prefab、维护实例列表、在 RunReset 时销毁实例
  - 注意：不要绕过 CQRS 写 Model；实例生成后通过 Command 告知 Model 更新计数
- [ ] 05.6 创建“死亡监听器”
  - 订阅 `PlayerDiedEvent`：
    - 如果还没超过上限：在位置生成灯，发送 RecordLampSpawnedCommand
    - 发送 LampSpawnedEvent（或由 Command/System 统一广播）
- [ ] 05.7 Prefab/资源准备（可先用简单 Sprite）
  - 创建 `LampPrefab`（放到项目资源目录，由你决定），包含：
    - `SafeZone2D` 子物体（Collider2D isTrigger，范围可调）
- [ ] 05.8 Root 注册（如需）：注册 `IKeroseneLampModel`
- [ ] 05.9 写 `FeatureNote.md`（如何配置 LampPrefab、如何设置 SafeZone 范围、如何验证死亡留灯）

**验收标准（DoD）**

- 每次死亡都会在死亡点生成灯（位置正确）；
- 重生后灯仍在；RunReset 会清灯；
- 灯数量变化会广播（供 Feature 06 / HUD）。

---

# Feature 06 — RunFailReset（失败条件与关卡重开）

## 实现功能目标

- 基于“灯数量上限”判定失败：当 `LampCount > LampMax`（或 ==Max 且再次死亡）时触发 `RunFailedEvent`。
- 失败后“一键重开本关挑战”：
  - 清灯（Feature 05）
  - 重置光值（Feature 02）
  - 重置黑暗/安全区状态（Feature 03/08）
  - 把玩家送回 RespawnPoint（Feature 04）
- 通过一个统一事件：`RunResetEvent` 来广播重开，避免互相硬引用。

## 任务清单

- [ ] 06.1 创建目录骨架：`Assets/Scripts/Features/RunFailReset/...`
- [ ] 06.2 在 `_Shared` 创建/复用 `RunFailedEvent`、`RunResetEvent`
- [ ] 06.3 创建 Model：`IRunFailResetModel` + `RunFailResetModel`
  - `BindableProperty<bool> IsFailed`
  - （可选）`BindableProperty<int> Attempts`（可不做）
- [ ] 06.4 创建 Commands（写路径）
  - `MarkRunFailedCommand()`
  - `ResetRunCommand()`（写入 IsFailed=false 等）
- [ ] 06.5 创建 System：`IRunFailResetSystem` + `RunFailResetSystem`
  - 监听 LampCountChanged（来自 Feature 05）或在 Tick 中查询 Count/Max
  - 触发失败：SendCommand(MarkRunFailedCommand) + 发送 RunFailedEvent
  - 提供 `RequestReset()` 方法：发送 RunResetEvent + SendCommand(ResetRunCommand)
- [ ] 06.6 创建 Unity 侧 Controller（输入/按钮）
  - `RunResetController`：失败后显示简单 UI 或按键（如 R）调用 `RequestReset()`
- [ ] 06.7 为各 Feature 增加 Reset 响应（通过订阅 RunResetEvent）
  - LightVitality：SetLightCommand(initial)
  - Darkness：SetInDarknessCommand(false)
  - SafeZone：SetSafeZoneCountCommand(0)
  - Lamps：ResetLampsCommand + LampManager 清实例
  - DeathRespawn：执行实际 Transform 回 RespawnPoint + MarkPlayerRespawnedCommand
- [ ] 06.8 Root 注册（如需）：注册 Model/System
- [ ] 06.9 写 `FeatureNote.md`（如何触发失败、如何重开、如何验证各系统被重置）

**验收标准（DoD）**

- 达到失败条件时能稳定进入失败状态；
- Reset 后：灯清空、光恢复、黑暗/安全区归零、玩家回出生点；
- Reset 的跨 Feature 协作仅通过事件/Commands 完成，没有直接 Controller 引用。

---

# Feature 07 — HUD（核心 HUD：光条/剩余次数/状态）

## 实现功能目标

- 显示：
  - 光值（数值或条）
  - 黑暗状态（InDarkness）
  - 安全区状态（IsSafe / SafeZoneCount）
  - 灯数量/剩余容错次数（Count / Max）
  - 失败提示（IsFailed）
- HUD 只能读状态（Query/Bindable），不得写入业务状态（除非你做 Debug 按钮，且必须走 Commands）。

## 任务清单

- [ ] 07.1 创建目录骨架：`Assets/Scripts/Features/HUD/...`
- [ ] 07.2 选择 UI 方式（按架构书要求）
  - 若项目已用 UIKit：创建一个 Panel（遵从 codegen 规则）
  - 若暂未用 UIKit：用简单 UGUI Canvas + MonoBehaviour Controller（但依旧遵从 IController + Bindable 订阅）
- [ ] 07.3 创建 HUDController（IController）
  - 订阅 LightVitalityModel.Bindables 更新 UI
  - 订阅 Darkness/SafeZone/Lamp/RunFailReset 的 Bindables 或 Events
  - 订阅须自动注销（OnDisable + UnRegisterWhenXXX）
- [ ] 07.4 若使用 Query：实现必要 Queries（或复用已有）
- [ ] 07.5 （可选）提供 Debug 按钮（加光/扣光/重开），但必须 SendCommand，不得直接改 Model
- [ ] 07.6 写 `FeatureNote.md`（如何在场景中放 HUD、如何绑定引用、如何验证）

**验收标准（DoD）**

- HUD 能实时反映光值/状态/灯数量/失败；
- 反复打开/关闭 HUD 不会导致多重订阅与重复刷新；
- HUD 不含任何业务写路径（或写路径全部走 Commands 且明确为 Debug）。

---

# Feature 08 已在上文（SafeZone）定义
