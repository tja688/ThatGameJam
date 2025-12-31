# agents.md — Interaction / Backpack / Scroll / KeroseneLamp 改造落地指令

## 你的身份与目标

你是**Unity 游戏程序开发工程师**，接到一组对现有系统的**修改 + 新增**需求。你需要在项目中落地这些功能，并确保改动与现有架构一致。

### 强约束（必须遵守）

* **开发约束必须遵守**：`Assets/Scripts/QFramework_Playbook`

  * 开工第一步：打开并阅读 Playbook，总结你将遵守的关键规范点（例如：目录结构、命名、事件/消息机制、依赖管理、Feature 边界、序列化规则等），并在 PR/汇报里写明“哪些代码点对应哪些规范”。
* 每个 feature 根目录下有 `feature note`（或类似说明文件）：

  * **改任何 feature 前先读 note**，用它来判断：你应该改哪个 feature、应该新增哪个 feature、避免误改。
* 不要在一个点上“顺手大改架构”。按需求最小改动、可验证、可回滚。

---

## 需求总览（按交付拆分）

你要完成以下 7 组内容：

1. **玩家控制新增按交互逻辑**
2. **新增一个 Interactable Feature（可交互系统）**

   * 交互对象挂脚本 + 触发器
   * 玩家在触发器内按Interact执行交互
   * 多个交互对象需**排队/顺序**，不能一起触发
   * 两类交互：**可对话**、**可拾取**
3. **简易背包系统（只装拾取物）**

   * 支持丢弃
   * 支持外部灵活查找（查询 API）
4. **把“自杀”动作逻辑改为：丢弃当前手持物品**

   * 名字先不变，只改逻辑
   * 丢完默认空手
5. **鼠标滚轮上下监听（名字叫 Scroll）**
6. **滚轮驱动的背包选中 + 3按钮循环 UI 选中器控制器**

   * 滚轮上下 → 选中目标前后切换（循环）
   * 3 个按钮范围内做“滚轮式选中器”表现
   * 最末端按钮淡隐淡入，形成循环效果
   * 要注意：内部信息通信/交互要清晰（事件/消息/回调）
7. **煤油灯（KeroseneLamp）改造**

   * 其他物品：拾取后**直接入背包**，不展示手持
   * 煤油灯特殊：有物理，拾取后涉及物理/碰撞处理
   * 改为 4 状态状态机（你实现切换控制）

     1. 在背包内
     2. 被手持
     3. 被丢弃
     4. 失去功能
   * “跨区域关闭光照”逻辑**独立**，不算状态：根据玩家所在区域做表现开关
   * 四个状态：**进入状态立刻触发 UnityEvent**（我来绑定具体逻辑）

     > 你只负责：状态切换 + 触发事件，不要再包一层复杂业务逻辑。

---

## 统一设计要求（你需要这样落地）

### A. 交互优先级与“顺序触发”

* 玩家可能同时站在多个可交互物体触发器内。
* 交互必须 **一次只触发一个对象**，并且有确定顺序（建议实现其一）：

  1. **最近优先**（按距离排序）
  2. **进入顺序优先**（队列）
  3. **显式优先级字段** + 再按距离/顺序
* 需要一个“当前可交互目标”的概念：用于 E 键触发，以及后续 UI 提示（即使你现在不做 UI，也要留接口）。

### B. 输入层

* 使用 Unity Input System（项目若已在用则沿用现有 Input Action 资产与风格）。
* 新增：

  * `Interact (E)`
  * `Scroll`（滚轮上下：输出 value）
* 输入不要散落在各处：要有清晰入口（例如 PlayerInput/输入适配器/输入服务）。

### C. 背包与“手持”的关系（关键）

* 拾取物品默认：**入背包**，不自动手持、不挂到手上。
* “手持”是玩家一个独立状态/槽位（例如 `HeldSlot`），可由背包选中器切换决定“当前手持引用”。
* 丢弃：

  * 丢弃的是“当前手持物品”（来自 HeldSlot）
  * 丢弃后 HeldSlot 为空手
  * 丢弃后物品生成/回到世界中（带物理/碰撞规则）

