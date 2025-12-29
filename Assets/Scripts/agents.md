## agents.md — Save System (Easy Save 3) Implementation Playbook

### 0. Mission / 目标

为 Unity 项目实现**单存档槽**的独立存档系统（Save/Load），基于 **Easy Save 3 (ES3)**。

**验收标准：**

* UI 上有两个按钮：`Save` 与 `Load`
* 玩家在任意时刻按 `Save`，再做一些改变，然后按 `Load`：

  * 场景中的关键状态能恢复到 `Save` 时刻
  * 尽量“宁杀错不放过”，体量小可以多存，优先正确性与可恢复性
* 单场景游戏，不做多槽，不做云存档，不做复杂版本迁移（但要留接口/结构方便未来扩展）

---

### 1. Constraints / 约束与原则

1. **集中式 Utility**

* 新增一个清晰的存档模块：`SaveSystem`（或 `SaveUtility`）
* Features 内尽量只做“注册/提供数据 + 挂钩”，不要把存档逻辑散落在各 Feature 里

2. **渐进式接入**

* 先做最小闭环：能存/能读/能恢复核心状态
* 然后逐个 Feature 扫描与接入：每接入一个 Feature，都要可测试

3. **可维护性优先**

* 所有“存什么/怎么存/怎么恢复”必须有清晰的结构与文档
* 不允许“想到哪存到哪”的硬编码散弹式写法

4. **运行态 vs 持久态区分**

* 持久态：玩家进度、开关、位置、已拾取、已触发、资源库存、已解锁……
* 运行态：缓存、临时插值、AI 当前寻路路径、瞬时动画参数、临时计时器（除非影响玩法）、对象引用缓存……
* 但本项目原则是：**不确定就先存**，再根据实际问题删减

---

### 2. Architecture / 总体设计（必须按这个结构落地）

#### 2.1 文件与命名建议（可按你项目风格调整，但结构别改散）

* `Assets/Scripts/SaveSystem/`

  * `SaveManager.cs`（入口：Save / Load / Delete / HasSave）
  * `SaveKeys.cs`（常量 Key：文件名、版本号、根 key 等）
  * `SaveSnapshot.cs`（“一次存档”的根数据结构）
  * `ISaveParticipant.cs`（可选：参与存档的接口）
  * `SaveRegistry.cs`（可选：自动发现/注册参与者）
  * `SaveLog.cs`（可选：日志开关、调试信息）
  * `Docs/SaveSystem.md`（最终必须产出：非常详细的使用说明）

> 你可以不做这么多文件，但**必须**做到：入口集中、数据结构清晰、参与者接入统一、文档完整。

#### 2.2 数据策略：Snapshot Root（统一快照）

* 采用一个根对象 `SaveSnapshot`，包含：

  * `int version`
  * `string timestamp`
  * `Dictionary<string, object>` 或 `Dictionary<string, string>`（更稳：存 JSON 字符串）
  * 或更类型安全：`List<FeatureSaveBlock>`（每个 Feature 一个块）

推荐做法（稳定 + 易排查）：

* 每个 Feature 输出自己的 `FeatureSaveData`（可序列化 class）
* 存档时把它们塞进 `SaveSnapshot.blocks[featureKey]`
* ES3 保存整个 `SaveSnapshot`（或保存 snapshot 的 JSON）

---

### 3. Save Flow / Load Flow（必须实现的行为）

#### 3.1 Save 流程（按顺序）

1. `SaveManager.Save()` 被按钮调用
2. `SaveManager` 构造一个新的 `SaveSnapshot`
3. `SaveManager` 向所有参与者收集数据：

   * 参与者来自：自动扫描（FindObjectsOfType）或显式注册（推荐）
4. 对每个参与者：

   * `participant.Capture()` -> 返回可序列化数据
   * 保存到 snapshot 对应块
5. `ES3.Save(SaveKeys.Snapshot, snapshot, SaveKeys.Settings)`
6. 打印清晰日志：存了哪些块、大小（或字段数）、耗时

