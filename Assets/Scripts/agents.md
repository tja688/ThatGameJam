# agents.md — Unity6 UI Toolkit 全量UI实现（UXML + USS）

在我的 Unity 6 项目里实现一套“可直接跑起来看效果”的 UI 系统。**必须使用 UI Toolkit**（UXML + USS），不允许用 uGUI/IMGUI 作为正式方案（允许仅在 Editor Debug 辅助，但本任务尽量不要）。

> 目标：把游戏里几乎所有 UI 需求用同一套 UI Toolkit 体系覆盖，并提供“假数据注入测试模式”，让我在未接好游戏系统时也能完整点点点、拖拖拖、看界面逻辑是否正确。

---

## 0. 总原则（必须遵守）

1. **UI Toolkit Only**
   - 所有界面用 `UIDocument + VisualTreeAsset(UXML) + StyleSheet(USS)`。
   - **不**使用 Canvas/uGUI Button/Text 等。

2. **分层菜单与统一暂停规则**
   - 只要 UI 打开（主菜单 / 设置菜单 / 重绑定菜单 / 玩家面板等），都应**暂停游戏**（默认 `Time.timeScale = 0`）。
   - UI 关闭后恢复（`Time.timeScale = 1`）。
   - 注意：UI Toolkit 本身不依赖 timeScale，但游戏逻辑会停。

3. **可扩展与可对接**
   - 目前许多系统（音频混音器、任务系统、道具系统等）未完成：UI 侧要做**明确的接口占位**与 **TODO 标识**。
   - 不要硬绑定到具体 Gameplay 脚本；用接口/事件/服务层方式解耦。

4. **可直接预览**
   - 提供 `UI Sandbox`/`Mock Injector`：一键注入假数据（任务、道具、玩家信息、音量等），用于 UI 调试。
   - 该按钮/入口必须有清晰注释说明：正式对接时应移除或用编译宏隐藏。

5. **一致风格与文件组织**
   - UXML 组件化：一级、二级、三级菜单分开模板，复用控件（按钮、列表项、滑条行等）。
   - USS 采用“主题变量 + 组件类名”方式，避免写死到具体元素层级。

---

## 1. 需要实现的界面清单（功能 + 行为）

### A) 主菜单（一级菜单，开场出现）
**出现时机**
- 游戏启动后立刻显示，遮挡游戏内容，暂停游戏。

**按钮（四个）**
1. **开始新游戏**
   - 调用占位 `IGameFlowService.StartNewGame()`
2. **继续游戏**
   - 调用占位 `ISaveService.LoadSlot(0)`
3. **设置**
   - 打开【设置菜单（二级）】
4. **退出游戏**
   - Editor 下：`UnityEditor.EditorApplication.isPlaying = false`
   - Build 下：`Application.Quit()`

**额外要求**
- 主菜单要能从“设置菜单的右下角按钮：回到主菜单”返回。
- 主菜单 UI 需要有清晰焦点/选中态（鼠标 hover、键盘/手柄导航都尽量支持）。

---

### B) 设置菜单（二级菜单，侧边栏 + 右侧内容）
**结构**
- 左侧：侧边栏（Tab 列表）
  - 音乐设置
  - 存档设置
  - 游戏设置
- 右侧：内容区域（根据左侧 Tab 切换）

**通用按钮**
- 右下角：**回到主菜单**
- 顶部或角落：**关闭/返回上一级**
  - 若从主菜单进入：返回主菜单
  - 若从游戏内进入：返回游戏（关闭设置 UI）

> 注意：设置菜单可能从游戏内直接打开，所以“返回上一级”必须支持不同入口路径（见第 4 节导航规则）。

---

#### B1) 音乐设置（右侧内容）
- 三行滑条（Slider）：
  - 背景音乐 BGM
  - 音效 SFX
  - UI 音效 UI SFX
- 每行包含：Label、数值显示（0-100 或 0.0-1.0）、可拖动滑条

**对接占位**
- `IAudioSettingsService.SetBgmVolume(float v01)`
- `IAudioSettingsService.SetSfxVolume(float v01)`
- `IAudioSettingsService.SetUiVolume(float v01)`
- 同时 UI 需要能从服务拿到当前值刷新：
  - `event Action<float> OnBgmChanged` 等，或 `Get*Volume()`

