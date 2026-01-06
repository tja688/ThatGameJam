# QFramework -> Codex Skills 构建计划（分析稿）

> 目标：把 QFramework 的核心架构、工具包与本项目 Playbook 约束沉淀为一组可组合的 Codex skills。默认优先用 QFramework 内置工具包；当用户明确要求外部方案时，必须遵守外部约束并禁用对应工具包。

## 1. 资料来源与证据路径

- 本地文档（权威顺序优先）：
  - `Assets/QFramework/QFramework_Docs_API.md`
  - `Assets/QFramework/QFramework_Docs_用户手册.md`
- 本地源码（只读参考）：
  - `Assets/QFramework/Framework/Scripts/QFramework.cs`
  - `Assets/QFramework/Toolkits/**`
- 项目规则（最高优先级）：
  - `Assets/Scripts/QFramework_Playbook/PLAYBOOK_v4.md`
  - `Assets/Scripts/QFramework_Playbook/RECIPES_v4.md`
  - `Assets/Scripts/QFramework_Playbook/TEMPLATES_v4.md`
- 线上技能结构参考（结构与组织方式，不作为 API 证据）：
  - https://github.com/openai/skills （示例：`gh-fix-ci`、`notion-spec-to-implementation`）

## 2. QFramework 架构核心（摘要）

来自 `QFramework.cs` 与手册“架构规范与推荐用法”的共识要点：

- **四层架构**：
  - Controller（IController）：Unity 入口、输入、表现
  - System（ISystem）：业务编排、跨 Feature 规则
  - Model（IModel）：状态、领域规则
  - Utility（IUtility）：基础设施（存储/网络/第三方 SDK）
- **CQRS 语义**：
  - Command：所有状态变更必须通过 Command
  - Query：读路径优先通过 Query
  - 事件/Bindable：状态变化向上层通知
- **通信规则**：上层可调用下层，下层不能引用上层；上行通知用 Event/Bindable。
- **核心机制**：`Architecture<T>` + IOC 容器；`TypeEventSystem` 事件系统；`BindableProperty` 数据绑定。

## 3. QFramework 组件与工具包（按功能域概览）

- **CoreKit（基础工具集）**：ActionKit、BindableKit、LogKit、PoolKit、SingletonKit、FluentAPI、FSMKit、TableKit、GridKit 等。
- **ResKit（资源管理）**：资源标记、模拟/真机模式、ResLoader 引用计数与回收、同步/异步加载、场景加载。
- **UIKit（UI 管理）**：层级与面板管理、自动绑定/代码生成、Panel 生命周期与加载策略。
- **AudioKit（音频管理）**：Music/Sound/Voice 通道划分；Settings 控制（开关/音量）；支持自定义 Loader。
- **DocKit（Pro）**：文档/图谱相关工具（可选）。

## 4. Playbook 约束（必须编码到 skills 的核心规则）

关键约束（高优先级）：

- **禁止修改区域**：`Assets/QFramework/**`（只读）。需要变更时只能输出 `MANUAL_ACTIONS`。
- **日志要求**：禁止 `UnityEngine.Debug.*`；必须使用 `QFramework.LogKit`。
- **单一 Root 架构**：必须用 `GameRootApp : Architecture<GameRootApp>`；禁止多 Root。
- **纵向切片**：功能必须在 `Assets/Scripts/Features/[FeatureName]/...`。
- **CQRS 强制**：写路径只允许 Command；System/Controller 不得直接写 Model。
- **事件对称与销毁**：订阅必须对应反注册，优先用 QF auto-unregister。
- **Toolkit 初始化**：UIKit/AudioKit loader override 和 ResKit.Init 必须在 `ProjectToolkitBootstrap` 中且在首次使用前。
- **API 证据优先级**：先本地 API 文档，再源码，最后线上（需提示版本不一致风险）。

## 5. Skill 组合与边界（推荐 7-9 个）

### 5.1 推荐 Skill 列表与职责

1) **`qframework-playbook`**
- 触发：本项目任何功能开发/修改。
- 责任：强制执行 Playbook 规则、产出 FeatureNote、日志规范、目录约束、禁区规则。

2) **`qframework-core-architecture`**
- 触发：提到 QFramework / QF / Architecture / MVC / CQRS / Command / Query / Model / System / Utility。
- 责任：四层架构规则、CQRS、事件/Bindable 规范、Architecture<T> 机制。

3) **`qframework-uikit`**
- 触发：UI 面板、UIKit、UIPanel、UIRoot、Panel 生命周期、UI 层级等关键词。
- 责任：UIKit 使用流程、Panel 加载/关闭、自动绑定、UI 层级管理。

4) **`qframework-reskit`**
- 触发：资源加载、AssetBundle、ResLoader、ResKit.Init 等。
- 责任：资源标记流程、ResLoader 生命周期、同步/异步加载。

5) **`qframework-audiokit`**
- 触发：音频播放、BGM/SFX/Voice、AudioKit Settings。
- 责任：AudioKit API、播放策略、Settings 绑定、loader override 规则。

6) **`qframework-actionkit`**
- 触发：延时/序列/并行动作、计时器、协程、时序编排。
- 责任：ActionKit 基本 API（Delay/Sequence/Parallel/Repeat）。

