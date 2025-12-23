# Markdown
agents.md 

— Unity 架构扫描器 60 分基础版
目标（必须实现）

在现有的 Mermaid 检视器 EditorWindow（UI Toolkit）基础上升级为一个两页签工具：

Config Tab（配置页）

提供：扫描、范围选择、规则配置、结果存放路径配置

配置必须持久化：Unity 关掉/重开后保持上次配置，不需要重新填

点击“Scan / 扫描”后：

执行基础扫描（先实现最小可用：基于路径/命名空间/命名后缀的结构化汇总即可）

输出结构化结果到你配置的“结果存放位置”（JSON + 生成的 Mermaid 文本文件或 JSON 内含 Mermaid 字符串）

扫描完成后自动切换到 Mermaid Tab 并展示

Mermaid Tab（展示页）

右侧或左侧（推荐左侧）增加一个Side Bar，内部是一个TreeView（层级树）

主区域继续使用现有 Mermaid 绘制器显示图

TreeView 支持导航切换：

点 “L0（全局 Feature 图）” → 展示 L0 Mermaid

展开某个 Feature → 点它的 “L1（内部结构图）” → 展示该 Feature 的 L1 Mermaid

Feature 下更细的组件节点（Controller/System/Model/…）→ 点它的 “L2（外部调用图）” → 展示该组件/或该 Feature 的 L2 Mermaid

暂时不做“点击图节点跳转代码行号”（明确不碰）

核心：L0-L2 的轻松切换一定要做，TreeView 导航+主图联动必须可用。

60 分范围的“扫描”定义（不要过度）

本阶段扫描只需要做到：

能从指定 Root（如 Assets/Scripts）下，按规则划分：

Features（按路径或命名空间）

L1 组件分类（Controllers/Systems/Models/Commands/Queries/Events/Utilities/Infrastructure…）

L2 粗粒度“跨 Feature 引用关系”（可先做 type-name / namespace 级别；不强求 invocation 级别）

能生成对应 Mermaid 文本（L0、每个 Feature 的 L1、每个 Feature/组件的 L2）

能把这些结果存储到磁盘，并在 Mermaid Tab 中读取展示

先做到“能扫—能存—能读—能切换—能看懂”。语义精准、调用点定位、外部程序集细分等后续再做。

约束（必须遵守）

必须在 Unity EditorWindow + UI Toolkit 内实现

必须复用现有 Mermaid 检视器的渲染能力（不要重新造渲染轮子）

UI 要稳定、可维护，不要堆一堆临时代码

配置持久化必须可靠：推荐 ScriptableObject + EditorPrefs 组合（见下面）

UI 设计（具体要求）
窗口结构

顶部：Tabs（Config / Mermaid）

Config Tab：

“Scan Root”（扫描根目录）：TextField + “Select Folder”按钮

“Include/Exclude 规则”：至少支持 Exclude（逗号分隔或列表）：

**/Editor/**, **/Tests/**, **/ThirdParty/**, **/Temp/**

“Feature 识别规则”：二选一（先做路径法即可）

Path 模式：Features/<FeatureName>/...

Namespace 模式（可选）：ThatGameJam.Features.<FeatureName>....

“分类规则”：

默认按文件夹：Controllers/Systems/Models/...

兜底按后缀：*Controller, *System, *Model, *Command, *Query, *Event

“Output Folder（结果存放位置）”：TextField + Select Folder

Button：Scan（扫描）

扫描状态：进度/结果提示（Label）

Mermaid Tab：

左侧：Side Bar（TreeView）

右侧：Mermaid Viewer（现有渲染区域）

顶部可选：当前选中路径 Breadcrumb（例如 L1 / KeroseneLamp）

TreeView 结构（必须符合）

根：

L0 - Features Overview

Features 节点（可选分组）：

FeatureA

L1 - FeatureA Internal

L2 - FeatureA External Calls（粗粒度）

Components

Controller: XxxController

L2 - External Calls

System: XxxSystem

L2 - External Calls

Model: XxxModel

L2 - External Calls

FeatureB…

组件层级先不需要特别精确：TreeView 先能导航到“某 Feature 的 L1”和“某 Feature/组件的 L2”即可。

配置持久化方案（必须实现）

优先推荐这套组合（简单且稳）：

ArchitectureScannerSettings（ScriptableObject）

存在 Assets/Editor/ArchitectureScanner/ArchitectureScannerSettings.asset（或你规定的固定路径）

字段包含：

scanRootPath（string）

outputFolderPath（string）

excludePatterns（List<string>）

featureMode（enum: Path / Namespace）

featurePathToken（默认 "Features"）

namespacePrefix（默认 "ThatGameJam.Features"）

lastSelectedNavId（string，可选，用于记住 Mermaid Tab 上次选中）

由窗口加载/保存（每次修改立即写回 asset）

如果用户移动/删除 asset（极端情况）：

用 EditorPrefs 记住 settings asset 的 GUID 或路径作为 fallback

自动重建默认 settings asset

结果存放与读取（必须实现）

扫描完成后，在 outputFolderPath 下写入：

scan_index.json（总索引，必须）

包含：timestamp、scanRoot、features 列表、每个 feature 的 id/name、关联 mermaid 文件路径

Mermaid 文件（建议按固定结构）

L0_features_overview.mmd

Features/<FeatureName>/L1_internal.mmd

Features/<FeatureName>/L2_external.mmd

Features/<FeatureName>/Components/<ComponentName>/L2_external.mmd（可选，先做有则读，没有就回退到 Feature L2）

Mermaid Tab 打开时：

默认尝试读取 scan_index.json

如果不存在：提示 “No scan results found. Please run Scan in Config tab.”

如果存在：加载 TreeView 数据源并默认展示 L0

扫描实现（60 分版本的最低要求）

不做 Roslyn 语义绑定，先做“结构化 + 粗依赖”：

输入

scanRootPath 下所有 .cs 文件

排除 excludePatterns 匹配的路径（先做简单 contains/regex 即可）

Feature 划分（优先 Path 模式）

若路径中包含 /Features/<FeatureName>/ → FeatureName 取该段

否则归为 Misc 或忽略（由设置决定）

组件分类（L1）

先看路径段（Controllers/Systems/Models/Commands/Queries/Events/Utilities/Infrastructure）

再兜底看类名后缀（Controller/System/Model/Command/Query/Event）

每个文件至少归到一个 bucket

粗依赖（L2）

先实现最简单可用版本：

扫描 using + namespace 引用（或同一行出现 ThatGameJam.Features.<OtherFeature>）

或者扫描类型名 token（非常粗也行，但必须可解释）

目标：能画出 “FeatureA → FeatureB（依赖）” 这类边，并能列出可能的组件级边（可选）

备注：60 分阶段宁愿“有误报”，不要“空白图”。

Mermaid 生成规范（必须）

输出 Mermaid 文本要包含：

标题（注释即可）

节点命名保持稳定：Feature::<Name>、Controller::<Name> 等

L0：Feature nodes + dependency edges

L1：subgraph FeatureName 内展示 buckets（Controllers/Systems/Models…）并把文件/类挂进去

L2：只展示跨 Feature 的边（从当前 Feature/组件指向其它 Feature 或 External）