#### 3.2 Load 流程（按顺序）

1. `SaveManager.Load()` 被按钮调用
2. 判断是否存在存档：`ES3.KeyExists(...)` 或 `ES3.FileExists(...)`
3. 读取 snapshot：`ES3.Load<SaveSnapshot>(...)`
4. 进入“恢复阶段”：

   * **阶段 A：准备**（停止会干扰恢复的系统）

     * 暂停 AI、暂停生成器、暂停动态刷怪、暂停时间推进（如果存在）
   * **阶段 B：销毁/重建**（如果你的场景存在“运行中生成的对象”）

     * 根据 snapshot 的对象列表重建（或先清空再创建）
   * **阶段 C：应用状态**

     * 对每个参与者调用 `participant.Restore(data)`
   * **阶段 D：收尾**

     * 恢复暂停的系统，必要时刷新 UI、重算缓存
5. 打印清晰日志：恢复了哪些块、是否有缺块、耗时

> 如果项目里没有运行时生成对象，也要至少支持“Transform / 激活状态 / 已拾取隐藏 / 机关开关”这种恢复。

---

### 4. “参与者”接入标准（AI 必须按这个做）

你要扫描整个项目 `Features/*`，找出**有状态**的系统，并按以下方式接入存档：

#### 4.1 两种接入方式（二选一，推荐第二种）

A) **接口式（推荐）**

* 每个需要存档的对象（或系统）实现 `ISaveParticipant`

  * `string SaveKey { get; }`
  * `object Capture()` 或 `T Capture<T>()`
  * `void Restore(object data)`
* `SaveManager` 在 Save/Load 时统一遍历

B) **集中适配器式**

* 不改 Feature 代码太多
* 在 `SaveSystem/Adapters/` 下写适配器：

  * `PlantSaveAdapter`、`LightSaveAdapter`、`PuzzleSaveAdapter`……
* 适配器内部引用 Feature 的运行对象，抽取数据并恢复

> AI 自行选择最少侵入的方式，但必须统一风格：要么大多接口式，要么大多适配器式，别混得乱七八糟。

#### 4.2 重点：对象标识（必须处理）

恢复对象时最怕“找不到对应对象”。

必须选择一种可靠标识策略：

* **Scene 内固定对象**：用一个 `PersistentId` 组件（string GUID）

  * 每个需要存档的关键对象挂一个 `PersistentId`
  * 编辑器下自动生成/校验唯一性
* **运行时生成对象**：存 `prefabId + guid + spawnData`

  * Load 时按 guid 重建，并恢复 transform/自定义状态

AI 需要在项目里搜索：

* 任何“可交互对象、可拾取物、可破坏物、机关、门、植物、灯、NPC、对话状态、任务/剧情状态、库存/资源”等
  并确保它们具备可恢复的 ID 或可定位方式。

---

### 5. What to Save（AI 扫描时的清单）

AI 扫描 Feature 时，按这套优先级判断哪些需要存：

**P0（必须存）**

* 玩家（或主要控制对象）：

  * world position / rotation / facing
  * 当前状态（例如攀爬/追光/回巢/受控等）如果会影响恢复
  * HP/资源/关键数值
* 关卡关键机关：

  * 开关是否触发、门是否打开、桥是否放下、是否解锁
* 可拾取/一次性物品：

  * 已拾取则隐藏/禁用，不可复活（除非设计允许）
* 影响通路的破坏/修复类状态
* 剧情/对话/任务推进的关键 flag（如果有）

**P1（应该存）**

* 植物生长/回缩的阶段、冷却、是否已被点亮/枯萎
* 光源/灯的状态（点亮/熄灭、是否可用、剩余时间）
* 任何“玩家看得见的改变”（场景布置/摆件状态）

**P2（可选，但体量小建议也存）**

* UI 选择、音量设置（如果项目里已经有设置系统）
* 临时计时器（如果会影响玩法体验）