7) **`qframework-codegen`**
- 触发：UI 代码生成、绑定、Designer 文件、CodeGenKit。
- 责任：代码生成流程、禁止在 `*.Designer.cs` 写逻辑、Prefab 路径约定。

8) **`qframework-corekit-utils`**（可与 core 合并）
- 触发：日志/对象池/单例/绑定/事件工具。
- 责任：LogKit、PoolKit、SingletonKit、BindableProperty、TypeEventSystem。

9) **`qframework-dockit`**（可选）
- 触发：DocKit/文档图谱相关需求。
- 责任：DocKit 使用说明与项目约束。

### 5.2 技能组合规则（最小组合）

- 任何 QFramework 任务至少组合：`qframework-playbook` + `qframework-core-architecture`。
- 领域触发再追加对应 toolkit skill（UI/Audio/Res/Action）。
- 不要无条件加载全部技能，按关键词选择最小集合以控制上下文。

### 5.3 “内联工具调用”策略（默认使用内置工具包）

**默认映射**：
- 音频相关 → `AudioKit`
- UI 管理/面板 → `UIKit`
- 资源加载 → `ResKit`
- 时序动作/定时/序列 → `ActionKit`

**显式外部覆盖**（必须尊重）：
- 用户说“不要用 AudioKit/ResKit/UIKit/ActionKit”或指定 FMOD/Wwise/Addressables/自研库 → 禁用对应 toolkit。
- 若出现冲突指令（例如同时提到 “用 AudioKit” 与 “不用 QFramework 音频”），必须先询问澄清。

**建议在 SKILL.md 的决策模板**：
- `IF user requests QFramework audio` → AudioKit
- `IF user says external or禁止` → external path + 标注 `TOOLKIT_OVERRIDE`
- `ELSE` → follow default toolkit mapping

## 6. Skill 内容组织（Progressive Disclosure）

- **SKILL.md 必须极简**：只放流程、决策树、关键约束。
- **references/** 放精炼摘要**：
  - 把本地大文档提炼为“要点 + 搜索关键词 + 路径”。
  - 例如 `references/architecture.md` 只保留规则与关键接口，不复制全量手册。
- **scripts/** 仅放高频重复任务（如提取 API 片段、生成索引）。
- 每个 skill 引导 “如何从本地文档验证 API” 的查证路径。

## 7. 校验与验收方案

- **结构校验**：使用 `skill-creator/scripts/quick_validate.py` 和 `package_skill.py` 检查 YAML、目录结构、资源引用。
- **触发回归**：准备 8-12 条标准 prompt 做触发测试（例如：
  - “用 QF 做一个音频播放系统” → `qframework-audiokit`
  - “QF UI 面板打开/关闭” → `qframework-uikit`
  - “不用 AudioKit，用 FMOD 做” → 禁用 `AudioKit`）
- **规则校验**：检查是否遵守 Playbook：禁区、LogKit、CQRS、Root 架构。
- **证据校验**：对每个涉及 API 的例子给出 `Source`（本地文档/源码路径）。

## 8. 线上项目参考要点（结构层面）

- **openai/skills**：
  - `SKILL.md` 采用“Overview → Quick start → Workflow → Bundled Resources”结构。
  - 对需要高确定性的部分提供脚本（scripts/）。
  - 前置说明触发条件与依赖工具（如 gh/notion）。
- 可借鉴点：
  - 清晰的触发描述（description）
  - 低上下文高复用的流程拆分
  - 资源目录对齐（scripts/references/assets）

## 9. 分阶段实施计划（建议）

**Phase 0 — 需求澄清**
- 确认“必须覆盖的 toolkit 列表”与“外部库默认策略”。

**Phase 1 — 核心规则沉淀**
- 先做 `qframework-playbook` 与 `qframework-core-architecture`。

**Phase 2 — 领域 toolkit**
- 逐个落地 UIKit/ResKit/AudioKit/ActionKit。

**Phase 3 — 代码生成与通用工具**
- CodeGenKit + LogKit/PoolKit/Singleton/Bindable。

**Phase 4 — 验收与迭代**
- 通过 prompt 回归 + `package_skill.py`。
- 收集真实使用反馈再精简/拆分。

## 10. 待确认问题（已确认）

- 你希望优先支持的 toolkit 顺序是什么？覆盖哪些 toolkit?
优先构建核心规则沉淀，必须要我提到用qf框架开发就严格遵守相关要求，覆盖所有官方给出的toolkits

- 是否要把“Unity 内置方案”作为默认外部替代（如不用 AudioKit 就用 AudioSource）？
是，如果明确提到不用qf的kit就默认用Unity 内置方案，如果我想用AudioKit 我会明确、专门提到“用qf自带的音频工具开发”，如果我什么都不说，单纯用“开发xx系统”进行描述，则用unity原生方案，如果我说：“用qf组织代码”或者agents.md里面默认要求调用qframework-playbook 与 qframework-core-architecture这个核心kit，那就按qf的代码架构组织，但仍用unity原生方案，而不是qf的AudioKit 

- 是否需要中英双语 Skill（描述/参考）？
英文Skill（本体）+绝对不会影响AI进行信息检索、加重认知负担的对应中文skill介绍

---
