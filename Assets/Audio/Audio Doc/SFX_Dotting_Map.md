# SFX_Dotting_Map

> 仅做分析与汇报，不含实现。

## 1. 概览

- 扫描范围：
  - `Assets/Scripts/Root`（入口/全局注册）
  - `Assets/Scripts/Features/*`（核心玩法系统）
  - `Assets/Scripts/SaveSystem`（存档相关 UI 入口）
  - `Assets/Scenes`（正式关卡、Test Scene、Spine Test；场景内部未逐物体展开）
- 候选点位总数：41
- 按系统统计：Player 5 / Enemy 6 / Interactable&Puzzle 14 / Environment&Ambient 11 / UI 3 / Narrative&Meta 2
- 高优先级 Top 20：
  1. SFX-PLR-0001 起跳
  2. SFX-PLR-0002 落地
  3. SFX-PLR-0004 死亡
  4. SFX-PLR-0005 复活
  5. SFX-INT-0002 门开关
  6. SFX-INT-0001 风铃花激活/熄灭
  7. SFX-INT-0005 冰块消融循环
  8. SFX-INT-0006 冰块消融完成/凝结
  9. SFX-ENV-0006 危险体积秒杀
 10. SFX-ENV-0009 尖刺击杀
 11. SFX-ENV-0010 落石命中
 12. SFX-ENV-0011 掉落死亡体积
 13. SFX-ENM-0006 幽灵接触吸光
 14. SFX-ENV-0001 进入/离开黑暗
 15. SFX-ENV-0002 黑暗扣光循环
 16. SFX-ENV-0003 进入/离开安全区
 17. SFX-INT-0011 存档点触发
 18. SFX-INT-0014 垃圾桶落石事件
 19. SFX-ENM-0003 虫子抓取/放手
 20. SFX-INT-0007 灯生成/点亮

## 2. 音效语义标签表（SFX Tags）

| Tag | 说明/典型触发 |
| --- | --- |
| `SFX_Player_Jump` | 玩家起跳（`PlayerJumpedEvent`） |
| `SFX_Player_Land` | 玩家落地（`PlayerGroundedChangedEvent` Grounded=true） |
| `SFX_Player_Climb_Start` | 开始攀爬（`PlayerClimbStateChangedEvent` true） |
| `SFX_Player_Climb_Loop` | 攀爬持续摩擦/抓握循环 |
| `SFX_Player_Climb_End` | 结束攀爬（`PlayerClimbStateChangedEvent` false） |
| `SFX_Player_Death` | 死亡（`PlayerDiedEvent`） |
| `SFX_Player_Respawn` | 复活（`PlayerRespawnedEvent`） |
| `SFX_Light_Depleted` | 光量归零（`LightDepletedEvent`） |
| `SFX_Bug_Chase_Start` | 虫子追光开始（`BugState.ChaseLight`） |
| `SFX_Bug_Return` | 虫子退回巢穴/冷却 |
| `SFX_Bug_Grab` | 抓虫开始 |
| `SFX_Bug_Release` | 抓虫结束 |
| `SFX_Bug_Stomp` | 踩踏虫子反弹 |
| `SFX_Ghost_Loop` | 幽灵漂浮循环 |
| `SFX_Ghost_Drain_Loop` | 幽灵接触吸光循环 |
| `SFX_Flower_Activate` | 风铃花被点亮/激活 |
| `SFX_Flower_Deactivate` | 风铃花熄灭 |
| `SFX_Door_Open` | 门开启 |
| `SFX_Door_Close` | 门关闭 |
| `SFX_Door_Move_Loop` | 门运动过程循环 |
| `SFX_Vine_Grow` | 藤蔓生长 |
| `SFX_Vine_Shrink` | 藤蔓回缩 |
| `SFX_Ice_Melt_Loop` | 冰块消融循环 |
| `SFX_Ice_Melt_Complete` | 冰块完全融化/破开 |
| `SFX_Ice_Refreeze` | 冰块凝结回位 |
| `SFX_Lamp_Spawn` | 灯生成/点亮 |
| `SFX_Lamp_Pickup` | 灯被拾起/抱持 |
| `SFX_Lamp_Drop` | 灯落地/放下 |
| `SFX_Lamp_Extinguish` | 灯玩法失效/熄灭 |
| `SFX_Lamp_Visual_Toggle` | 灯视觉显隐切换 |
| `SFX_Checkpoint_Activate` | 存档点触发 |
| `SFX_Rockfall_Start_Loop` | 落石事件开始循环 |
| `SFX_Rockfall_End` | 落石事件结束 |
| `SFX_Rock_Hit` | 落石命中地面/玩家 |
| `SFX_Darkness_Enter` | 进入黑暗 |
| `SFX_Darkness_Exit` | 离开黑暗 |
| `SFX_Darkness_Drain_Loop` | 黑暗扣光循环 |
| `SFX_SafeZone_Enter` | 进入安全区 |
| `SFX_SafeZone_Exit` | 离开安全区 |
| `SFX_SafeZone_Regen_Loop` | 安全区回血/回光循环 |
| `SFX_Area_Change` | 区域切换提示 |
| `SFX_Hazard_InstantKill` | 危险体积秒杀 |
| `SFX_Hazard_Drain_Loop` | 危险体积持续扣光 |
| `SFX_Damage_Tick` | 伤害体积周期扣光 |
| `SFX_Spike_Hit` | 尖刺命中/死亡 |
| `SFX_KillVolume_Death` | 掉落死亡体积 |
| `SFX_Dialogue_Start` | 对话开始提示 |
| `SFX_UI_Save_Click` | 保存按钮点击 |
| `SFX_UI_Load_Click` | 读取按钮点击 |
| `SFX_UI_Delete_Click` | 删除按钮点击 |