---

### 6. Testing Protocol / 自测与调试要求（必须做）

AI 每接一个 Feature，必须写一个简单可复现的自测步骤记录在文档里。

最低自测集合（最终验收你会做的）：

1. 改变玩家位置 -> Save -> 再移动 -> Load -> 回到原位置
2. 触发一个机关（门开）-> Save -> 关门/离开/改变 -> Load -> 门仍开
3. 拾取一个物品 -> Save -> 退出/乱改 -> Load -> 物品保持“已拾取”
4. 对多个系统同时改动（至少 5 项状态）-> Save -> 大幅改动 -> Load -> 全部恢复

调试要求：

* `SaveManager` 必须有详细 log 开关：

  * 存档包含哪些块、块大小（或字段数）、耗时
  * 读档缺失块要告警但不中断（允许版本演进）

---

### 7. Implementation Steps / AI 工作步骤（按顺序执行）

1. **项目勘察**

* 扫描 `Assets/Scripts/Features/`（或你的 Feature 根目录）
* 输出：一份“候选可存系统清单”（按 P0/P1/P2 分类）
* 同时找出：

  * 哪些对象是固定场景物
  * 哪些对象是运行时生成
  * 哪些系统会在 Load 时主动刷新/覆盖状态（潜在冲突）

2. **搭建 SaveSystem 骨架**

* 完成 `SaveManager.Save()` / `SaveManager.Load()` 闭环
* 完成 `SaveSnapshot` 数据结构
* 完成 `PersistentId`（如需要）与查找机制
* 做一个 Demo 参与者：只存/读玩家 Transform（先跑通）

3. **逐个 Feature 接入**

* 从 P0 开始，一个一个接
* 每接入一个：

  * 增加 Capture/Restore
  * 增加必要的 ID
  * 写自测步骤
  * 确保 Save/Load 不会报错，不会产生重复对象/幽灵对象

4. **处理 Load 顺序与系统暂停**

* 如果发现加载后某些 Feature 会在 `Start/OnEnable/Update` 把状态覆盖回默认值：

  * 增加 `SaveManager` 的“恢复中”标记
  * 或让对应系统在恢复阶段暂停运行
  * 或提供 `OnBeforeRestore/OnAfterRestore` 钩子

5. **输出最终文档**

* `Docs/SaveSystem.md` 必须非常详细，包含：

  * 系统目标与能力边界
  * 一键存读如何接 UI
  * 如何让新 Feature 接入存档（示例代码）
  * Key 规范（SaveKey 命名、PersistentId 规范）
  * Load 阶段说明与常见坑
  * 调试方式与日志解释
  * 已接入的 Feature 列表与各自存了哪些字段

---

### 8. Key Conventions / 命名规范（强制）

* `SaveKey` 必须稳定、可读、可 grep：

  * `player.core`
  * `puzzle.door.<persistentId>`
  * `pickup.<persistentId>`
  * `feature.<FeatureName>.<SubSystem>`
* `PersistentId` 推荐：`GUID` 字符串（编辑器生成）
* `SaveSnapshot.version` 从 `1` 开始

---

### 9. Non-goals / 明确不做

* 多存档槽
* 云同步
* 跨版本复杂迁移（只做基础 `version` 与缺字段容错）
* 加密防篡改（除非你未来明确要）

---

### 10. Deliverables / 最终交付物清单（必须全部给到）

1. 可运行代码：

* `SaveManager` + `SaveSnapshot` +（必要的）`PersistentId`
* 至少覆盖 P0 项（玩家 + 机关 + 拾取物等）
* Save/Load 按钮示例或可直接挂到现有 UI

2. 文档：

* `Docs/SaveSystem.md`（非常详细，作为未来 AI 的二次开发说明书）

3. 接入报告（写进文档也行）：

* 已接入哪些 Feature
* 每个 Feature 存了哪些字段
* 哪些暂不存，以及理由

---