---

#### B2) 存档设置（右侧内容）
- 两个按钮：
  - **存档**（Save）
  - **读档**（Load）
- 下方一块“存档槽信息文本框”（固定一个槽位 Slot 0）
  - 展示：存档时间、游戏内状态摘要（例如区域名、死亡次数、光亮值等）
  - 若没有存档：显示“暂无存档”

**对接占位**
- `ISaveService.SaveSlot(0)`
- `ISaveService.LoadSlot(0)`
- `ISaveService.TryGetSlotInfo(0, out SaveSlotInfo info)`
  - `SaveSlotInfo` 建议字段：`DateTime savedAt`, `string areaName`, `int deathsInArea`, `int totalDeaths`, `float lightValue`, `string summary`

---

#### B3) 游戏设置（右侧内容）
这里按“市面标准”实现输入绑定设置；若内容较多，做成三级菜单：

二级菜单里：按钮组
- **键盘绑定设置**
- **手柄绑定设置**
- （可选）**鼠标绑定设置**（若你认为应独立）
- 下方：
  - **返回游戏**（关闭设置菜单，回到游戏）
  - **退出游戏**（同主菜单退出逻辑）

点击“键盘/手柄绑定设置”后：打开 **三级菜单覆盖窗口**（见 C）

---

### C) 重绑定菜单（三级菜单，覆盖在二级之上）
**表现**
- 一个弹窗/覆盖窗口（Modal），盖在设置菜单上
- 列表展示可绑定的动作（Input Action）
  - 每行：动作名 + 当前绑定 + “重新绑定”按钮 + （可选）“恢复默认”
- 支持 Esc/取消退出本次绑定操作
- 完成绑定后可返回二级菜单（关闭三级窗口即可）

**输入系统要求**
- 使用 Unity Input System（ActionAsset）
- 重绑定逻辑用标准流程实现：
  - `InputAction.PerformInteractiveRebinding()` 或官方常见做法
  - 绑定过程中显示提示文本：“按下任意键…”/“按下手柄按键…”
- 重绑定结果要可持久化（建议 PlayerPrefs/JSON，占位即可）
  - 提供 `IRebindPersistence` 占位：Save/Load Rebind Overrides

**对接占位**
- `IInputRebindService`：
  - `IEnumerable<RebindEntry> GetEntries(DeviceType device)`
  - `Task StartRebind(string actionId, int bindingIndex)`
  - `void ResetBinding(string actionId, int bindingIndex)`
  - `void SaveOverrides() / LoadOverrides()`
- UI 不直接访问 `InputActionAsset`，通过服务层拿数据（或由服务注入 asset）。

---

### D) 游戏内玩家面板（一级菜单，但仅游戏内可打开）
**打开方式**
- 游戏内按键/按钮打开（占位输入事件）
- 打开后暂停游戏
- 关闭返回游戏

**布局**
- 左侧区域：
  - 玩家原画（Portrait）
  - 下方：光亮值（Light Value）
  - 区域名（Area）
  - 当前区域死亡次数（Deaths in Area）
  - 总死亡次数（Total Deaths）
- 右侧区域：
  - 上半：任务提示区域（Quest Log）
    - 列表显示任务 1/2/3...
    - 点击任务项后显示详细信息：
      - 目标描述
      - 需要物品/条件
      - 状态：已完成/未完成
  - 下半：道具栏（Inventory）
    - 固定最多 6 格
    - 均匀排布，留白合理，不要挤
    - 每格显示：图标（可用占位 Sprite/Texture）、数量（可选）

**对接占位**
- `IPlayerStatusProvider`：
  - `PlayerPanelData GetData()`
  - `event Action<PlayerPanelData> OnChanged`
- `IQuestLogProvider`：
  - `IReadOnlyList<QuestData> GetQuests()`
  - `event Action OnQuestChanged`
- `IInventoryProvider`：
  - `IReadOnlyList<ItemStack> GetSlots(int maxSlots=6)`
  - `event Action OnInventoryChanged`

---

## 2. 导航与“上一级”规则（必须实现且不混乱）

实现一个统一的 `UIRouter` / `UIStack` 概念（推荐栈）：

