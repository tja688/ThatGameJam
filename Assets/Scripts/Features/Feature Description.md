Project Feature Report
扫描时间：2025-12-27 20:52:45 +08:00
扫描范围：Assets/Scripts/Features，Assets/Scripts/Root
Boundary Issues
RunResetController.cs 位于 Testing 目录但命名空间是 ThatGameJam.Features.RunFailReset.Controllers，且 RunFailReset 文档引用它为入口，导致 Testing 与 RunFailReset 边界不清晰；建议将该脚本移入 Assets/Scripts/Features/RunFailReset/ 或统一 Testing 目录/命名空间。
危险/死亡逻辑同时存在于 HazardVolume2D.cs 与 SpikeHazard2D.cs（两者都是即时死亡入口），责任边界模糊；建议统一归入 Hazard 或为 SpikeHazard2D 重新归类/命名以避免重复语义。
1) Feature Index（一览表）
FeatureKey/名称	入口点	主要职责	主要依赖 (Top 3)	被依赖者 (Top 3)	风险等级
SharedEvents (_Shared)	RunResetEvent 等事件结构体	共享事件/枚举定义	None	RunFailReset, LightVitality, DeathRespawn	Low
AreaSystem	GameRootApp.Init<br>PlayerAreaSensor<br>AreaVolume2D	维护当前区域并广播切换	RunFailReset, DeathRespawn	KeroseneLamp, Mechanisms	Med
BellFlower	BellFlower2D	光照计时激活花并广播	KeroseneLamp	DoorGate	Low
BugAI	BugMovementBase 系列	虫子移动与光照行为	KeroseneLamp	None	Low
Checkpoint	GameRootApp.Init<br>LevelNode2D<br>Mailbox2D	维护复活点节点与查询	None	DeathRespawn	Med
Darkness	GameRootApp.Init<br>PlayerDarknessSensor<br>DarknessTickController	黑暗状态与扣光	LightVitality, SafeZone, RunFailReset	HUD	Med
DeathRespawn	GameRootApp.Init<br>DeathController<br>RespawnController	死亡/复活状态与事件	LightVitality, Checkpoint, RunFailReset	KeroseneLamp, PlayerCharacter2D, Hazard	High
DoorGate	GameRootApp.Init<br>DoorGateConfig2D<br>DoorGateSystem	门的计数开关与事件	BellFlower, RunFailReset	Mechanisms, StoryTasks	Med
FallingRockFromTrashCan	FallingRockFromTrashCanController	触发落石与伤害	Hazard	None	Low
Hazard	GameRootApp.Init<br>DamageVolume2D<br>HazardVolume2D	统一伤害入口	LightVitality, DeathRespawn	FallingRockFromTrashCan	Med
HUD	HUDController	显示光/状态/灯数量	LightVitality, Darkness, SafeZone	None	Low
IceBlock	IceBlock2D	冰块消融/凝结与死亡	LightVitality, DeathRespawn, RunFailReset	None	Med
KeroseneLamp	GameRootApp.Init<br>KeroseneLampManager	灯注册/淘汰/视觉控制	AreaSystem, DeathRespawn, RunFailReset	BellFlower, BugAI, Mechanisms	High
LightVitality	GameRootApp.Init<br>LightVitalityResetSystem	光量资源与事件	RunFailReset, DeathRespawn	Darkness, SafeZone, Hazard	High
Mechanisms	MechanismControllerBase<br>VineMechanism2D 等	机关行为集合	AreaSystem, KeroseneLamp, DoorGate	None	Med
PlayerCharacter2D	GameRootApp.Init<br>PlatformerCharacterController	角色移动/攀爬	DeathRespawn	DeathRespawn	High
RunFailHandling	RunFailHandlingController	旧失败入口占位	RunFailReset	None	Low
RunFailReset	GameRootApp.Init<br>RunFailResetSystem	HardReset 事件入口	None	AreaSystem, LightVitality, KeroseneLamp	Med
SafeZone	GameRootApp.Init<br>PlayerSafeZoneSensor<br>SafeZoneTickController	安全区状态与回光	LightVitality, RunFailReset, DeathRespawn	Darkness, HUD	Med
StoryTasks	GameRootApp.Init<br>StoryTaskTrigger2D	触发器驱动剧情动作	DoorGate, KeroseneLamp	None	Med
Testing	RunResetController	测试触发 HardReset	RunFailReset	None	Low
2) Dependency Graph Summary（总依赖摘要）
2.1 Feature -> Feature 依赖列表（按被依赖次数排序）

RunFailReset（11）：AreaSystem, Darkness, DeathRespawn, DoorGate, IceBlock, KeroseneLamp, LightVitality, Mechanisms, SafeZone, Testing, RunFailHandling

DeathRespawn（9）：KeroseneLamp, PlayerCharacter2D, AreaSystem, SafeZone, Darkness, LightVitality, Hazard, IceBlock, Mechanisms

LightVitality（7）：Darkness, Hazard, SafeZone, IceBlock, Mechanisms(Ghost), HUD, DeathRespawn

KeroseneLamp（5）：BellFlower, BugAI, Mechanisms(Vine), HUD, StoryTasks

AreaSystem（2）：KeroseneLamp, Mechanisms

DoorGate（2）：Mechanisms(DoorMechanism2D), StoryTasks

SafeZone（2）：Darkness, HUD

Checkpoint（1）：DeathRespawn

Hazard（1）：FallingRockFromTrashCan

PlayerCharacter2D（1）：DeathRespawn

BellFlower（1）：DoorGate

其余（0）：BugAI, FallingRockFromTrashCan, HUD, IceBlock, RunFailHandling, StoryTasks, Testing, SharedEvents

2.2 循环依赖检测（如发现环，必须列出链路）

未发现扫描范围内的代码级循环依赖（事件驱动链条存在但无直接闭环）。

2.3 关键“枢纽Feature”（被很多人依赖的）

RunFailReset（HardReset 事件源）

DeathRespawn（生死事件源）

LightVitality（光量资源中心）

KeroseneLamp（光照与交互中心）

2.4 关键“高风险边”（例如：时序敏感/弱引用/字符串key）

DoorGate ← FlowerActivatedEvent：若 DoorGateConfig2D 尚未注册，UpdateDoorProgressCommand 会被直接忽略（UpdateDoorProgressCommand.cs）。

RunFailReset → 多个监听者：事件为一次性分发，禁用/未激活对象将错过重置（RunFailResetSystem.cs）。

AreaSystem → KeroseneLamp：区域切换时批量改视觉依赖 AreaId 一致性，空区域会导致全部灯可见（KeroseneLampManager.cs）。

LightVitality → DeathRespawn：同帧多源扣光/回光的命令顺序可能影响 LightDepletedEvent 触发时机（LightVitalityCommandUtils.cs）。

3) Feature Cards（逐个Feature详解）
[SharedEvents] Shared Events
Location: Assets/Scripts/Features/_Shared（namespace ThatGameJam.Features.Shared）
Entry Points: 事件/枚举定义（无运行时注册）
Responsibility: 统一跨 Feature 事件载荷与枚举

