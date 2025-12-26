agents.md/10_Modify_Existing_Features.md
10.1 RunFailReset（已实现）→ 需要降级/改造

现状：死亡次数阈值触发失败与 reset 


新目标：无限死亡，不再存在“失败触发 reset”



要求

移除“deathCount 到阈值触发 RunFailedEvent”的默认行为

保留（或迁移）一个“HardReset/TestReset”能力，仅开发/测试使用

全项目统一约定：

Respawn ≠ HardReset

机关默认不监听 Respawn 做世界回滚（除非明确需要）

验收：玩家死 100 次也不会触发 RunFailedEvent；只有测试脚本能触发 HardReset。

10.2 RunFailHandling（已实现）→ 废弃或仅保留测试占位

现状：监听 RunFailedEvent 并延迟 reset 



要求：若 RunFailedEvent 不再存在，应移除该控制器在场景中的依赖，避免误触发/日志污染。

10.3 DeathRespawn（已实现）→ 接入 Checkpoint

现状：RespawnController 使用固定 respawnPoint 



要求

Respawn 时从 Checkpoint feature 查询“当前复活 LevelNode/nodeId”

Respawn 需要保证：清速度、恢复输入、重置攀爬状态（已有 ResetClimbStateCommand 机制）



保留 DeathCount 作为统计/提示用，但不用于失败判定（可选）

10.4 KeroseneLamp（已实现）→ 引入 LampRegistry + 区域规则

现状：死亡生成灯、无上限 



新规则：跨区域关视觉光；每区域最多 3 个“有效灯”



要求（必须拆分灯状态）

每盏灯至少有两种独立开关：

VisualEnabled：控制 Light2D/发光表现（性能）

GameplayEnabled：控制是否参与“花点亮/藤蔓生长/虫子吸引”等玩法

新增 LampRegistry（可在 KeroseneLampModel 内扩展）：

记录 lampId → 实例引用/位置/所属区域/生成顺序/当前状态

新增“区域灯策略接口”：接入 AreaSystem（见新增 features），当区域切换时批量切换 VisualEnabled

实现“每区域最多 3 盏有效灯”：

新灯生成时，若超过上限：淘汰同区域最早灯 → 设置 GameplayEnabled=false（并给出明显反馈：变暗/熄灭音效/材质切换）

10.5 Mechanisms（已实现）→ 强化基类以承载后续大量机关

现状：Spike + Vine 原型 




要求

MechanismControllerBase 增加“可重置协议”：

至少区分 OnHardReset()（测试重置）

可选：OnAreaEnter/Exit()（区域性能策略）

VineMechanism2D 从“只监听 LampSpawnedEvent”升级为可选“持续光照判定/阈值计时”（为 BellFlower/VineGate 打底）

10.6 LightVitality / SafeZone / Darkness（小改）

保持 LightVitality 在 respawn 时回满的行为（已经满足）

Darkness 在 SafeZone 生效时不 drain（你现在描述已是这个目标）


agents.md/20_New_Features.md

按你规划分：核心系统（A）/ 机关（B）/ 剧情任务（C&D）。



A. 核心系统类（新增）
A1) Checkpoint / Mailbox（新增）

来源：A3 规划 



目标

玩家触发 Mailbox 后，写入当前 nodeId 作为“复活点唯一事实来源”

与“整图连通但分区域”的设计兼容

要求

LevelNode（数据对象）：nodeId、spawnPoint、所属区域 areaId

Mailbox（场景触发器）：只负责“把 nodeId 写入模型”

对外提供 Query：获取当前有效 spawnPoint / nodeId

DeathRespawn 在 respawn 时必须通过该 Query 获取位置

验收：玩家触发不同 Mailbox 后死亡，都会复活到最新 Mailbox。

A2) AreaSystem（新增：区域/性能策略）

来源：你在 A3 评价里提出的“跨区域关灯节能 + 每区域最多 3 盏有效灯”



目标

维护玩家当前所在 areaId

触发区域切换事件

驱动灯的 VisualEnabled 开关（仅性能，不影响灯作用）

要求

AreaVolume2D：区域触发器（可重叠，按优先级/最大面积规则选定）

PlayerAreaSensor：检测当前区域并发事件

AreaSystem：

OnAreaChanged：通知 KeroseneLamp 批量切换灯的 VisualEnabled