---

## Feature 划分建议（按最小落地）

> 你可以按 Playbook 调整命名/目录，但建议保持下面的职责边界。

### 1) 新增 Feature：`InteractableFeature`

包含：

* `Interactable` 组件（挂在可交互对象上）

  * 必需：一个 Trigger Collider（2D/3D按项目）
  * 字段：`InteractableType { Dialogue, Pickup }`
  * Dialogue 类型：引用/绑定 Dialogue System 的 Trigger（由策划/关卡提前配好）
  * Pickup 类型：引用一个 `ItemDefinition`（用于入背包）
* `PlayerInteractSensor`（挂在玩家上）

  * 负责维护“触发器内候选列表”
  * 提供 `GetCurrentCandidate()`（按优先级规则取一个）
* `InteractionController`（挂在玩家上或 PlayerFeature）

  * 监听 `Interact(E)` 输入
  * 拿到当前 candidate → 执行交互
  * 执行后要防止同帧/重复触发（冷却/锁）
* 交互执行规则：

  * Dialogue：只负责“触发 DialogueSystemTrigger”，对话逻辑由对话系统自己处理
  * Pickup：调用 Backpack.Add(item)，然后让世界物体消失/禁用（或回收到池中）

### 2) 新增 Feature：`BackpackFeature`

包含：

* `Backpack`（数据容器 + API）

  * Add / Remove / Contains / GetAll / FindById / FindByTag 等最小必要查询能力
  * 对外提供“事件”：例如 OnChanged、OnSelectedChanged（给 UI/手持系统用）
* `ItemDefinition`（ScriptableObject 推荐）

  * id、显示名、icon、prefab(用于丢弃生成)、stackable(可选)等
* `HeldSlot`（或 `HandSlot`）

  * 当前手持 item（引用 ItemDefinition 或 item instance）
  * SetHeld(item) / ClearHeld()
  * 丢弃时用 item.prefab 在玩家前方生成，并清空手持

### 3) 修改现有玩家“自杀”逻辑

* 保留原输入/按钮/函数名（策划要求名字先不变）
* 内部实现改为：`DropHeldItem()`
* 丢弃成功 → 空手

### 4) 新增 Scroll 输入与滚轮 UI 选中器

* 输入侧：

  * `Scroll` action：滚轮上下输出（通常是 float 或 Vector2.y）
  * 做一个 `ScrollInput`/`ScrollListener`，对外抛事件 `OnScrollUp/OnScrollDown`（带节流阈值）
* 逻辑侧：

  * `InventorySelectorController`

    * 监听 scroll
    * 在 backpack 列表中循环切换 selected index
    * 同步 HeldSlot（选中即手持 or 选中仅选中、按别的键才手持——按你的默认实现，但要留开关）
* UI侧（3按钮循环表现）：

  * `BackpackWheelUIController`

    * 输入：当前选中的 index、背包 items
    * 输出：刷新 3 个按钮显示（前/中/后）
    * 循环淡隐淡入：对“末端”按钮做 alpha 动画（用协程/DoTween/Animator按项目规范）
  * 信息通信要求：

    * Backpack ->（事件）-> Selector ->（事件）-> UI
    * 不要 UI 反向直接改数据；UI 只发“用户意图”（可选），核心状态由 Selector 管

### 5) 煤油灯 KeroseneLamp 改造（重点）

为煤油灯单独做组件与状态机（建议独立 Feature 或放在物品 Feature 下的子模块）：

#### 状态机：4 状态

* `InBackpack`
* `Held`
* `Dropped`
* `Disabled`

你要实现：

* 状态切换方法：`SetState(state)`
* **进入每个状态立刻触发 UnityEvent**：

  * `OnEnterInBackpack`
  * `OnEnterHeld`
  * `OnEnterDropped`
  * `OnEnterDisabled`