## 3. 分系统点位清单

### Player

<a id="SFX-PLR-0001"></a>
### SFX-PLR-0001 起跳
| 字段 | 内容 |
| --- | --- |
| ID | SFX-PLR-0001 |
| 系统/对象 | Player / TickFixedStepCommand |
| 玩家感知 | 起跳瞬间（含攀爬跳） |
| 建议音效 | 轻快起跳、布料/鞋底离地 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 执行跳跃时发送 `PlayerJumpedEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.05s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs`<br>类/方法：`TickFixedStepCommand.OnExecute()`<br>行号范围：`L195-L212`、`L320-L329` |
| 代码证据（必须） | `this.SendEvent<PlayerJumpedEvent>();` |
| 置信度 | High（显式事件） |
| 优先级 | P0 |
| 备注 | 攀爬跳与普通跳可共用或做轻微变体 |

<a id="SFX-PLR-0002"></a>
### SFX-PLR-0002 落地
| 字段 | 内容 |
| --- | --- |
| ID | SFX-PLR-0002 |
| 系统/对象 | Player / TickFixedStepCommand |
| 玩家感知 | 落地冲击（可带力度差异） |
| 建议音效 | 落地闷响/脚步落地，支持力度变化 |
| 触发类型 | OneShot + Param Driven |
| 触发条件（来自代码） | 从空中转为地面时发送 `PlayerGroundedChangedEvent(Grounded=true, ImpactSpeed)` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.1s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs`<br>类/方法：`TickFixedStepCommand.OnExecute()`<br>行号范围：`L73-L81` |
| 代码证据（必须） | `this.SendEvent(new PlayerGroundedChangedEvent { Grounded = true, ImpactSpeed = impact });` |
| 置信度 | High（显式事件 + 速度参数） |
| 优先级 | P0 |
| 备注 | 建议用 `ImpactSpeed` 驱动音量/滤波 |

<a id="SFX-PLR-0003"></a>
### SFX-PLR-0003 攀爬开始/结束
| 字段 | 内容 |
| --- | --- |
| ID | SFX-PLR-0003 |
| 系统/对象 | Player / TickFixedStepCommand |
| 玩家感知 | 抓附墙面进入攀爬、或放手退出 |
| 建议音效 | 抓握“哗/擦” + 持续轻摩擦循环 |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | 攀爬状态变化时发送 `PlayerClimbStateChangedEvent(IsClimbing)` |
| 停止条件（Loop 必填） | `IsClimbing=false` 时停止循环 |
| 推荐防抖/冷却 | 0.1s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs`<br>类/方法：`TickFixedStepCommand.OnExecute()`<br>行号范围：`L283-L294` |
| 代码证据（必须） | `this.SendEvent(new PlayerClimbStateChangedEvent { IsClimbing = model.IsClimbing.Value });` |
| 置信度 | High（显式事件） |
| 优先级 | P1 |
| 备注 | 可按墙面材质/水平攀爬切换变体 |

<a id="SFX-PLR-0004"></a>
### SFX-PLR-0004 死亡
| 字段 | 内容 |
| --- | --- |
| ID | SFX-PLR-0004 |
| 系统/对象 | Player / MarkPlayerDeadCommand |
| 玩家感知 | 玩家死亡、失败反馈 |
| 建议音效 | 短促死亡音或失衡下坠音效 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 死亡标记时发送 `PlayerDiedEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/DeathRespawn/Commands/MarkPlayerDeadCommand.cs`<br>类/方法：`MarkPlayerDeadCommand.OnExecute()`<br>行号范围：`L27-L34` |
| 代码证据（必须） | `this.SendEvent(new PlayerDiedEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P0 |
| 备注 | 可根据 `Reason` 区分跌落/光耗尽/机关击杀 |

<a id="SFX-PLR-0005"></a>
### SFX-PLR-0005 复活
| 字段 | 内容 |
| --- | --- |
| ID | SFX-PLR-0005 |
| 系统/对象 | Player / MarkPlayerRespawnedCommand |
| 玩家感知 | 复活/重生提示 |
| 建议音效 | 轻快回归或能量回充音 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 复活标记时发送 `PlayerRespawnedEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/DeathRespawn/Commands/MarkPlayerRespawnedCommand.cs`<br>类/方法：`MarkPlayerRespawnedCommand.OnExecute()`<br>行号范围：`L25-L29` |
| 代码证据（必须） | `this.SendEvent(new PlayerRespawnedEvent { WorldPos = _worldPos });` |
| 置信度 | High（显式事件） |
| 优先级 | P1 |
| 备注 | 可与镜头/粒子效果同步 |

### Enemy

<a id="SFX-ENM-0001"></a>
### SFX-ENM-0001 虫子追光开始
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENM-0001 |
| 系统/对象 | Enemy / BugMovementBase |
| 玩家感知 | 虫子从游荡切换为追光状态 |
| 建议音效 | 轻微嗡鸣加速、追逐提示 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 找到光源后 `TransitionTo(BugState.ChaseLight, true)` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s（受 `scanInterval` 限制） |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/BugAI/Controllers/BugMovementBase.cs`<br>类/方法：`BugMovementBase.TryAcquireLamp()`<br>行号范围：`L290-L299` |
| 代码证据（必须） | `SetTrackedLamp(lampInfo);`<br>`TransitionTo(BugState.ChaseLight, true);` |
| 置信度 | Medium（状态切换无事件） |
| 优先级 | P1 |
| 备注 | 可与追光循环音的 Start 绑定 |