- `Open(MainMenu)`：入栈 MainMenu
- `Open(Settings)`：从任意入口入栈 Settings，并记录 `SettingsOpenedFrom`（MainMenu / InGame）
- `Open(RebindModal)`：入栈 RebindModal（modal）
- `Open(PlayerPanel)`：入栈 PlayerPanel（仅 InGame）

“返回上一级”行为：
- 关闭当前栈顶 UI，回到上一层 UI
- 若栈空：回到游戏（无 UI）
- Settings 的“关闭/返回上一级”：
  - 若入口为 MainMenu：回 MainMenu
  - 若入口为 InGame：回游戏（栈清空）

Settings 的“回到主菜单”（右下角）：
- 清空栈，打开 MainMenu（或者直接切到 MainMenu 为栈顶）
- 同时触发游戏流程占位：`IGameFlowService.ReturnToMainMenu()`

---

## 3. 暂停策略（必须稳定）

集中管理，不允许每个面板自己改 timeScale 互相打架：

- `UIPauseService` 或 `UIRoot` 统一处理：
  - 当 UI 栈非空：`Time.timeScale = 0`
  - 栈为空：`Time.timeScale = 1`

注意：
- 如果未来需要某些 UI 不暂停（比如纯 HUD），要预留扩展点：
  - Panel 可标记 `PausesGame = true/false`

---

## 4. 工程落地：建议的目录与产物（必须交付）

在项目内创建（或按现有结构融入）如下目录：

- `Assets/UI/Toolkit/UXML/`
  - `MainMenu.uxml`
  - `SettingsMenu.uxml`
  - `Settings_Audio.uxml`
  - `Settings_Save.uxml`
  - `Settings_Game.uxml`
  - `RebindModal.uxml`
  - `PlayerPanel.uxml`
  - `Components/`
    - `SidebarItem.uxml`
    - `SliderRow.uxml`
    - `QuestListItem.uxml`
    - `InventorySlot.uxml`
- `Assets/UI/Toolkit/USS/`
  - `theme.uss`（变量/配色/字体尺寸/间距）
  - `components.uss`
  - `menus.uss`
- `Assets/Scripts/UI/`
  - `UIRoot.cs`（挂在场景里，持有 UIDocument）
  - `UIRouter.cs`（UI 栈与导航）
  - `UIPauseService.cs`
  - `Panels/`
    - `MainMenuPanel.cs`
    - `SettingsPanel.cs`
    - `RebindModalPanel.cs`
    - `PlayerPanel.cs`
  - `Services/Interfaces/`（全是占位接口）
    - `IGameFlowService.cs`
    - `ISaveService.cs`
    - `IAudioSettingsService.cs`
    - `IInputRebindService.cs`
    - `IPlayerStatusProvider.cs`
    - `IQuestLogProvider.cs`
    - `IInventoryProvider.cs`
  - `Mock/`
    - `UIMockBootstrap.cs`（假实现集合）
    - `UIMockDataInjector.cs`（一键注入、随机数据）
- `Assets/UI/Toolkit/Resources/`（如需 Resources 加载）
  - 可放 `VisualTreeAsset`/`StyleSheet`，或改用 Inspector 引用（推荐 Inspector 直引）

**必须交付内容**
1. 所有 UXML/USS 能在 Play Mode 正常显示并可交互
2. 菜单导航、返回、关闭逻辑完整
3. 暂停逻辑稳定（不会“关了一个 UI 还暂停”或“开了 UI 没暂停”）
4. Mock 模式可注入数据并刷新 UI
5. 一份对接文档（写在 `Assets/Scripts/UI/README_UI_Integration.md` 或直接注释很清晰也行，但要“可交接给另一个AI”）

---

## 5. Mock 注入与“测试按钮”要求（必须好用）

### Mock 注入入口（至少提供一种）
- 方案 A：在 Settings 菜单右上角加一个小按钮：`[DEV] Inject Mock Data`
- 方案 B：在 PlayerPanel 里加一个 `[DEV] Refresh Mock` 按钮
- 方案 C：在 Hierarchy 放一个 `UIMockBootstrap`，Inspector 勾选 `EnableMockMode`