定义 Run/Death/Light/SafeZone/Darkness 等公共事件
统一 EDeathReason / ELightConsumeReason
作为事件总线的契约层
Public Surface（对外能力）:

Commands: None
Queries: None
Events: RunResetEvent, PlayerDiedEvent, PlayerRespawnedEvent, LightChangedEvent, LightConsumedEvent, LightDepletedEvent, LampSpawnedEvent, LampCountChangedEvent, DeathCountChangedEvent, DarknessStateChangedEvent, SafeZoneStateChangedEvent
Models/Data: EDeathReason, ELightConsumeReason
Unity Components/Prefabs: None
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：由各 Feature 发送/监听
核心判断条件（if/阈值/状态机）：无（纯数据）
主要调用链（从入口到关键动作，分步骤）：发送事件 → QFramework 事件总线 → 订阅者处理
关键状态与持有者：无
关键可配置参数（来自哪里，默认值在哪）：无
Dependencies（我依赖谁）:

None
Reverse Dependencies（谁依赖我）:
RunFailReset：事件总线(c) + 证据 RunFailResetSystem.cs
LightVitality：事件总线(c) + 证据 LightVitalityCommandUtils.cs
DeathRespawn：事件总线(c) + 证据 MarkPlayerDeadCommand.cs
Coupling & Risks:

耦合来源：事件总线；FeatureNote：无（目录仅事件/枚举）
潜在bug/时序风险：事件为 fire-and-forget，监听者未注册会丢失
容易误用的点：值类型事件不要依赖引用语义；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

在监听者中 RegisterEvent 并记录日志
触发对应 Feature 发送事件
验证载荷字段是否正确
Key Files（最重要的5-10个文件）:

RunResetEvent.cs 〞 HardReset 事件
PlayerDiedEvent.cs 〞 死亡事件
PlayerRespawnedEvent.cs 〞 复活事件
LightChangedEvent.cs 〞 光量变化事件
ELightConsumeReason.cs 〞 扣光原因枚举
[AreaSystem] AreaSystem
Location: Assets/Scripts/Features/AreaSystem（namespace ThatGameJam.Features.AreaSystem）
Entry Points: GameRootApp.Init 注册 Model/System；PlayerAreaSensor、AreaVolume2D 组件
Responsibility: 维护玩家当前区域并广播切换

聚合重叠区域并选择最优（优先级/面积）
更新 IAreaModel.CurrentAreaId
发送 AreaChangedEvent
Public Surface（对外能力）:

Commands: SetCurrentAreaCommand
Queries: GetCurrentAreaIdQuery
Events: AreaChangedEvent
Models/Data: IAreaModel.CurrentAreaId（Bindable）
Unity Components/Prefabs: PlayerAreaSensor, AreaVolume2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：进入/离开 AreaVolume2D 触发器
核心判断条件（if/阈值/状态机）：选择最高 priority，同级选面积更大
主要调用链（从入口到关键动作，分步骤）：触发器 → PlayerAreaSensor 聚合 → SetCurrentAreaCommand → AreaModel → AreaChangedEvent
关键状态与持有者：AreaModel._currentAreaId
关键可配置参数（来自哪里，默认值在哪）：AreaVolume2D.areaId/priority（Inspector）
Dependencies（我依赖谁）:

RunFailReset：事件总线(c) + 证据 PlayerAreaSensor.cs
DeathRespawn：事件总线(c) + 证据 PlayerAreaSensor.cs
Reverse Dependencies（谁依赖我）:
KeroseneLamp：直接代码引用(a) + 证据 KeroseneLampManager.cs
Mechanisms：事件总线(c) + 证据 MechanismControllerBase.cs
Coupling & Risks:

耦合来源：事件总线 + 命令调用；FeatureNote：已检查
潜在bug/时序风险：重叠刷新在 RunReset/Respawn 后延迟一帧执行，可能出现短暂空区域
容易误用的点：areaId 为空时被视为 string.Empty，需保持关卡一致；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

玩家挂 PlayerAreaSensor，场景放多个 AreaVolume2D
进入/离开区域观察 AreaChangedEvent
触发 RunResetEvent 与 PlayerRespawnedEvent 看区域刷新
Key Files（最重要的5-10个文件）:

PlayerAreaSensor.cs 〞 叠加检测与选择逻辑
AreaVolume2D.cs 〞 区域触发体
SetCurrentAreaCommand.cs 〞 更新模型并发事件
AreaChangedEvent.cs 〞 区域切换事件
AreaModel.cs 〞 区域状态模型
FeatureNote.md 〞 FeatureNote
[BellFlower] BellFlower
Location: Assets/Scripts/Features/BellFlower（namespace ThatGameJam.Features.BellFlower）
Entry Points: BellFlower2D 组件
Responsibility: 光照计时激活花并广播事件

统计可用灯数量与距离
计时满足阈值后激活/可选撤销
发送 FlowerActivatedEvent
Public Surface（对外能力）:

Commands: None
Queries: None
Events: FlowerActivatedEvent
Models/Data: None
Unity Components/Prefabs: BellFlower2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：Update 中检测附近灯光
核心判断条件（if/阈值/状态机）：lightAffectRadius + minLampCount + activationSeconds
主要调用链（从入口到关键动作，分步骤）：BellFlower2D.Update → GetGameplayEnabledLampsQuery → 计时 → FlowerActivatedEvent
关键状态与持有者：_lightTimer、_isActive（组件内）
关键可配置参数（来自哪里，默认值在哪）：lightAffectRadius、activationSeconds、minLampCount、allowDeactivate（Inspector）
Dependencies（我依赖谁）:

KeroseneLamp：直接代码引用(a) + 证据 BellFlower2D.cs
Reverse Dependencies（谁依赖我）:
DoorGate：事件总线(c) + 证据 DoorGateSystem.cs
Coupling & Risks:

耦合来源：查询调用 + 事件广播；FeatureNote：已检查
潜在bug/时序风险：灯列表为空时始终不激活；需保证灯 Feature 先注册
容易误用的点：minLampCount 小于 1 会被逻辑提升到 1；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

放置 BellFlower2D，配置 lightAffectRadius
用 KeroseneLamp 生成灯并靠近
观察 FlowerActivatedEvent 激活与撤销
Key Files（最重要的5-10个文件）:

BellFlower2D.cs 〞 光照计时逻辑
FlowerActivatedEvent.cs 〞 事件定义
FeatureNote.md 〞 FeatureNote
GetGameplayEnabledLampsQuery.cs 〞 灯查询依赖
DoorGateSystem.cs 〞 事件消费者
[BugAI] BugAI
Location: Assets/Scripts/Features/BugAI（namespace ThatGameJam.Features.BugAI）
Entry Points: BugMovementBase、BugFearLight、BugAttractLamp、BugStompInteraction、ElectricLure
Responsibility: 提供虫子移动与光照/踩踏行为组合

目标优先级移动与朝向
怕光/趋光行为（灯查询）
踩踏弹跳与诱饵吸引
Public Surface（对外能力）:

Commands: None
Queries: None
Events: None
Models/Data: None
Unity Components/Prefabs: BugMovementBase, BugFearLight, BugAttractLamp, BugStompInteraction, ElectricLure
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：Update 查询灯、Trigger 进入/退出诱饵
核心判断条件（if/阈值/状态机）：fearRadius / attractRadius / priority
主要调用链（从入口到关键动作，分步骤）：BugAttractLamp.Update → GetGameplayEnabledLampsQuery → BugMovementBase.SetTarget
关键状态与持有者：BugMovementBase._targets（字典）
关键可配置参数（来自哪里，默认值在哪）：moveSpeed、fearRadius、attractRadius、bounceVelocity（Inspector）
Dependencies（我依赖谁）:

KeroseneLamp：直接代码引用(a) + 证据 BugFearLight.cs
Reverse Dependencies（谁依赖我）:
None（未发现其他 Feature 直接依赖）
Coupling & Risks:

耦合来源：查询调用；FeatureNote：已检查
潜在bug/时序风险：灯查询为空时会清理目标，移动停止
容易误用的点：需要 BugMovementBase 配合使用；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

给虫子挂 BugMovementBase + BugAttractLamp
在附近生成灯，观察趋光
替换为 BugFearLight 验证远离行为
Key Files（最重要的5-10个文件）:

BugMovementBase.cs 〞 移动核心
BugAttractLamp.cs 〞 趋光逻辑
BugFearLight.cs 〞 怕光逻辑
BugStompInteraction.cs 〞 踩踏弹跳
ElectricLure.cs 〞 诱饵逻辑
FeatureNote.md 〞 FeatureNote
[Checkpoint] Checkpoint
Location: Assets/Scripts/Features/Checkpoint（namespace ThatGameJam.Features.Checkpoint）
Entry Points: GameRootApp.Init 注册 Model/System；LevelNode2D、Mailbox2D 组件
Responsibility: 维护复活点节点注册与查询

记录 nodeId、areaId、spawnPoint
触发器写入当前节点
发送 CheckpointChangedEvent
Public Surface（对外能力）:

Commands: RegisterCheckpointNodeCommand, UnregisterCheckpointNodeCommand, SetCurrentCheckpointCommand
Queries: GetCurrentCheckpointQuery, GetCurrentCheckpointNodeIdQuery
Events: CheckpointChangedEvent
Models/Data: ICheckpointModel.CurrentNodeId（Bindable）
Unity Components/Prefabs: LevelNode2D, Mailbox2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：玩家进入 Mailbox2D 触发器
核心判断条件（if/阈值/状态机）：triggerOnce / nodeId 是否有效
主要调用链（从入口到关键动作，分步骤）：Mailbox2D → SetCurrentCheckpointCommand → CheckpointModel → CheckpointChangedEvent
关键状态与持有者：CheckpointModel._nodes、_currentNodeId
关键可配置参数（来自哪里，默认值在哪）：LevelNode2D.nodeId/areaId/spawnPoint（Inspector）
Dependencies（我依赖谁）:

None
Reverse Dependencies（谁依赖我）:
DeathRespawn：查询调用(a) + 证据 RespawnController.cs
Coupling & Risks:

耦合来源：命令/查询；FeatureNote：已检查
潜在bug/时序风险：SetCurrentCheckpointCommand 在 node 未注册时仅发警告并返回空数据
容易误用的点：nodeId 为空会被忽略；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

放置 LevelNode2D 与 Mailbox2D
触发 Mailbox 后观察 CheckpointChangedEvent
触发复活看位置是否来自最新节点
Key Files（最重要的5-10个文件）:

LevelNode2D.cs 〞 节点注册
Mailbox2D.cs 〞 触发写入
CheckpointModel.cs 〞 模型
SetCurrentCheckpointCommand.cs 〞 当前节点设置
GetCurrentCheckpointQuery.cs 〞 查询接口
FeatureNote.md 〞 FeatureNote
[Darkness] Darkness
Location: Assets/Scripts/Features/Darkness（namespace ThatGameJam.Features.Darkness）
Entry Points: GameRootApp.Init 注册 Model/System；PlayerDarknessSensor、DarknessZone2D、DarknessTickController
Responsibility: 追踪黑暗状态并在黑暗中扣光

计算进入/离开黑暗的延迟状态
更新 IDarknessModel.IsInDarkness
通过 DarknessSystem.Tick 扣光
Public Surface（对外能力）:

Commands: SetInDarknessCommand
Queries: None
Events: DarknessStateChangedEvent
Models/Data: IDarknessModel.IsInDarkness（Bindable）
Unity Components/Prefabs: PlayerDarknessSensor, DarknessZone2D, DarknessTickController
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：进入/离开 DarknessZone2D，或 RunReset/Respawn 刷新重叠
核心判断条件（if/阈值/状态机）：enterDelay/exitDelay + zones.Count
主要调用链（从入口到关键动作，分步骤）：触发器 → PlayerDarknessSensor → SetInDarknessCommand → DarknessModel → DarknessStateChangedEvent → DarknessTickController → DarknessSystem.Tick → ConsumeLightCommand
关键状态与持有者：DarknessModel._isInDarkness、PlayerDarknessSensor 内部计时
关键可配置参数（来自哪里，默认值在哪）：enterDelay/exitDelay、DefaultDrainPerSec（代码常量）
Dependencies（我依赖谁）:

LightVitality：命令调用(a) + 证据 DarknessSystem.cs
SafeZone：事件总线(c) + 证据 DarknessSystem.cs
RunFailReset：事件总线(c) + 证据 PlayerDarknessSensor.cs
DeathRespawn：事件总线(c) + 证据 PlayerDarknessSensor.cs
Reverse Dependencies（谁依赖我）:
HUD：模型读取(d) + 证据 HUDController.cs
Coupling & Risks:

耦合来源：事件总线 + 命令调用；FeatureNote：已检查
潜在bug/时序风险：重叠刷新与延迟状态叠加，可能导致短暂闪断
容易误用的点：未放置 DarknessTickController 则不会扣光；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

放置 DarknessZone2D 与 PlayerDarknessSensor
观察 DarknessStateChangedEvent
运行时看光量是否按秒减少
Key Files（最重要的5-10个文件）:

PlayerDarknessSensor.cs 〞 进入/离开判断
DarknessZone2D.cs 〞 触发体
DarknessTickController.cs 〞 Tick 驱动
DarknessSystem.cs 〞 扣光逻辑
SetInDarknessCommand.cs 〞 状态写入
FeatureNote.md 〞 FeatureNote
[DeathRespawn] DeathRespawn
Location: Assets/Scripts/Features/DeathRespawn（namespace ThatGameJam.Features.DeathRespawn）
Entry Points: GameRootApp.Init 注册 Model/System；DeathController、RespawnController、KillVolume2D
Responsibility: 统一死亡/复活状态与事件

追踪存活状态与死亡次数
触发死亡/复活事件
管理复活位置与重置动作
Public Surface（对外能力）:

Commands: MarkPlayerDeadCommand, MarkPlayerRespawnedCommand, ResetDeathCountCommand
Queries: None
Events: PlayerDiedEvent, PlayerRespawnedEvent, DeathCountChangedEvent
Models/Data: IDeathRespawnModel.IsAlive, IDeathRespawnModel.DeathCount
Unity Components/Prefabs: DeathController, RespawnController, KillVolume2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：LightDepleted/坠落/杀伤触发器
核心判断条件（if/阈值/状态机）：IsAlive、fallYThreshold、respawnDelay
主要调用链（从入口到关键动作，分步骤）：DeathController → DeathRespawnSystem.MarkDead → MarkPlayerDeadCommand → 事件广播 → RespawnController 延迟 → GetCurrentCheckpointQuery → 位置重置 → MarkPlayerRespawnedCommand
关键状态与持有者：DeathRespawnModel._isAlive、_deathCount
关键可配置参数（来自哪里，默认值在哪）：fallYThreshold、respawnDelay（Inspector）
Dependencies（我依赖谁）:

LightVitality：事件总线(c) + 证据 DeathController.cs
Checkpoint：查询调用(a) + 证据 RespawnController.cs
PlayerCharacter2D：命令调用(a) + 证据 RespawnController.cs
RunFailReset：事件总线(c) + 证据 RespawnController.cs
Reverse Dependencies（谁依赖我）:
KeroseneLamp：事件总线(c) + 证据 KeroseneLampManager.cs
PlayerCharacter2D：事件总线(c) + 证据 PlatformerCharacterController.cs
Hazard：接口/DI注入(b) + 证据 HazardSystem.cs
Coupling & Risks:

耦合来源：事件总线 + 命令/查询；FeatureNote：已检查
潜在bug/时序风险：多来源死亡可能在同帧竞争，IsAlive 保护会吞掉后续死亡原因
容易误用的点：未配置 Checkpoint 时复活会使用 respawnPoint 或当前位置；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

用 KillVolume2D 触发死亡
观察 PlayerDiedEvent 与 DeathCountChangedEvent
等待复活并确认位置/速度重置
Key Files（最重要的5-10个文件）:

DeathController.cs 〞 死亡触发
RespawnController.cs 〞 复活流程
DeathRespawnSystem.cs 〞 系统入口
DeathRespawnModel.cs 〞 状态模型
MarkPlayerDeadCommand.cs 〞 死亡命令
FeatureNote.md 〞 FeatureNote
[DoorGate] DoorGate
Location: Assets/Scripts/Features/DoorGate（namespace ThatGameJam.Features.DoorGate）
Entry Points: GameRootApp.Init 注册 Model/System；DoorGateConfig2D、DoorGateSystem
Responsibility: 管理门条件与开关事件

注册 door 配置与阈值
处理花激活计数
广播门状态变化
Public Surface（对外能力）:

Commands: RegisterDoorConfigCommand, UpdateDoorProgressCommand, SetDoorStateCommand, ResetDoorsCommand
Queries: None
Events: DoorStateChangedEvent, DoorOpenEvent
Models/Data: DoorGateState（内部）
Unity Components/Prefabs: DoorGateConfig2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：FlowerActivatedEvent 或 SetDoorStateCommand
核心判断条件（if/阈值/状态机）：ActiveFlowerCount >= RequiredFlowerCount、AllowCloseOnDeactivate
主要调用链（从入口到关键动作，分步骤）：DoorGateConfig2D 注册 → DoorGateSystem 监听花事件 → UpdateDoorProgressCommand → DoorGateModel → DoorStateChangedEvent
关键状态与持有者：DoorGateModel._doors
关键可配置参数（来自哪里，默认值在哪）：requiredFlowerCount、allowCloseOnDeactivate、startOpen（Inspector）
Dependencies（我依赖谁）:

BellFlower：事件总线(c) + 证据 DoorGateSystem.cs
RunFailReset：事件总线(c) + 证据 DoorGateSystem.cs
Reverse Dependencies（谁依赖我）:
Mechanisms：事件总线(c) + 证据 DoorMechanism2D.cs
StoryTasks：命令调用(a) + 证据 StoryTaskTrigger2D.cs
Coupling & Risks:

耦合来源：事件总线 + 命令调用；FeatureNote：已检查
潜在bug/时序风险：花事件早于门注册会被忽略，导致进度丢失
容易误用的点：doorId 为空时会被所有门接收；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

配置 DoorGateConfig2D 与 DoorMechanism2D
触发 FlowerActivatedEvent 至阈值
观察 DoorStateChangedEvent 与门表现
Key Files（最重要的5-10个文件）:

DoorGateConfig2D.cs 〞 门配置注册
DoorGateSystem.cs 〞 花事件处理
DoorGateModel.cs 〞 门状态集合
UpdateDoorProgressCommand.cs 〞 计数更新
DoorStateChangedEvent.cs 〞 状态变化事件
FeatureNote.md 〞 FeatureNote
[FallingRockFromTrashCan] FallingRockFromTrashCan
Location: Assets/Scripts/Features/FallingRockFromTrashCan（namespace ThatGameJam.Features.FallingRockFromTrashCan）
Entry Points: FallingRockFromTrashCanController、FallingRockProjectile
Responsibility: 触发落石生成与伤害

触发器触发生成
对象池复用落石
碰撞时通过 Hazard 系统伤害
Public Surface（对外能力）:

Commands: None
Queries: None
Events: None
Models/Data: None
Unity Components/Prefabs: FallingRockFromTrashCanController, FallingRockProjectile
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：玩家进入触发器
核心判断条件（if/阈值/状态机）：triggerOnEnter、spawnInterval、loopSpawn
主要调用链（从入口到关键动作，分步骤）：FallingRockFromTrashCanController → Pool.Get → FallingRockProjectile → 碰撞 → IHazardSystem.ApplyInstantKill
关键状态与持有者：_pool、_spawnRoutine
关键可配置参数（来自哪里，默认值在哪）：spawnInterval、spawnCount、preloadCount（Inspector）
Dependencies（我依赖谁）:

Hazard：接口/DI注入(b) + 证据 FallingRockProjectile.cs
Reverse Dependencies（谁依赖我）:
None
Coupling & Risks:

耦合来源：系统调用；FeatureNote：已检查
潜在bug/时序风险：rockPrefab 缺失时只记录日志，不会生成
容易误用的点：Collider2D 需为 Trigger，否则无触发；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

场景放 FallingRockFromTrashCanController
触发玩家进入并观察生成
碰撞玩家确认死亡触发
Key Files（最重要的5-10个文件）:

FallingRockFromTrashCanController.cs 〞 生成调度
FallingRockProjectile.cs 〞 碰撞伤害
SimpleGameObjectPool.cs 〞 对象池
FeatureNote.md 〞 FeatureNote
HazardSystem.cs 〞 伤害依赖
[Hazard] Hazard
Location: Assets/Scripts/Features/Hazard（namespace ThatGameJam.Features.Hazard）
Entry Points: GameRootApp.Init 注册 System；DamageVolume2D、HazardVolume2D
Responsibility: 统一伤害入口