<a id="SFX-ENM-0002"></a>
### SFX-ENM-0002 虫子退回巢穴/冷却
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENM-0002 |
| 系统/对象 | Enemy / BugMovementBase |
| 玩家感知 | 追光失败后回巢或恢复游荡 |
| 建议音效 | 嗡鸣降速或退场提示 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 失去目标触发 `TransitionTo(BugState.ReturnHome)`；进入巢穴触发 `TransitionTo(BugState.Loiter)` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/BugAI/Controllers/BugMovementBase.cs`<br>类/方法：`BugMovementBase.UpdateChaseLight()` / `UpdateReturnHome()`<br>行号范围：`L303-L313`、`L341-L348` |
| 代码证据（必须） | `TransitionTo(BugState.ReturnHome);`<br>`TransitionTo(BugState.Loiter);` |
| 置信度 | Medium（状态切换无事件） |
| 优先级 | P2 |
| 备注 | 也可作为追光循环音的 Stop 点 |

<a id="SFX-ENM-0003"></a>
### SFX-ENM-0003 虫子抓取/放手
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENM-0003 |
| 系统/对象 | Enemy / BugGrabInteraction |
| 玩家感知 | 玩家抓住虫子或放开虫子 |
| 建议音效 | 轻柔抓握“啪/嗒”+ 放开“扑” |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | 抓取条件满足时 `NotifyPlayerGrabbed()`，松手时 `NotifyPlayerReleased()` |
| 停止条件（Loop 必填） | 放手/离开触发器即停止 |
| 推荐防抖/冷却 | 0.1s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/BugAI/Controllers/BugGrabInteraction.cs`<br>类/方法：`BugGrabInteraction.Update()` / `OnTriggerExit2D()`<br>行号范围：`L69-L87`、`L108-L112` |
| 代码证据（必须） | `movement?.NotifyPlayerGrabbed();`<br>`movement?.NotifyPlayerReleased();` |
| 置信度 | High（显式调用） |
| 优先级 | P1 |
| 备注 | 可与玩家抓取键音效做区分 |

<a id="SFX-ENM-0004"></a>
### SFX-ENM-0004 虫子踩踏反弹
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENM-0004 |
| 系统/对象 | Enemy / BugStompInteraction |
| 玩家感知 | 踩踏虫子并反弹 |
| 建议音效 | 轻微“咚/噗”踩踏反馈 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 玩家进入触发器且 `enableStompResponse=true` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.1s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/BugAI/Controllers/BugStompInteraction.cs`<br>类/方法：`BugStompInteraction.OnTriggerEnter2D()`<br>行号范围：`L25-L48` |
| 代码证据（必须） | `velocity.y = Mathf.Max(velocity.y, bounceVelocity);`<br>`movement.ResetToHome();` |
| 置信度 | High（显式逻辑） |
| 优先级 | P1 |
| 备注 | 可叠加轻微弹跳风声 |

<a id="SFX-ENM-0005"></a>
### SFX-ENM-0005 幽灵漂浮循环
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENM-0005 |
| 系统/对象 | Enemy / GhostMechanism2D |
| 玩家感知 | 幽灵在范围内漂浮移动 |
| 建议音效 | 低频幽灵飘忽循环（轻微起伏） |
| 触发类型 | Loop |
| 触发条件（来自代码） | `Update()` 中持续调用 `StepMovement()` |
| 停止条件（Loop 必填） | 组件禁用/销毁或 bounds 无效 |
| 推荐防抖/冷却 | 无 |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Mechanisms/Controllers/GhostMechanism2D.cs`<br>类/方法：`GhostMechanism2D.StepMovement()`<br>行号范围：`L150-L190` |
| 代码证据（必须） | `StepMovement(dt);`<br>`transform.position = new Vector3(_currentPos.x, _currentPos.y, _z);` |
| 置信度 | Medium（循环需实现层接入） |
| 优先级 | P2 |
| 备注 | 可用轻微随机调制匹配噪声漂移 |