### Mock 行为
点击后，UI 应立即显示一组“看起来真实”的数据：
- 任务：至少 3 条，包含完成/未完成混合
- 道具：6 格里随机 0~6 个占用，带数量
- 玩家：区域名、死亡次数、光亮值合理范围
- 音量：BGM/SFX/UI 分别给不同值，并能拖动更新 UI 上的数值显示

### 明确标注“正式对接时如何处理”
- 所有 DEV 按钮旁边写注释：对接完成后删除或用 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 包起来隐藏
- Mock 服务实现必须在代码中集中在 `Mock/` 目录，避免污染正式逻辑

---

## 6. UI 组件交互细节（必须实现）

1. **Hover/Pressed/Selected 反馈**
   - 按钮 hover 变亮/描边
   - Tab 选中有明显高亮
   - 列表项选中可展示细节

2. **键盘/手柄导航（尽量支持）**
   - UI Toolkit 默认 Focus 可用：确保按钮、滑条可 focus
   - Esc 关闭当前层（优先关闭三级，再二级，再一级）
   - Enter/Submit 激活按钮

3. **滑条显示同步**
   - 拖动滑条时数值文本同步更新
   - 如果服务层发来外部变化事件，也能刷新滑条位置（占位事件）

4. **任务列表 + 详情**
   - 左侧/上方列表，右侧/下方详情
   - 选中任务项时刷新详情面板

5. **道具栏固定 6 格**
   - 6 个 slot 永远存在；空格显示空态（透明/占位框）
   - 排布均匀，留白舒服

---

## 7. 代码质量与约束（必须）

- 不要写“到处 FindObjectOfType”
- UXML 元素查找统一封装：`root.Q<Button>("StartButton")` 等，命名一致
- 所有与 Gameplay 的交互都走接口服务（先 Mock，后对接）
- 关键逻辑写清楚注释，尤其是：
  - UI 栈/导航
  - 暂停策略
  - 重绑定流程与持久化位置
  - Mock 注入与移除方式

---

## 8. 验收清单（你完成后我如何验收）

我会在一个空场景或现有场景里：
1. 运行后看到主菜单，游戏暂停（timeScale=0）
2. 点“开始新游戏/继续游戏/退出”不会报错（占位可打印日志）
3. 点“设置”进入二级菜单，侧边栏切换右侧内容正确
4. 音乐滑条可拖动，数值显示变，且不会报错
5. 存档页：存/读按钮可按，下面文本框显示 mock 或占位信息
6. 游戏设置页：可进入重绑定三级菜单，Esc 可退出，能返回二级
7. 从游戏内打开玩家面板：左侧信息、右侧任务与道具显示正常
8. 点击 DEV 注入后，所有区域数据刷新
9. 关闭所有 UI 后恢复游戏（timeScale=1）

---

## 9. 你需要输出的“对接说明文档”内容（必须写）

在 `README_UI_Integration.md` 写清楚：

1. 哪些接口需要由未来系统实现（列出 Interfaces）
2. 各 UI 面板订阅了哪些事件、刷新入口是什么
3. 保存/加载/返回主菜单/开始新游戏等按钮要接到哪里
4. 重绑定数据如何保存与加载（你现在占位在哪里）
5. 如何移除 Mock：删哪些脚本/按钮/宏开关

---

## 10. 立即开始执行（行动顺序）

1. 在项目里落地目录结构与基础 UIDocument（UIRoot）
2. 先实现 UI 栈与暂停（UIRouter + UIPauseService）
3. 做主菜单 UXML/USS + Panel 脚本
4. 做设置菜单（侧边栏切换）+ 三个子页面（音频/存档/游戏）
5. 做三级重绑定 modal（先实现 UI 列表与按钮，再接 Input System）
6. 做玩家面板（任务列表+详情、道具6格、玩家信息）
7. 加 Mock 模式与 DEV 注入按钮
8. 写对接文档

---

## 备注（务必遵守）
- 我更在意：结构清晰、交互稳定、可对接、能直接看效果。
- 具体美术风格你可以先给一个简洁现代的默认主题（深色/浅色都行），但要统一且易扩展。
- 所有“未实现系统”的位置：用明确 `TODO(INTEGRATION): ...` 标注，并在文档里列清楚。
