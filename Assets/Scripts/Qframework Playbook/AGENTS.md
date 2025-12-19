# Codex Engineering Playbook for QFramework (Project Seed)

> QFramework location: `Assets/QFramework/`
>
> Playbook output: `Assets/Scripts/Qframework Playbook/`
>
> Goal: Make future AI/Codex development follow QFramework’s intended architecture and conventions:
> Read rules → Decide placement → Implement → Self-check → Report.

---

## Scope（范围）

本 Playbook **只覆盖 QFramework 框架本体/官方意图**：
- 分层/Architecture 的使用规范
- 推荐模式与入口（以仓库中确认存在为准）
- 初始化/注册/释放/生命周期
- 命名、目录组织（以官方示例为准）
- “需求 → 落位”的决策树、清单、模板

本 Playbook **不包含**（后续再加）：
- 项目个性化规则（例如日志框架替换、禁用 Debug.Log 等）
- 第三方插件、CI、代码格式化、提交规范等

---

## Golden Rules（最高优先级）

1) **证据驱动**  
   每条关键规则必须附：
   - Source: `<relative path>` — `<section/class/example>` (optional: heading/line)
   找不到证据必须标 `UNVERIFIED`，并写明下一步去哪找（路径+关键词）。

2) **证据优先级**  
   用户手册/教程与示例工程 > API 文档说明 > 源码注释 > 推断

3) **只读 QFramework**  
   不修改 `Assets/QFramework/**`。本次只允许写入：
   `Assets/Scripts/Qframework Playbook/**`

---

## Repository Map（仓库地图：由构建代理填写）

- QFramework Root:
  - `Assets/QFramework/`

- User Manual / Tutorial Entry:
  - `TODO: Assets/QFramework/...`
  - Source: TODO

- API Docs Entry:
  - `TODO: Assets/QFramework/...`
  - Source: TODO

- Source Code Root:
  - `TODO: Assets/QFramework/...`
  - Source: TODO

- Examples / Samples Root (if any):
  - `TODO: Assets/QFramework/...`
  - Source: TODO

> 完整地图见：`MAP_QFRAMEWORK_LOCATIONS.md`

---

## How Future AI Must Use This Playbook（后续 AI 必须遵守的流程）

任何业务开发任务，必须按顺序输出并执行：

### 1) Read
- 读本文件：`AGENTS.md`
- 读导航：`INDEX.md`
- 读相关规则：`RULES/*`

### 2) Plan（必做）
在写代码前，先输出：
- 本功能涉及哪些 QFramework 分层概念（以实际存在为准）
- 计划新增/修改的文件清单（路径）
- 每个文件的“落位层级”（Model/System/Controller/View/Utility…）
- 可能新增的机制点：Command/Query/Event/BindableProperty…（如适用）

### 3) Implement
- 严格按 `RULES/architecture.md` 的分层/依赖边界落位
- 使用 `RULES/patterns.md` 中确认存在的推荐机制
- 生命周期按 `RULES/lifecycle.md` 的证据规则处理

### 4) Self-check
- 是否存在跨层反向依赖？
- 是否绕开框架推荐入口（直接 new、乱找单例、硬耦合）？
- 是否遗漏注册/解绑/释放（若该机制存在）？

### 5) Report
- 变更文件列表（路径）
- 每个文件属于哪一层（按 Playbook 定义）
- 最小手动验证步骤（Unity 里如何触发验证）

---

## Deliverables Layout（Playbook 结构）

- `AGENTS.md`：总纲（本文件）
- `INDEX.md`：任务导航入口
- `MAP_QFRAMEWORK_LOCATIONS.md`：QFramework 资产地图（手册/API/源码/示例）
- `RULES/architecture.md`：分层与依赖边界 + 决策树（最关键）
- `RULES/lifecycle.md`：初始化/注册/释放/PlayMode 注意点
- `RULES/naming.md`：命名与目录组织
- `RULES/patterns.md`：推荐模式与入口（以实际存在为准）
- `RECIPES/add_feature_checklist.md`：新增 Feature 的可执行清单
- `TEMPLATES/feature_skeleton.md`：Feature 骨架模板（结构示意）

---

## Evidence Format（证据格式）

每条关键规则下使用：

- Source: `<relative path>` — `<section/class/example>` (optional: heading/line)
- Notes: 用一句话说明证据如何支持规则

若无证据：
- UNVERIFIED: 说明推断原因
- Next search: 给出检索路径与关键词（例如在 `Assets/QFramework/` 下 grep 哪些词）

---

## Build Plan（构建代理必须完成）

1) 先写 `MAP_QFRAMEWORK_LOCATIONS.md`：把 `Assets/QFramework/` 下的手册/API/源码/示例入口路径列清楚  
2) 再写 `RULES/architecture.md`：提炼分层与依赖边界 + 决策树 + 证据  
3) 再写 `RULES/lifecycle.md`：初始化/注册/释放/PlayMode 注意点 + 证据  
4) 再写 `RULES/patterns.md`：确认存在的机制清单 + 用法/禁忌 + 证据  
5) 再写 `RULES/naming.md`：从示例提炼命名与目录组织 + 证据  
6) 最后生成 `INDEX.md`、`RECIPES`、`TEMPLATES`，保证后续 AI 能直接照着执行

---

## Current Status（由构建代理填写）

- Completed:
  - [ ] MAP_QFRAMEWORK_LOCATIONS.md
  - [ ] RULES/architecture.md
  - [ ] RULES/lifecycle.md
  - [ ] RULES/naming.md
  - [ ] RULES/patterns.md
  - [ ] INDEX.md
  - [ ] RECIPES/add_feature_checklist.md
  - [ ] TEMPLATES/feature_skeleton.md

- UNVERIFIED / Missing Evidence:
  - TODO