<a id="SFX-ENM-0006"></a>
### SFX-ENM-0006 幽灵接触吸光
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENM-0006 |
| 系统/对象 | Enemy / GhostMechanism2D |
| 玩家感知 | 贴近幽灵时被持续吸光 |
| 建议音效 | 接触“嘶/嗡”+ 持续吸光循环 |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | `OnTriggerEnter2D` 使 `_playerOverlapCount>0`，`StepDrain()` 持续扣光 |
| 停止条件（Loop 必填） | `OnTriggerExit2D` 使重叠数归零，`StepDrain()` 停止 |
| 推荐防抖/冷却 | 无 |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Mechanisms/Controllers/GhostMechanism2D.cs`<br>类/方法：`GhostMechanism2D.StepDrain()` / `OnTriggerEnter2D()` / `OnTriggerExit2D()`<br>行号范围：`L199-L214`、`L391-L409` |
| 代码证据（必须） | `if (_playerOverlapCount <= 0) return;`<br>`this.SendCommand(new ConsumeLightCommand(amount, consumeReason));` |
| 置信度 | High（接触+持续扣光） |
| 优先级 | P0 |
| 备注 | 建议与 UI 光量变化配合侧链 | 

### Interactable / Puzzle

<a id="SFX-INT-0001"></a>
### SFX-INT-0001 风铃花激活/熄灭
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0001 |
| 系统/对象 | Interactable / BellFlower2D |
| 玩家感知 | 花被点亮并触发机关；或光不足熄灭 |
| 建议音效 | 轻脆铃音激活；回落时淡出或熄灭音 |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | 计时满足后 `SetActive(true)`；失去光照且允许关闭时 `SetActive(false)` |
| 停止条件（Loop 必填） | `SetActive(false)` 即停止循环/氛围 |
| 推荐防抖/冷却 | 0.2s（防止抖动） |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/BellFlower/Controllers/BellFlower2D.cs`<br>类/方法：`BellFlower2D.Update()` / `SetActive()`<br>行号范围：`L28-L40`、`L85-L92` |
| 代码证据（必须） | `SetActive(true);` / `SetActive(false);`<br>`this.SendEvent(new FlowerActivatedEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P1 |
| 备注 | 可按 `activationSeconds` 做缓慢起音 |

<a id="SFX-INT-0002"></a>
### SFX-INT-0002 门开关（逻辑门）
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0002 |
| 系统/对象 | Interactable / DoorGate |
| 玩家感知 | 门开启或关闭状态变化 |
| 建议音效 | 机关门“咔哒/隆隆”开关 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `DoorStateChangedEvent` 在门状态改变时发送；打开时额外 `DoorOpenEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/DoorGate/Commands/UpdateDoorProgressCommand.cs`<br>类/方法：`UpdateDoorProgressCommand.OnExecute()`<br>行号范围：`L34-L61` |
| 代码证据（必须） | `this.SendEvent(new DoorStateChangedEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P0 |
| 备注 | 与 DoorMechanism 的运动循环配合 |

<a id="SFX-INT-0003"></a>
### SFX-INT-0003 门体运动循环（表现层）
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0003 |
| 系统/对象 | Interactable / DoorMechanism2D |
| 玩家感知 | 门体上下移动过程 |
| 建议音效 | 低频门体移动循环（起/止点轻响） |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | `_isOpen` 状态变化后，`UpdateDoorsHeight()` 持续 MoveTowards |
| 停止条件（Loop 必填） | 高度达到目标或 `_isOpen` 反向变化 |
| 推荐防抖/冷却 | 无 |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Mechanisms/Controllers/DoorMechanism2D.cs`<br>类/方法：`DoorMechanism2D.UpdateDoorsHeight()` / `OnDoorStateChanged()`<br>行号范围：`L65-L92`、`L120-L129` |
| 代码证据（必须） | `size.y = Mathf.MoveTowards(...);`<br>`_isOpen = e.IsOpen;` |
| 置信度 | Medium（需在状态变化处启动/停止） |
| 优先级 | P1 |
| 备注 | 适合与 SFX-INT-0002 形成开关+运动两段音 |

<a id="SFX-INT-0004"></a>
### SFX-INT-0004 藤蔓生长/回缩
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0004 |
| 系统/对象 | Interactable / VineMechanism2D |
| 玩家感知 | 光照触发生长，离开光照回缩 |
| 建议音效 | 生长“沙沙”与回缩“唰” |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | `shouldActivate` 变化后调用 `PlayGrowthAnimation(target)` |
| 停止条件（Loop 必填） | Tween 完成（growthValue 达到 0/1） |
| 推荐防抖/冷却 | 0.2s（防止光照抖动） |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Mechanisms/Controllers/VineMechanism2D.cs`<br>类/方法：`VineMechanism2D.Update()` / `PlayGrowthAnimation()`<br>行号范围：`L70-L98`、`L148-L156` |
| 代码证据（必须） | `PlayGrowthAnimation(_isActivated ? 1f : 0f);` |
| 置信度 | High（显式动画入口） |
| 优先级 | P1 |
| 备注 | 可按生长方向做左右/上下声像偏移 |

<a id="SFX-INT-0005"></a>
### SFX-INT-0005 冰块消融循环
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0005 |
| 系统/对象 | Interactable / IceBlock2D |
| 玩家感知 | 靠近冰块导致持续消融、滴水/裂纹 |
| 建议音效 | 冰裂+滴水循环，随进度增强 |
| 触发类型 | Loop + Param Driven |
| 触发条件（来自代码） | 玩家触发器内且 `_currentProgress < meltLightAmount` 时 `HandleMelting()` |
| 停止条件（Loop 必填） | 玩家离开并进入恢复，或消融完成进入序列 |
| 推荐防抖/冷却 | 无 |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/IceBlock/Controllers/IceBlock2D.cs`<br>类/方法：`IceBlock2D.Update()` / `HandleMelting()`<br>行号范围：`L53-L96` |
| 代码证据（必须） | `if (isTouching) { HandleMelting(); }`<br>`this.SendCommand(new ConsumeLightCommand(...));` |
| 置信度 | High（显式消融流程） |
| 优先级 | P0 |
| 备注 | 可按 `_currentProgress / meltLightAmount` 调参 |

<a id="SFX-INT-0006"></a>
### SFX-INT-0006 冰块消融完成/凝结
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0006 |
| 系统/对象 | Interactable / IceBlock2D |
| 玩家感知 | 冰块完全融化打开通路，等待后回凝 | 
| 建议音效 | 融化崩解“咔嚓” + 回凝“结冰” |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | 进度满后进入 `CompleteMeltSequence()`，`solidCollider.enabled=false`；结束时 `solidCollider.enabled=true` |
| 停止条件（Loop 必填） | 回凝结束/碰撞体恢复 |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/IceBlock/Controllers/IceBlock2D.cs`<br>类/方法：`IceBlock2D.CompleteMeltSequence()`<br>行号范围：`L136-L167` |
| 代码证据（必须） | `if (solidCollider != null) solidCollider.enabled = false;`<br>`solidCollider.enabled = true;` |
| 置信度 | High（显式开关） |
| 优先级 | P1 |
| 备注 | 可在等待阶段做低频滴水残响 |

<a id="SFX-INT-0007"></a>
### SFX-INT-0007 灯生成/点亮
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0007 |
| 系统/对象 | Interactable / KeroseneLampManager |
| 玩家感知 | 场景/剧情生成灯，或持有灯生成 |
| 建议音效 | 点燃/亮起“噗”+轻微火焰起音 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `SpawnLamp()` 或 `SpawnHeldLampIfNeeded()` 实例化灯并启用视觉/玩法 |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampManager.cs`<br>类/方法：`KeroseneLampManager.SpawnLamp()` / `SpawnHeldLampIfNeeded()`<br>行号范围：`L88-L132`、`L189-L244` |
| 代码证据（必须） | `instance = Instantiate(prefab, ...);`<br>`lampInstance.SetGameplayEnabled(true);` |
| 置信度 | High（显式生成） |
| 优先级 | P1 |
| 备注 | 点燃循环可由灯自身粒子/火焰系统接入 |

<a id="SFX-INT-0008"></a>
### SFX-INT-0008 灯拾起/放下
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0008 |
| 系统/对象 | Interactable / KeroseneLampManager |
| 玩家感知 | 玩家死亡掉灯/重新持有灯 |
| 建议音效 | 拾起“咔哒”+放下“嘭/碰” |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `DropHeldLamp()` 调用 `SetHeld(false)`；`AttachHeldLamp()` 调用 `SetHeld(true)` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.1s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampManager.cs`<br>类/方法：`DropHeldLamp()` / `AttachHeldLamp()`<br>行号范围：`L247-L285` |
| 代码证据（必须） | `_heldLampInstance.SetHeld(false);`<br>`lampInstance.SetHeld(true);` |
| 置信度 | High（显式持有切换） |
| 优先级 | P1 |
| 备注 | 可依据落地高度做音量随机化 |

<a id="SFX-INT-0009"></a>
### SFX-INT-0009 灯玩法失效/熄灭
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0009 |
| 系统/对象 | Interactable / KeroseneLampModel + KeroseneLampInstance |
| 玩家感知 | 灯达到上限被关闭/熄灭 |
| 建议音效 | 熄灭“噗”或气流熄火 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 超过区域上限时 `SetLampGameplayEnabled(false)` 并发送 `LampGameplayStateChangedEvent`；实例 `gameplayDisabledSfx.Play()` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/KeroseneLamp/Commands/RecordLampSpawnedCommand.cs`<br>类/方法：`RecordLampSpawnedCommand.OnExecute()`<br>行号范围：`L93-L107`<br>文件路径：`Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampInstance.cs`<br>类/方法：`KeroseneLampInstance.SyncGameplayState()`<br>行号范围：`L118-L132` |
| 代码证据（必须） | `this.SendEvent(new LampGameplayStateChangedEvent { ... });`<br>`gameplayDisabledSfx.Play();` |
| 置信度 | High（显式逻辑+已有 AudioSource） |
| 优先级 | P1 |
| 备注 | 已有 `gameplayDisabledSfx`，后续可改为统一音效入口 |

<a id="SFX-INT-0010"></a>
### SFX-INT-0010 灯视觉显隐切换
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0010 |
| 系统/对象 | Interactable / KeroseneLampManager |
| 玩家感知 | 进入/离开区域时灯显隐变化 |
| 建议音效 | 轻微“呼”或环境渐入/渐出 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 区域切换后 `SetLampVisualStateCommand` 触发 `LampVisualStateChangedEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/KeroseneLamp/Commands/SetLampVisualStateCommand.cs`<br>类/方法：`SetLampVisualStateCommand.OnExecute()`<br>行号范围：`L31-L35`<br>文件路径：`Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampManager.cs`<br>类/方法：`KeroseneLampManager.OnAreaChanged()`<br>行号范围：`L179-L185` |
| 代码证据（必须） | `this.SendEvent(new LampVisualStateChangedEvent { ... });` |
| 置信度 | Medium（视玩法需要） |
| 优先级 | P2 |
| 备注 | 更适合作为环境音淡入/淡出提示 |

<a id="SFX-INT-0011"></a>
### SFX-INT-0011 存档点触发
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0011 |
| 系统/对象 | Interactable / Mailbox2D + Checkpoint |
| 玩家感知 | 触发存档点/检查点 |
| 建议音效 | 轻快提示音/存档“叮” |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 玩家进入触发器后发送 `SetCurrentCheckpointCommand`，进而发 `CheckpointChangedEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Checkpoint/Controllers/Mailbox2D.cs`<br>类/方法：`Mailbox2D.OnTriggerEnter2D()`<br>行号范围：`L28-L48`<br>文件路径：`Assets/Scripts/Features/Checkpoint/Commands/SetCurrentCheckpointCommand.cs`<br>类/方法：`SetCurrentCheckpointCommand.OnExecute()`<br>行号范围：`L30-L48` |
| 代码证据（必须） | `this.SendCommand(new SetCurrentCheckpointCommand(...));`<br>`this.SendEvent(new CheckpointChangedEvent { ... });` |
| 置信度 | High（显式命令+事件） |
| 优先级 | P1 |
| 备注 | 可与 UI 提示同步 |

<a id="SFX-INT-0012"></a>
### SFX-INT-0012 剧情触发门开关
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0012 |
| 系统/对象 | Interactable / StoryTaskTrigger2D |
| 玩家感知 | 剧情触发门开启/关闭 |
| 建议音效 | 机关门触发声（同门开关） |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `StoryTaskActionType.SetGateState` 执行 `SetDoorStateCommand` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/StoryTasks/Controllers/StoryTaskTrigger2D.cs`<br>类/方法：`StoryTaskTrigger2D.ExecuteAction()`<br>行号范围：`L99-L103` |
| 代码证据（必须） | `this.SendCommand(new SetDoorStateCommand(action.DoorId, action.GateOpen));` |
| 置信度 | High（显式命令） |
| 优先级 | P1 |
| 备注 | 可复用 SFX-INT-0002 音效标签 |

<a id="SFX-INT-0013"></a>
### SFX-INT-0013 剧情触发生成灯
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0013 |
| 系统/对象 | Interactable / StoryTaskTrigger2D |
| 玩家感知 | 剧情触发灯生成/点亮 |
| 建议音效 | 点火/生成提示 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `StoryTaskActionType.SpawnLamp` 发送 `RequestSpawnLampEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/StoryTasks/Controllers/StoryTaskTrigger2D.cs`<br>类/方法：`StoryTaskTrigger2D.ExecuteSpawnLamp()`<br>行号范围：`L140-L154` |
| 代码证据（必须） | `this.SendEvent(new RequestSpawnLampEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P2 |
| 备注 | 可复用 SFX-INT-0007 标签 |

<a id="SFX-INT-0014"></a>
### SFX-INT-0014 垃圾桶落石事件开始/结束
| 字段 | 内容 |
| --- | --- |
| ID | SFX-INT-0014 |
| 系统/对象 | Interactable / FallingRockFromTrashCanController |
| 玩家感知 | 进入区域触发落石，离开后结束 |
| 建议音效 | 落石持续“轰隆”循环 + 结束淡出 |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | `StartFallingEvent()` 发送 `FallingRockFromTrashCanStartedEvent` 并开启生成协程 |
| 停止条件（Loop 必填） | `EndFallingEvent()` 发送 `...EndedEvent`，延迟后停止生成 |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/FallingRockFromTrashCan/Controllers/FallingRockFromTrashCanController.cs`<br>类/方法：`StartFallingEvent()` / `EndFallingEvent()`<br>行号范围：`L85-L123` |
| 代码证据（必须） | `this.SendEvent(new FallingRockFromTrashCanStartedEvent { ... });`<br>`this.SendEvent(new FallingRockFromTrashCanEndedEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P1 |
| 备注 | 建议控制循环音层级避免淹没环境 |

### Environment / Ambient

<a id="SFX-ENV-0001"></a>
### SFX-ENV-0001 进入/离开黑暗
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0001 |
| 系统/对象 | Environment / Darkness |
| 玩家感知 | 进入黑暗区域或离开黑暗 |
| 建议音效 | 氛围压低/风声变化，进入/离开提示 |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | `SetInDarknessCommand` 状态变化时发送 `DarknessStateChangedEvent` |
| 停止条件（Loop 必填） | `IsInDarkness=false` 时停止黑暗循环 |
| 推荐防抖/冷却 | 0.2s（有 enter/exit delay） |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Darkness/Commands/SetInDarknessCommand.cs`<br>类/方法：`SetInDarknessCommand.OnExecute()`<br>行号范围：`L16-L28` |
| 代码证据（必须） | `this.SendEvent(new DarknessStateChangedEvent { IsInDarkness = _isInDarkness });` |
| 置信度 | High（显式事件） |
| 优先级 | P1 |
| 备注 | 可做环境滤波与混响切换 |

<a id="SFX-ENV-0002"></a>
### SFX-ENV-0002 黑暗扣光循环
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0002 |
| 系统/对象 | Environment / DarknessSystem |
| 玩家感知 | 处于黑暗时持续扣光 |
| 建议音效 | 低频压迫感循环/心跳渐强 |
| 触发类型 | Loop |
| 触发条件（来自代码） | `DarknessSystem.Tick()` 中 `IsInDarkness` 为真时 `ConsumeLightCommand` |
| 停止条件（Loop 必填） | 退出黑暗或进入安全区 |
| 推荐防抖/冷却 | 无 |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Darkness/Systems/DarknessSystem.cs`<br>类/方法：`DarknessSystem.Tick()`<br>行号范围：`L33-L45` |
| 代码证据（必须） | `this.SendCommand(new ConsumeLightCommand(amount, ELightConsumeReason.Darkness));` |
| 置信度 | High（显式扣光） |
| 优先级 | P1 |
| 备注 | 可按剩余光量驱动强度 |

<a id="SFX-ENV-0003"></a>
### SFX-ENV-0003 进入/离开安全区
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0003 |
| 系统/对象 | Environment / SafeZone |
| 玩家感知 | 进入安全区或离开安全区 |
| 建议音效 | 护罩“嗡”进入/退出提示 |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | `SetSafeZoneCountCommand` 发送 `SafeZoneStateChangedEvent` |
| 停止条件（Loop 必填） | `IsSafe=false` 时停止安全区循环 |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/SafeZone/Commands/SetSafeZoneCountCommand.cs`<br>类/方法：`SetSafeZoneCountCommand.OnExecute()`<br>行号范围：`L26-L37` |
| 代码证据（必须） | `this.SendEvent(new SafeZoneStateChangedEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P1 |
| 备注 | 与黑暗氛围互斥播放 |

<a id="SFX-ENV-0004"></a>
### SFX-ENV-0004 安全区回光循环
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0004 |
| 系统/对象 | Environment / SafeZoneSystem |
| 玩家感知 | 安全区内持续回复光量 |
| 建议音效 | 柔和能量回充循环 |
| 触发类型 | Loop |
| 触发条件（来自代码） | `SafeZoneSystem.Tick()` 中 `IsSafe` 为真时 `AddLightCommand` |
| 停止条件（Loop 必填） | `IsSafe=false` |
| 推荐防抖/冷却 | 无 |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/SafeZone/Systems/SafeZoneSystem.cs`<br>类/方法：`SafeZoneSystem.Tick()`<br>行号范围：`L24-L36` |
| 代码证据（必须） | `this.SendCommand(new AddLightCommand(amount));` |
| 置信度 | High（显式回光） |
| 优先级 | P1 |
| 备注 | 可用轻微颗粒音强化安全感 |

<a id="SFX-ENV-0005"></a>
### SFX-ENV-0005 区域切换提示
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0005 |
| 系统/对象 | Environment / AreaSystem |
| 玩家感知 | 进入新区域时环境变化 |
| 建议音效 | 氛围过门/提示音或环境渐变 |
| 触发类型 | Start/Stop Pair |
| 触发条件（来自代码） | 区域 ID 变化时发送 `AreaChangedEvent` |
| 停止条件（Loop 必填） | 新区域 BGM/环境稳定后 |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/AreaSystem/Commands/SetCurrentAreaCommand.cs`<br>类/方法：`SetCurrentAreaCommand.OnExecute()`<br>行号范围：`L24-L31` |
| 代码证据（必须） | `this.SendEvent(new AreaChangedEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P2 |
| 备注 | 建议与环境音/灯视觉切换同步 |

<a id="SFX-ENV-0006"></a>
### SFX-ENV-0006 危险体积秒杀
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0006 |
| 系统/对象 | Environment / HazardVolume2D |
| 玩家感知 | 触碰危险体积立即死亡 |
| 建议音效 | 高强度打击/爆裂死亡音 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `OnTriggerEnter2D` 且 `mode == InstantKill` 时 `ApplyInstantKill` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Hazard/Controllers/HazardVolume2D.cs`<br>类/方法：`HazardVolume2D.OnTriggerEnter2D()`<br>行号范围：`L34-L44` |
| 代码证据（必须） | `this.GetSystem<IHazardSystem>().ApplyInstantKill(...);` |
| 置信度 | High（显式逻辑） |
| 优先级 | P0 |
| 备注 | 可按 `deathReason` 区分音色 |

<a id="SFX-ENV-0007"></a>
### SFX-ENV-0007 危险体积持续扣光
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0007 |
| 系统/对象 | Environment / HazardVolume2D |
| 玩家感知 | 身处危险区域持续扣光 |
| 建议音效 | 持续灼烧/腐蚀循环 |
| 触发类型 | Loop |
| 触发条件（来自代码） | `OnTriggerStay2D` 且 `mode == DrainFast` 时 `ApplyLightDrainRatio` |
| 停止条件（Loop 必填） | 离开触发器或模式切换 |
| 推荐防抖/冷却 | 无 |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Hazard/Controllers/HazardVolume2D.cs`<br>类/方法：`HazardVolume2D.OnTriggerStay2D()`<br>行号范围：`L47-L55` |
| 代码证据（必须） | `ApplyLightDrainRatio(drainRatioPerSecond, Time.deltaTime, drainReason);` |
| 置信度 | High（显式逻辑） |
| 优先级 | P1 |
| 备注 | 与 `LightConsumedEvent` 可叠加 UI 提示 |

<a id="SFX-ENV-0008"></a>
### SFX-ENV-0008 伤害体积周期扣光
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0008 |
| 系统/对象 | Environment / DamageVolume2D |
| 玩家感知 | 站在伤害区域间歇受损 |
| 建议音效 | 规律性轻击/“嘟”提示 |
| 触发类型 | Cooldown Gated |
| 触发条件（来自代码） | `TryApply()` 冷却通过后 `ApplyLightCostRatio` |
| 停止条件（Loop 必填） | 离开触发器或冷却未到 |
| 推荐防抖/冷却 | 使用 `cooldownSeconds`（默认 1s） |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Hazard/Controllers/DamageVolume2D.cs`<br>类/方法：`DamageVolume2D.TryApply()`<br>行号范围：`L39-L52` |
| 代码证据（必须） | `_nextApplyTime = Time.time + Mathf.Max(0f, cooldownSeconds);` |
| 置信度 | High（显式冷却） |
| 优先级 | P1 |
| 备注 | 可与屏幕闪红同步 |

<a id="SFX-ENV-0009"></a>
### SFX-ENV-0009 尖刺击杀
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0009 |
| 系统/对象 | Environment / SpikeHazard2D |
| 玩家感知 | 触碰尖刺被击杀 |
| 建议音效 | 尖锐命中/失败提示 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `OnTriggerEnter2D` 中 `MarkDead` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/Mechanisms/Controllers/SpikeHazard2D.cs`<br>类/方法：`SpikeHazard2D.OnTriggerEnter2D()`<br>行号范围：`L23-L31` |
| 代码证据（必须） | `system.MarkDead(deathReason, other.transform.position);` |
| 置信度 | High（显式逻辑） |
| 优先级 | P0 |
| 备注 | 可按 `deathReason` 区分材质 |

<a id="SFX-ENV-0010"></a>
### SFX-ENV-0010 落石命中
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0010 |
| 系统/对象 | Environment / FallingRockProjectile |
| 玩家感知 | 落石砸到地面或玩家 |
| 建议音效 | 重物撞击、碎石飞溅 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `TryHandleHit()` 触发 `ProcessHitEffects()` 并 `hitSfx.Play()` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.1s（单次触发） |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/FallingRockFromTrashCan/Controllers/FallingRockProjectile.cs`<br>类/方法：`TryHandleHit()` / `PlayEffects()`<br>行号范围：`L101-L143`、`L214-L224` |
| 代码证据（必须） | `ProcessHitEffects();`<br>`hitSfx.Play();` |
| 置信度 | High（已有 AudioSource） |
| 优先级 | P0 |
| 备注 | 可按随机质量/规模微调音色 |

<a id="SFX-ENV-0011"></a>
### SFX-ENV-0011 掉落死亡体积
| 字段 | 内容 |
| --- | --- |
| ID | SFX-ENV-0011 |
| 系统/对象 | Environment / KillVolume2D |
| 玩家感知 | 掉入死亡体积立即死亡 |
| 建议音效 | 下坠终点“咚/失衡” |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `KillVolume2D.OnTriggerEnter2D()` 调用 `KillAt(EDeathReason.Fall)` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/DeathRespawn/Controllers/KillVolume2D.cs`<br>类/方法：`KillVolume2D.OnTriggerEnter2D()`<br>行号范围：`L8-L16` |
| 代码证据（必须） | `deathController.KillAt(EDeathReason.Fall, other.transform.position);` |
| 置信度 | High（显式逻辑） |
| 优先级 | P0 |
| 备注 | 可与落地死亡做区分音色 |

### UI

<a id="SFX-UI-0001"></a>
### SFX-UI-0001 保存按钮点击
| 字段 | 内容 |
| --- | --- |
| ID | SFX-UI-0001 |
| 系统/对象 | UI / SaveButtonsUI |
| 玩家感知 | 点击保存按钮 |
| 建议音效 | 轻快点击/保存提示 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 调用 `SaveButtonsUI.Save()` 执行保存 |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.05s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/SaveSystem/SaveButtonsUI.cs`<br>类/方法：`SaveButtonsUI.Save()`<br>行号范围：`L17-L22` |
| 代码证据（必须） | `saveManager.Save();` |
| 置信度 | High（显式入口） |
| 优先级 | P2 |
| 备注 | 失败/无存档可用不同提示音 |

<a id="SFX-UI-0002"></a>
### SFX-UI-0002 读取按钮点击
| 字段 | 内容 |
| --- | --- |
| ID | SFX-UI-0002 |
| 系统/对象 | UI / SaveButtonsUI |
| 玩家感知 | 点击读取按钮 |
| 建议音效 | 点击+加载提示 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 调用 `SaveButtonsUI.Load()` 执行加载 |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.05s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/SaveSystem/SaveButtonsUI.cs`<br>类/方法：`SaveButtonsUI.Load()`<br>行号范围：`L25-L30` |
| 代码证据（必须） | `saveManager.Load();` |
| 置信度 | High（显式入口） |
| 优先级 | P2 |
| 备注 | 如加载失败可播失败提示 |

<a id="SFX-UI-0003"></a>
### SFX-UI-0003 删除按钮点击
| 字段 | 内容 |
| --- | --- |
| ID | SFX-UI-0003 |
| 系统/对象 | UI / SaveButtonsUI |
| 玩家感知 | 点击删除存档按钮 |
| 建议音效 | 删除确认“咔/滴” |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 调用 `SaveButtonsUI.Delete()` 执行删除 |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.05s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/SaveSystem/SaveButtonsUI.cs`<br>类/方法：`SaveButtonsUI.Delete()`<br>行号范围：`L33-L37` |
| 代码证据（必须） | `saveManager.Delete();` |
| 置信度 | High（显式入口） |
| 优先级 | P2 |
| 备注 | 建议配合二次确认 UI |

### Narrative / Meta

<a id="SFX-META-0001"></a>
### SFX-META-0001 对话开始提示
| 字段 | 内容 |
| --- | --- |
| ID | SFX-META-0001 |
| 系统/对象 | Narrative / StoryTasks |
| 玩家感知 | 触发剧情对话 |
| 建议音效 | 对话提示“叮/纸张翻页” |
| 触发类型 | OneShot |
| 触发条件（来自代码） | `DialogueRequestedEvent` 被发送 |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.1s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/StoryTasks/Controllers/StoryTaskTrigger2D.cs`<br>类/方法：`StoryTaskTrigger2D.ExecuteDialogue()`<br>行号范围：`L131-L135` |
| 代码证据（必须） | `this.SendEvent(new DialogueRequestedEvent { ... });` |
| 置信度 | High（显式事件） |
| 优先级 | P1 |
| 备注 | 若对话系统已有语音，提示音可减弱 |

<a id="SFX-META-0002"></a>
### SFX-META-0002 光量耗尽
| 字段 | 内容 |
| --- | --- |
| ID | SFX-META-0002 |
| 系统/对象 | Meta / LightVitality |
| 玩家感知 | 光量归零、濒死提示 |
| 建议音效 | 低电量警报/熄灭声 |
| 触发类型 | OneShot |
| 触发条件（来自代码） | 光量从 >0 变为 <=0 时发送 `LightDepletedEvent` |
| 停止条件（Loop 必填） | 不适用（OneShot） |
| 推荐防抖/冷却 | 0.2s |
| 定位点（必须精确） | 文件路径：`Assets/Scripts/Features/LightVitality/Commands/LightVitalityCommandUtils.cs`<br>类/方法：`LightVitalityCommandUtils.ApplyCurrentLight()`<br>行号范围：`L22-L30` |
| 代码证据（必须） | `if (previous > 0f && clamped <= 0f) { sender.SendEvent(new LightDepletedEvent()); }` |
| 置信度 | High（显式事件） |
| 优先级 | P0 |
| 备注 | 可与 SFX-PLR-0004 死亡音效区分 |

## 4. 附录

### 4.1 去重合并规则
- 门相关：`DoorGate` 只负责状态变化事件，门体运动声音集中在 `DoorMechanism2D`；开关与运动分层处理。
- 灯相关：`SpawnLamp`、剧情 `RequestSpawnLampEvent`、玩家持有灯生成统一复用 `SFX_Lamp_Spawn` 语义。
- 扣光相关：`DarknessSystem`、`HazardVolume2D`、`DamageVolume2D` 均触发 `ConsumeLightCommand`，建议统一走“扣光循环/单次”语义标签并做原因变体。

### 4.2 风险点
- 循环叠加：黑暗/安全区/幽灵/危险体积可能同时触发循环，需做层级与优先级管理，避免过吵。
- 高频触发：`DamageVolume2D` 与落地/攀爬切换等容易密集触发，需冷却或随机化。
- 落石事件：`SpawnRoutine()` 可能短时间生成大量落石，冲击类音效需限并发。
- 区域切换：`AreaChangedEvent` 可能在重置/复活时连续触发，注意淡入淡出抖动。

### 4.3 需要进一步确认的问题
- 未发现统一 AudioManager/AudioBus 入口，后续实现需确认接入方案。
- 场景中是否存在脚步/跑步反馈需求（当前代码未显式给出脚步事件）。