可扩展：后续也可用来做“区域 BGM/雾效/摄像机限制”

验收：玩家离开区域后，该区域历史灯的 Light2D 全部关闭，但灯依然存在（且未被淘汰的仍保留玩法效果）。

A3) DoorGate（新增：门规则层）

对应你“需不需要独立 feature”那段 



目标

维护 doorId 的条件与开关状态

接收 BellFlower 激活计数事件，满足条件则开门

要求

Door2D/Gate2D 放 Mechanisms（表现层）

DoorGate 只提供：DoorStateChangedEvent / DoorOpenEvent（或等价事件）

条件配置支持：requiredFlowerCount（先做最小）

验收：两朵花点亮后门开启；门脚本不需要知道“花是什么”。

A4) WorldReset（可选新增，用于测试）

把原 RunFailReset 的 “reset”语义迁移出来（如果你选择方案A废弃失败链路）。

目标

仅提供 TestReset/HardReset 事件

给机关/灯/临时物体一个统一的“回到初始状态”的入口

B. 机关/环境交互类（新增/扩展）
B1) BellFlower（新增）

来源：规划 B1 




目标：花被光照到达到阈值→点亮→发事件（不要在花里开门）

要求

点亮条件可选：持续时间/强度阈值

发 FlowerActivatedEvent(flowerId, isActive)

DoorGate 监听该事件完成开门计数

B2) IceBlock（新增）

来源：规划 B2 




目标：消耗 1/4 光融化，5 秒恢复

要求

触发方式：交互或靠近（由你选，但必须防抖）

融化时立即扣光；恢复倒计时；HardReset 强制 Frozen

B3) MeteorPool / DamageVolume（新增）

来源：规划 B3 




目标：进入区域扣 1/4 光（带 cooldown）

要求

做成通用 DamageVolume(costRatio, cooldown, reason)

MeteorPool 只是 prefab 配置实例

B4) FallingRockFromTrashCan（新增）

来源：规划 B4 




目标：触发后落石（一次性或周期），落地销毁/回收

要求

对象池（最小实现也要避免频繁 Instantiate）

掉落点列表配置化

伤害走统一 Hazard/Damage 规则（不要各写各的）

B5) VineGrowWithLight（扩展你现有 VineMechanism2D）

你现有是“灯生成在半径内→藤蔓长出来”



。
规划希望“根部持续有光才生长；无光回退”



要求

支持持续检测 + 进度 growthValue(0..1)

无光回退是渐变（可调速度）

HardReset 归零

可选：光源类型过滤（让“死亡灯/煤油灯”成为解谜条件）



B6) BugAI 套件（新增）

来源：B7~B11 



拆分建议（最小可落地）：

BugMovementBase（通用移动）

BugFearLight（怕光）

BugAttractLamp（靠近灯）

BugStompInteraction（踩踏触发：虫回位 + 玩家获得弹跳）



ElectricLure（电源/背包吸引器：给虫子下发强制 target）



B7) LightDeathZone / HazardVolume（新增）

来源：B13 




目标：红点/危险点统一为 HazardVolume：InstantKill 或 DrainFast

要求

统一接 DeathRespawn 或 LightVitality（不要散落在各脚本 if/else）

C/D. NPC/剧情/组合谜题（新增：轻量 StoryTasks）
C1) StoryTasks（新增）

来源：C1/C2/C3/D1…“组合动作”需求 



目标

用“触发器 + 动作序列 + 一次性标记”承载剧情逻辑

对话播放交给 Dialogue System 插件，但触发与排队由你掌控 



必须支持的 Action（最小集）

PlayDialogue(dialogueId, priority, once)

SpawnLamp(preset, positionRef)（用于牧羊犬→灯）



SetGateState(doorId/open)

SetFlag(flagId) / RequireFlag(flagId)


agents.md/30_Integration_Checklist.md

全局验收清单（AI 实现完必须自测通过）

无限死亡：死不会触发世界重置/失败流程 



复活点唯一来源：Mailbox/Checkpoint nodeId

灯规则：

跨区域只关视觉光（性能），玩法仍有效

超过每区域 3 盏：淘汰最早灯 → 玩法失效 + 清晰反馈 



机关重置语义：

Respawn 不回滚世界

HardReset 才回滚（测试用）

“会改变可达性”的机关必须保证不软锁（至少可逆/可替代/可恢复三选一）