* 状态切换时对物理/碰撞做基础处理（只做“必要底层处理”，不要包业务）：

  * InBackpack：通常禁用世界碰撞/刚体模拟/渲染（按项目需要）
  * Held：挂到手持点/或跟随玩家；处理碰撞（例如忽略与玩家碰撞）
  * Dropped：恢复刚体/碰撞，放到地面
  * Disabled：按需求让其失效（例如不可再点亮/不可交互），但具体表现由 UnityEvent 绑定决定

#### 跨区域关闭光照（独立于状态）

* 做一个独立组件：`LampRegionLightController`（或按项目命名）
* 输入：玩家当前区域（来自你们现有区域系统/触发器/黑板）
* 输出：仅控制“灯光表现开关”（Light enabled / VFX enabled）
* 注意：它不改变上述 4 状态，只改表现开关

---

## 具体落地步骤（执行顺序）

1. 阅读 `Assets/Scripts/QFramework_Playbook`，整理你将遵守的规则点
2. 逐个查找并阅读现有 Feature 的 feature note，确认：

   * 玩家输入/控制在哪个 feature
   * 是否已有拾取/物品/手持的旧实现（决定复用/替换）
   * 是否已有“自杀”逻辑所在位置
3. 先实现 **输入层**：Interact(E) + Scroll
4. 实现 **InteractableFeature**（先做 Dialogue + Pickup 最小闭环）
5. 实现 **BackpackFeature**（Add/Drop/Query + 事件）
6. 把“自杀”逻辑重定向到 DropHeldItem
7. 实现 **Selector + UI Controller**（先无动画可验收，再加淡隐淡入）
8. 改造 **KeroseneLamp**：

   * 状态机 + 物理/碰撞基础处理 + UnityEvents
   * 独立区域光照控制组件
9. 串联全流程测试 + 输出使用说明文档（给后续关卡/策划接入）

---

## 验收标准（必须可测）

### 交互

* 玩家进入可交互触发器范围内，按 E：

  * 只触发 **一个**交互对象
  * 多个对象同时在范围内时，触发顺序稳定且符合你实现的规则（并在文档中说明）
* Dialogue 类型：按 E 会触发 DialogueSystemTrigger，对话系统能正常开始
* Pickup 类型：按 E 会进入背包，世界物体消失/不可再捡（或按回收策略）

### 背包/手持/丢弃

* 拾取物品默认入背包，不自动展示手持
* Scroll 上下滚动会改变选中项（循环）
* UI 三按钮显示正确，循环时末端按钮有淡隐淡入效果
* “自杀”按钮触发后：丢弃当前手持物品到地上，随后空手

### 煤油灯

* 煤油灯拾取后可进入背包状态（必要的物理/碰撞处理正确）
* 可在 Held / Dropped / Disabled / InBackpack 四状态间切换
* 每次进入状态都会触发对应 UnityEvent（可在 Inspector 里看到并可绑定）
* 区域光照开关逻辑独立：玩家跨区域时只影响灯光表现，不改变灯状态

## 补充

背包内物品必须能在UI中展示，Assets/Scripts/UI/README_UI_Integration.md 这里是UI文档，里面预留了对接背包物品信息的接口

背包状态、玩家选中物品状态以及本次升级中所有你觉得有必要的保存的状态信息必须被存档系统存档，Assets/Scripts/SaveSystem/Docs/SaveSystem.md 这里是存档系统的文档。

---

## 交付物（你最终必须输出）

1. 代码实现（按 Playbook 规范）
2. 一份简短的开发说明 `README`（建议放在新 feature 根目录）：

   * 如何在场景里配置一个可对话/可拾取对象
   * 如何配置背包与 UI Selector
   * 煤油灯 4 状态分别意味着什么、UnityEvent 怎么绑
   * 交互多目标时的优先级规则说明
   * UI和存档系统相关是否有额外配置操作？如何执行？

3. 修改点清单：你改了哪些 feature、哪些脚本、为什么改这里（引用 feature note / Playbook 原则）