即时死亡
按比例扣光/持续扣光
将伤害统一转到 Light/Death 系统
Public Surface（对外能力）:

Commands: ConsumeLightCommand（由系统发送）
Queries: None
Events: None
Models/Data: None
Unity Components/Prefabs: DamageVolume2D, HazardVolume2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：进入或停留在危险触发体
核心判断条件（if/阈值/状态机）：HazardMode、cooldownSeconds、drainRatioPerSecond
主要调用链（从入口到关键动作，分步骤）：触发器 → IHazardSystem → ConsumeLightCommand/MarkDead
关键状态与持有者：HazardSystem 内部无持久状态
关键可配置参数（来自哪里，默认值在哪）：costRatio、drainRatioPerSecond、deathReason
Dependencies（我依赖谁）:

LightVitality：命令调用(a) + 证据 HazardSystem.cs
DeathRespawn：接口/DI注入(b) + 证据 HazardSystem.cs
Reverse Dependencies（谁依赖我）:
FallingRockFromTrashCan：接口/DI注入(b) + 证据 FallingRockProjectile.cs
Coupling & Risks:

耦合来源：系统调用；FeatureNote：已检查
潜在bug/时序风险：MaxLight 为 0 时扣光比例失效（伤害变为 0）
容易误用的点：playerTag 配置错误会导致不触发伤害；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

放置 DamageVolume2D 并进入触发
放置 HazardVolume2D 为 InstantKill 模式
观察 Light/Death 事件是否正确触发
Key Files（最重要的5-10个文件）:

HazardSystem.cs 〞 统一伤害入口
IHazardSystem.cs 〞 接口
DamageVolume2D.cs 〞 扣光触发
HazardVolume2D.cs 〞 即死/持续扣光
FeatureNote.md 〞 FeatureNote
[HUD] HUD
Location: Assets/Scripts/Features/HUD（namespace ThatGameJam.Features.HUD）
Entry Points: HUDController
Responsibility: 展示核心 HUD 信息

读取光量与最大值
展示黑暗/安全区状态
展示灯数量
Public Surface（对外能力）:

Commands: None
Queries: None
Events: None
Models/Data: ILightVitalityModel, IDarknessModel, ISafeZoneModel, IKeroseneLampModel
Unity Components/Prefabs: HUDController
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：模型 Bindable 更新
核心判断条件（if/阈值/状态机）：UI 引用非空
主要调用链（从入口到关键动作，分步骤）：HUDController.OnEnable 注册回调 → 更新 Text/Image
关键状态与持有者：HUDController 内部缓存 _currentLight/_maxLight/_lampCount
关键可配置参数（来自哪里，默认值在哪）：UI 引用（Inspector）
Dependencies（我依赖谁）:

LightVitality：模型读取(d) + 证据 HUDController.cs
Darkness：模型读取(d) + 证据 HUDController.cs
SafeZone：模型读取(d) + 证据 HUDController.cs
KeroseneLamp：模型读取(d) + 证据 HUDController.cs
Reverse Dependencies（谁依赖我）:
None
Coupling & Risks:

耦合来源：模型读取；FeatureNote：已检查
潜在bug/时序风险：UI 引用未绑定会导致显示为空
容易误用的点：Text/Image 为 legacy UI，需要确认 Canvas 设置；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

绑定 HUD 引用并运行
触发光量变化与安全区切换
验证文本与填充实时更新
Key Files（最重要的5-10个文件）:

HUDController.cs 〞 UI 绑定逻辑
FeatureNote.md 〞 FeatureNote
LightVitalityModel.cs 〞 光量模型
DarknessModel.cs 〞 黑暗模型
SafeZoneModel.cs 〞 安全区模型
KeroseneLampModel.cs 〞 灯数量模型
[IceBlock] IceBlock
Location: Assets/Scripts/Features/IceBlock（namespace ThatGameJam.Features.IceBlock）
Entry Points: IceBlock2D
Responsibility: 以消融/凝结机制提供可通行的临时障碍

触发后扣光并渐变消融
临时关闭碰撞允许通行
凝结恢复时检查挤压死亡
Public Surface（对外能力）:

Commands: ConsumeLightCommand, MarkPlayerDeadCommand
Queries: GetMaxLightQuery
Events: None
Models/Data: None
Unity Components/Prefabs: IceBlock2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：玩家进入触发体
核心判断条件（if/阈值/状态机）：transitionDuration、waitDuration、lightCostRatio
主要调用链（从入口到关键动作，分步骤）：OnTriggerEnter2D → MeltSequence → ConsumeLightCommand → Fade → 关闭碰撞 → 等待 → 恢复碰撞 → Overlap → MarkPlayerDeadCommand
关键状态与持有者：_isProcessing、_originalColor
关键可配置参数（来自哪里，默认值在哪）：meltedColor、transitionDuration、waitDuration、lightCostRatio
Dependencies（我依赖谁）:

LightVitality：命令/查询(a) + 证据 IceBlock2D.cs
DeathRespawn：命令调用(a) + 证据 IceBlock2D.cs
RunFailReset：事件总线(c) + 证据 IceBlock2D.cs
Reverse Dependencies（谁依赖我）:
None
Coupling & Risks:

耦合来源：命令调用 + 事件总线；FeatureNote：需更新（文档写 EDeathReason.Environment，代码使用 EDeathReason.Script）
潜在bug/时序风险：StopAllCoroutines 在 Reset 中会中断外部协程（若挂在同对象）
容易误用的点：solidCollider 未指定会导致挤压检测失效；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

触发冰块并观察消融/凝结
等待凝结完成时站在冰块中
观察是否触发死亡
Key Files（最重要的5-10个文件）:

IceBlock2D.cs 〞 核心逻辑
FeatureNote.md 〞 FeatureNote
GetMaxLightQuery.cs 〞 取最大光量
ConsumeLightCommand.cs 〞 扣光
MarkPlayerDeadCommand.cs 〞 触发死亡
[KeroseneLamp] KeroseneLamp
Location: Assets/Scripts/Features/KeroseneLamp（namespace ThatGameJam.Features.KeroseneLamp）
Entry Points: GameRootApp.Init 注册 Model；KeroseneLampManager、KeroseneLampInstance
Responsibility: 管理灯注册、区域可见性与最大有效数量

生成灯并注册到模型
控制视觉/玩法启用状态
区域切换时仅关闭视觉
Public Surface（对外能力）:

Commands: RecordLampSpawnedCommand, SetLampGameplayStateCommand, SetLampVisualStateCommand, ResetLampsCommand
Queries: GetGameplayEnabledLampsQuery, GetAllLampInfosQuery
Events: LampSpawnedEvent, LampCountChangedEvent, LampGameplayStateChangedEvent, LampVisualStateChangedEvent, RequestSpawnLampEvent
Models/Data: IKeroseneLampModel.LampCount, LampInfo
Unity Components/Prefabs: KeroseneLampManager, KeroseneLampInstance
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：PlayerDiedEvent、RequestSpawnLampEvent、AreaChangedEvent
核心判断条件（if/阈值/状态机）：maxActivePerArea、AreaId 匹配
主要调用链（从入口到关键动作，分步骤）：死亡/请求 → SpawnLamp → RecordLampSpawnedCommand → 事件 → KeroseneLampInstance 更新 → 区域切换 → SetLampVisualStateCommand
关键状态与持有者：KeroseneLampModel._lamps、_areaActiveCounts、_areaOrder
关键可配置参数（来自哪里，默认值在哪）：lampPrefab、maxActivePerArea（Inspector）
Dependencies（我依赖谁）:

AreaSystem：查询/事件(a,c) + 证据 KeroseneLampManager.cs
DeathRespawn：事件总线(c) + 证据 KeroseneLampManager.cs
RunFailReset：事件总线(c) + 证据 KeroseneLampManager.cs
Reverse Dependencies（谁依赖我）:
BellFlower：查询调用(a) + 证据 BellFlower2D.cs
BugAI：查询调用(a) + 证据 BugAttractLamp.cs
Mechanisms：查询调用(a) + 证据 VineMechanism2D.cs
Coupling & Risks:

耦合来源：事件总线 + 命令/查询；FeatureNote：已检查
潜在bug/时序风险：区域切换时依赖灯列表快照，若同帧新增灯可能出现短暂视觉错配
容易误用的点：fallbackAreaId 会把未定义区域灯聚合到同一区域；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

让玩家死亡生成灯
切换区域观察灯视觉开关
同一区域生成超过 maxActivePerArea 的灯，观察旧灯被禁用
Key Files（最重要的5-10个文件）:

KeroseneLampManager.cs 〞 生成与管理
KeroseneLampInstance.cs 〞 视觉/玩法切换
KeroseneLampModel.cs 〞 状态模型
LampInfo.cs 〞 灯快照
RecordLampSpawnedCommand.cs 〞 注册命令
FeatureNote.md 〞 FeatureNote
[LightVitality] LightVitality
Location: Assets/Scripts/Features/LightVitality（namespace ThatGameJam.Features.LightVitality）
Entry Points: GameRootApp.Init 注册 Model/System；LightVitalityResetSystem；可选调试控制器
Responsibility: 管理光量资源并广播变化

维护 Current/Max 光量
处理加/减/设置光量命令
触发 LightChanged/Depleted 事件
Public Surface（对外能力）:

Commands: AddLightCommand, ConsumeLightCommand, SetLightCommand, SetMaxLightCommand
Queries: GetCurrentLightQuery, GetMaxLightQuery, GetLightPercentQuery
Events: LightChangedEvent, LightConsumedEvent, LightDepletedEvent
Models/Data: ILightVitalityModel.CurrentLight, ILightVitalityModel.MaxLight
Unity Components/Prefabs: LightVitalityDebugController, LightVitalityResetController（可选）
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：外部 Feature 发送 Add/Consume/Set 命令
核心判断条件（if/阈值/状态机）：clamp 到 [0, Max]，previous > 0 && current <= 0
主要调用链（从入口到关键动作，分步骤）：命令 → LightVitalityCommandUtils.ApplyCurrentLight → 更新模型 → 事件广播
关键状态与持有者：LightVitalityModel._currentLight/_maxLight
关键可配置参数（来自哪里，默认值在哪）：DefaultMaxLight、DefaultInitialLight（代码常量）
Dependencies（我依赖谁）:

RunFailReset：事件总线(c) + 证据 LightVitalityResetSystem.cs
DeathRespawn：事件总线(c) + 证据 LightVitalityResetSystem.cs
Reverse Dependencies（谁依赖我）:
Darkness：命令调用(a) + 证据 DarknessSystem.cs
SafeZone：命令调用(a) + 证据 SafeZoneSystem.cs
Hazard：命令调用(a) + 证据 HazardSystem.cs
Coupling & Risks:

耦合来源：命令调用 + 事件总线；FeatureNote：已检查
潜在bug/时序风险：多命令同帧执行时事件触发顺序依赖 QFramework 调度
容易误用的点：SetMaxLightCommand 可裁剪当前值，导致瞬间光量跳变；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

使用 LightVitalityDebugController 按键加/减光
将光量降至 0 观察 LightDepletedEvent
触发 RunResetEvent 看光量是否回满
Key Files（最重要的5-10个文件）:

LightVitalityModel.cs 〞 光量模型
ConsumeLightCommand.cs 〞 扣光命令
LightVitalityCommandUtils.cs 〞 更新与事件
LightVitalityResetSystem.cs 〞 重置系统
LightVitalityDebugController.cs 〞 调试控制器
FeatureNote.md 〞 FeatureNote
[Mechanisms] Mechanisms
Location: Assets/Scripts/Features/Mechanisms（namespace ThatGameJam.Features.Mechanisms）
Entry Points: MechanismControllerBase、VineMechanism2D、GhostMechanism2D、SpikeHazard2D、DoorMechanism2D
Responsibility: 提供机关行为集合与统一复位/区域钩子

统一处理 RunReset 与 AreaEnter/Exit 钩子
藤蔓生长、幽灵巡逻、尖刺伤害、门表现
基于场景配置驱动
Public Surface（对外能力）:

Commands: ConsumeLightCommand（Ghost），MarkDead（Spike）
Queries: GetGameplayEnabledLampsQuery（Vine）
Events: 监听 DoorStateChangedEvent、AreaChangedEvent、RunResetEvent
Models/Data: None
Unity Components/Prefabs: MechanismControllerBase, VineMechanism2D, GhostMechanism2D, SpikeHazard2D, DoorMechanism2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：区域切换、RunReset、灯光/触发器接触
核心判断条件（if/阈值/状态机）：藤蔓 lightRequiredSeconds；幽灵 extraConsumePerSecond；门 doorId 匹配
主要调用链（从入口到关键动作，分步骤）：事件 → MechanismControllerBase → 派生类逻辑（Vine/Ghost/Spike/Door）
关键状态与持有者：各 MonoBehaviour 内部状态（如 Vine 的 growthValue）
关键可配置参数（来自哪里，默认值在哪）：shouldResetOnRunReset、藤蔓/幽灵/门参数（Inspector）
Dependencies（我依赖谁）:

AreaSystem：事件总线(c) + 证据 MechanismControllerBase.cs
RunFailReset：事件总线(c) + 证据 MechanismControllerBase.cs
KeroseneLamp：查询调用(a) + 证据 VineMechanism2D.cs
LightVitality：命令调用(a) + 证据 GhostMechanism2D.cs
DeathRespawn：接口/DI注入(b) + 证据 SpikeHazard2D.cs
DoorGate：事件总线(c) + 证据 DoorMechanism2D.cs
Reverse Dependencies（谁依赖我）:
None
Coupling & Risks:

耦合来源：事件总线 + 多 Feature 命令/查询；FeatureNote：已检查
潜在bug/时序风险：DOTween 依赖外部插件，缺失会导致藤蔓动画异常
容易误用的点：shouldResetOnRunReset 关闭后机关状态会跨死亡保留；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

放置藤蔓与灯，观察计时生长
放置幽灵并进入触发范围，看扣光
触发 RunReset 看机关是否按开关重置
Key Files（最重要的5-10个文件）:

MechanismControllerBase.cs 〞 统一钩子
VineMechanism2D.cs 〞 藤蔓生长
GhostMechanism2D.cs 〞 幽灵巡逻/扣光
SpikeHazard2D.cs 〞 尖刺伤害
DoorMechanism2D.cs 〞 门表现
FeatureNote.md 〞 FeatureNote
[PlayerCharacter2D] PlayerCharacter2D
Location: Assets/Scripts/Features/PlayerCharacter2D（namespace ThatGameJam.Features.PlayerCharacter2D）
Entry Points: GameRootApp.Init 注册 Model/System；PlatformerCharacterController、PlatformerCharacterInput
Responsibility: 2D 平台角色移动与攀爬

输入读取并写入模型
FixedUpdate 驱动状态与速度
发送跳跃/落地/攀爬事件
Public Surface（对外能力）:

Commands: SetFrameInputCommand, ResetClimbStateCommand, TickFixedStepCommand
Queries: GetDesiredVelocityQuery
Events: PlayerGroundedChangedEvent, PlayerClimbStateChangedEvent, PlayerJumpedEvent
Models/Data: IPlayerCharacter2DModel（Grounded/IsClimbing/Velocity 等）
Unity Components/Prefabs: PlatformerCharacterController, PlatformerCharacterInput
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：输入系统 + 物理检测
核心判断条件（if/阈值/状态机）：CoyoteTime、JumpBuffer、ClimbRegrabLockout
主要调用链（从入口到关键动作，分步骤）：PlatformerCharacterInput → SetFrameInputCommand → TickFixedStepCommand → GetDesiredVelocityQuery → Rigidbody2D
关键状态与持有者：PlayerCharacter2DModel 内部状态
关键可配置参数（来自哪里，默认值在哪）：PlatformerCharacterStats ScriptableObject
Dependencies（我依赖谁）:

DeathRespawn：事件总线(c) + 证据 PlatformerCharacterController.cs
Reverse Dependencies（谁依赖我）:
DeathRespawn：命令调用(a) + 证据 RespawnController.cs
Coupling & Risks:

耦合来源：事件总线 + 输入系统；FeatureNote：已检查
潜在bug/时序风险：_stats 未配置会导致 Update/FixedUpdate 提前返回
容易误用的点：_climbSensor 缺失导致无法攀爬；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

挂 PlatformerCharacterController + PlatformerCharacterInput
配置 PlatformerCharacterStats 并测试跳跃/攀爬
触发死亡事件，确认输入锁定/恢复
Key Files（最重要的5-10个文件）:

PlatformerCharacterController.cs 〞 运动驱动
PlatformerCharacterInput.cs 〞 输入读取
PlayerCharacter2DModel.cs 〞 状态模型
TickFixedStepCommand.cs 〞 物理步进
PlatformerCharacterStats.cs 〞 参数配置
FeatureNote.md 〞 FeatureNote
[RunFailHandling] RunFailHandling
Location: Assets/Scripts/Features/RunFailHandling（namespace ThatGameJam.Features.RunFailHandling）
Entry Points: RunFailHandlingController
Responsibility: 已废弃测试入口占位

提供测试 HardReset 入口
不监听失败事件
仅供临时调试
Public Surface（对外能力）:

Commands: None
Queries: None
Events: None
Models/Data: None
Unity Components/Prefabs: RunFailHandlingController
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：手动调用 RequestHardResetFromTest
核心判断条件（if/阈值/状态机）：无
主要调用链（从入口到关键动作，分步骤）：RunFailHandlingController → IRunFailResetSystem.RequestResetFromTest
关键状态与持有者：无
关键可配置参数（来自哪里，默认值在哪）：无
Dependencies（我依赖谁）:

RunFailReset：接口/DI注入(b) + 证据 RunFailHandlingController.cs
Reverse Dependencies（谁依赖我）:
None
Coupling & Risks:

耦合来源：系统调用；FeatureNote：已检查
潜在bug/时序风险：如果系统未注册将抛出运行时异常
容易误用的点：不应保留在正式关卡中；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

调用 RequestHardResetFromTest()
确认 RunResetEvent 被触发
观察各 Feature 是否重置
Key Files（最重要的5-10个文件）:

RunFailHandlingController.cs 〞 测试入口
FeatureNote.md 〞 FeatureNote
RunFailResetSystem.cs 〞 依赖系统
RunResetEvent.cs 〞 重置事件
RunResetController.cs 〞 另一测试入口
[RunFailReset] RunFailReset
Location: Assets/Scripts/Features/RunFailReset（namespace ThatGameJam.Features.RunFailReset）
Entry Points: GameRootApp.Init 注册 System；RunFailResetSystem
Responsibility: 提供统一 HardReset 事件入口

对外暴露 HardReset 请求
发送 RunResetEvent
不处理失败统计
Public Surface（对外能力）:

Commands: None
Queries: None
Events: RunResetEvent
Models/Data: None
Unity Components/Prefabs: RunFailResetSystem（通过系统访问）
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：测试入口调用 RequestResetFromTest
核心判断条件（if/阈值/状态机）：无
主要调用链（从入口到关键动作，分步骤）：RunFailResetSystem → RunResetEvent
关键状态与持有者：无
关键可配置参数（来自哪里，默认值在哪）：无
Dependencies（我依赖谁）:

None
Reverse Dependencies（谁依赖我）:
AreaSystem：事件总线(c) + 证据 PlayerAreaSensor.cs
LightVitality：事件总线(c) + 证据 LightVitalityResetSystem.cs
KeroseneLamp：事件总线(c) + 证据 KeroseneLampManager.cs
Coupling & Risks:

耦合来源：事件总线；FeatureNote：已检查
潜在bug/时序风险：事件不持久，禁用对象不会收到重置
容易误用的点：仅测试入口触发，生产环境需确认调用路径；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

用 RunResetController 按键触发
观察 RunResetEvent 监听者是否执行
验证灯/机关/区域状态重置
Key Files（最重要的5-10个文件）:

RunFailResetSystem.cs 〞 事件触发
IRunFailResetSystem.cs 〞 接口
RunResetEvent.cs 〞 事件结构体
GameRootApp.cs 〞 系统注册
RunResetController.cs 〞 测试入口
FeatureNote.md 〞 FeatureNote
[SafeZone] SafeZone
Location: Assets/Scripts/Features/SafeZone（namespace ThatGameJam.Features.SafeZone）
Entry Points: GameRootApp.Init 注册 Model/System；PlayerSafeZoneSensor、SafeZone2D、SafeZoneTickController
Responsibility: 维护安全区状态并在安全区内回光

统计重叠安全区数量
更新 IsSafe 状态
Tick 回光
Public Surface（对外能力）:

Commands: SetSafeZoneCountCommand
Queries: None
Events: SafeZoneStateChangedEvent
Models/Data: ISafeZoneModel.SafeZoneCount, ISafeZoneModel.IsSafe
Unity Components/Prefabs: PlayerSafeZoneSensor, SafeZone2D, SafeZoneTickController
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：进入/离开安全区触发体或 RunReset/Respawn 刷新
核心判断条件（if/阈值/状态机）：_zones.Count > 0
主要调用链（从入口到关键动作，分步骤）：触发器 → PlayerSafeZoneSensor → SetSafeZoneCountCommand → SafeZoneModel → SafeZoneStateChangedEvent → SafeZoneSystem.Tick → AddLightCommand
关键状态与持有者：SafeZoneModel._safeZoneCount/_isSafe
关键可配置参数（来自哪里，默认值在哪）：DefaultRegenPerSec（代码常量）
Dependencies（我依赖谁）:

LightVitality：命令调用(a) + 证据 SafeZoneSystem.cs
RunFailReset：事件总线(c) + 证据 PlayerSafeZoneSensor.cs
DeathRespawn：事件总线(c) + 证据 PlayerSafeZoneSensor.cs
Reverse Dependencies（谁依赖我）:
Darkness：事件总线(c) + 证据 DarknessSystem.cs
HUD：模型读取(d) + 证据 HUDController.cs
Coupling & Risks:

耦合来源：事件总线 + 命令调用；FeatureNote：已检查
潜在bug/时序风险：出生点在安全区内时依赖重叠刷新逻辑才能立即生效
容易误用的点：未放 SafeZoneTickController 不会回光；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

放置 SafeZone2D 与 PlayerSafeZoneSensor
进入/离开并观察 SafeZoneStateChangedEvent
验证光量是否在安全区回升
Key Files（最重要的5-10个文件）:

PlayerSafeZoneSensor.cs 〞 重叠统计
SafeZone2D.cs 〞 触发体
SafeZoneTickController.cs 〞 Tick 驱动
SafeZoneSystem.cs 〞 回光逻辑
SafeZoneModel.cs 〞 模型
FeatureNote.md 〞 FeatureNote
[StoryTasks] StoryTasks
Location: Assets/Scripts/Features/StoryTasks（namespace ThatGameJam.Features.StoryTasks）
Entry Points: GameRootApp.Init 注册 Model；StoryTaskTrigger2D
Responsibility: 触发器驱动剧情动作序列

条件触发（flag gating）
执行动作（对话/灯/门/标记）
维护一次性剧情标记
Public Surface（对外能力）:

Commands: SetFlagCommand, SetDoorStateCommand（外部）
Queries: HasFlagQuery
Events: DialogueRequestedEvent, StoryFlagChangedEvent, RequestSpawnLampEvent（外部）
Models/Data: StoryFlagsModel
Unity Components/Prefabs: StoryTaskTrigger2D
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：玩家进入触发器
核心判断条件（if/阈值/状态机）：triggerOnce、HasFlagQuery
主要调用链（从入口到关键动作，分步骤）：触发器 → ExecuteActions → DialogueRequestedEvent/RequestSpawnLampEvent/SetDoorStateCommand → SetFlagCommand
关键状态与持有者：StoryFlagsModel._flags
关键可配置参数（来自哪里，默认值在哪）：动作列表与 flagId（Inspector）
Dependencies（我依赖谁）:

DoorGate：命令调用(a) + 证据 StoryTaskTrigger2D.cs
KeroseneLamp：事件总线(c) + 证据 StoryTaskTrigger2D.cs
Reverse Dependencies（谁依赖我）:
None（DialogueRequestedEvent 期望外部插件订阅）
Coupling & Risks:

耦合来源：命令调用 + 事件请求；FeatureNote：已检查
潜在bug/时序风险：对话系统未实现会导致 DialogueRequestedEvent 无响应
容易误用的点：RequireFlag 动作失败会中断后续动作执行；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

配置 StoryTaskTrigger2D 动作为 PlayDialogue/SetFlag
触发并观察 DialogueRequestedEvent
再次触发确认一次性 flag 生效
Key Files（最重要的5-10个文件）:

StoryTaskTrigger2D.cs 〞 动作序列
StoryFlagsModel.cs 〞 标记模型
SetFlagCommand.cs 〞 写入标记
HasFlagQuery.cs 〞 读取标记
DialogueRequestedEvent.cs 〞 对话请求
FeatureNote.md 〞 FeatureNote
[Testing] Testing
Location: Assets/Scripts/Features/Testing（namespace ThatGameJam.Features.RunFailReset.Controllers）
Entry Points: RunResetController
Responsibility: 仅用于开发环境触发 HardReset

键盘触发重置
调用 RunFailReset System
无运行时状态
Public Surface（对外能力）:

Commands: None
Queries: None
Events: None
Models/Data: None
Unity Components/Prefabs: RunResetController
Runtime Flow（运行逻辑，必须可问答）:

玩家/外部触发条件：按下 resetKey（仅编辑器/开发构建）
核心判断条件（if/阈值/状态机）：UNITY_EDITOR || DEVELOPMENT_BUILD
主要调用链（从入口到关键动作，分步骤）：RunResetController.Update → IRunFailResetSystem.RequestResetFromTest → RunResetEvent
关键状态与持有者：无
关键可配置参数（来自哪里，默认值在哪）：resetKey（Inspector）
Dependencies（我依赖谁）:

RunFailReset：接口/DI注入(b) + 证据 RunResetController.cs
Reverse Dependencies（谁依赖我）:
None
Coupling & Risks:

耦合来源：系统调用；FeatureNote：需更新（命名空间与目录不一致）
潜在bug/时序风险：非开发构建下 Update 不触发
容易误用的点：不要在正式场景保留此组件；CQRS/MVC：未见明显违背
How to Test Quickly（最小验证步骤）:

在 Editor/DevBuild 挂 RunResetController
按下 resetKey
确认 RunResetEvent 被触发
Key Files（最重要的5-10个文件）:

RunResetController.cs 〞 测试入口
FeatureNote.md 〞 FeatureNote
RunFailResetSystem.cs 〞 依赖系统
RunResetEvent.cs 〞 重置事件
RunFailHandlingController.cs 〞 相关测试入口
4) Searchable Glossary（可检索词典）
Light: LightVitality, Darkness, SafeZone, Hazard, IceBlock, Mechanisms(Ghost/Vine), HUD, KeroseneLamp, BellFlower
Lamp: KeroseneLamp, BellFlower, BugAI, StoryTasks, HUD
Vine/Plant: Mechanisms(VineMechanism2D), BellFlower
Mechanism: Mechanisms, DoorGate, Hazard
PlayerController: PlayerCharacter2D, DeathRespawn, Checkpoint, AreaSystem
Death: DeathRespawn, Hazard, Mechanisms(Spike/Ghost), IceBlock, FallingRockFromTrashCan
Area: AreaSystem, KeroseneLamp, Mechanisms
Door/Gate: DoorGate, Mechanisms(DoorMechanism2D), StoryTasks
Save/Load: None（扫描范围内未发现持久化功能）
UI: HUD
Story/Dialogue: StoryTasks
Reset/HardReset: RunFailReset, Testing, RunFailHandling
Bug/AI: BugAI
Checkpoint/Respawn: Checkpoint, DeathRespawn