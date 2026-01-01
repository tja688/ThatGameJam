# SFX_Dotting_Index

## 系统目录树简图（Step A）

```
Assets/
  Scenes/
    正式关卡.unity
    Test Scene.unity
    Spine Test.unity
  Scripts/
    Root/
      GameRootApp.cs
      ProjectToolkitBootstrap.cs
    Features/
      AreaSystem/
      BellFlower/
      BugAI/
      Checkpoint/
      Darkness/
      DeathRespawn/
      DoorGate/
      FallingRockFromTrashCan/
      Hazard/
      HUD/
      IceBlock/
      KeroseneLamp/
      LightVitality/
      Mechanisms/
      PlayerCharacter2D/
      RunFailHandling/
      RunFailReset/
      SafeZone/
      StoryTasks/
      Testing/
    SaveSystem/
    Independents/
```

- 入口/启动：`Assets/Scripts/Root/ProjectToolkitBootstrap.cs`、`Assets/Scripts/Root/GameRootApp.cs`

## 快速索引（按系统）

### Player
- [SFX-PLR-0001 起跳](SFX_Dotting_Map.md#SFX-PLR-0001)
- [SFX-PLR-0002 落地](SFX_Dotting_Map.md#SFX-PLR-0002)
- [SFX-PLR-0003 攀爬开始/结束](SFX_Dotting_Map.md#SFX-PLR-0003)
- [SFX-PLR-0004 死亡](SFX_Dotting_Map.md#SFX-PLR-0004)
- [SFX-PLR-0005 复活](SFX_Dotting_Map.md#SFX-PLR-0005)

### Enemy
- [SFX-ENM-0001 虫子追光开始](SFX_Dotting_Map.md#SFX-ENM-0001)
- [SFX-ENM-0002 虫子退回巢穴/冷却](SFX_Dotting_Map.md#SFX-ENM-0002)
- [SFX-ENM-0003 虫子抓取/放手](SFX_Dotting_Map.md#SFX-ENM-0003)
- [SFX-ENM-0004 虫子踩踏反弹](SFX_Dotting_Map.md#SFX-ENM-0004)
- [SFX-ENM-0005 幽灵漂浮循环](SFX_Dotting_Map.md#SFX-ENM-0005)
- [SFX-ENM-0006 幽灵接触吸光](SFX_Dotting_Map.md#SFX-ENM-0006)

### Interactable / Puzzle
- [SFX-INT-0001 风铃花激活/熄灭](SFX_Dotting_Map.md#SFX-INT-0001)
- [SFX-INT-0002 门开关（逻辑门）](SFX_Dotting_Map.md#SFX-INT-0002)
- [SFX-INT-0003 门体运动循环（表现层）](SFX_Dotting_Map.md#SFX-INT-0003)
- [SFX-INT-0004 藤蔓生长/回缩](SFX_Dotting_Map.md#SFX-INT-0004)
- [SFX-INT-0005 冰块消融循环](SFX_Dotting_Map.md#SFX-INT-0005)
- [SFX-INT-0006 冰块消融完成/凝结](SFX_Dotting_Map.md#SFX-INT-0006)
- [SFX-INT-0007 灯生成/点亮](SFX_Dotting_Map.md#SFX-INT-0007)
- [SFX-INT-0008 灯拾起/放下](SFX_Dotting_Map.md#SFX-INT-0008)
- [SFX-INT-0009 灯玩法失效/熄灭](SFX_Dotting_Map.md#SFX-INT-0009)
- [SFX-INT-0010 灯视觉显隐切换](SFX_Dotting_Map.md#SFX-INT-0010)
- [SFX-INT-0011 存档点触发](SFX_Dotting_Map.md#SFX-INT-0011)
- [SFX-INT-0012 剧情触发门开关](SFX_Dotting_Map.md#SFX-INT-0012)
- [SFX-INT-0013 剧情触发生成灯](SFX_Dotting_Map.md#SFX-INT-0013)
- [SFX-INT-0014 垃圾桶落石事件开始/结束](SFX_Dotting_Map.md#SFX-INT-0014)

### Environment / Ambient
- [SFX-ENV-0001 进入/离开黑暗](SFX_Dotting_Map.md#SFX-ENV-0001)
- [SFX-ENV-0002 黑暗扣光循环](SFX_Dotting_Map.md#SFX-ENV-0002)
- [SFX-ENV-0003 进入/离开安全区](SFX_Dotting_Map.md#SFX-ENV-0003)
- [SFX-ENV-0004 安全区回光循环](SFX_Dotting_Map.md#SFX-ENV-0004)
- [SFX-ENV-0005 区域切换提示](SFX_Dotting_Map.md#SFX-ENV-0005)
- [SFX-ENV-0006 危险体积秒杀](SFX_Dotting_Map.md#SFX-ENV-0006)
- [SFX-ENV-0007 危险体积持续扣光](SFX_Dotting_Map.md#SFX-ENV-0007)
- [SFX-ENV-0008 伤害体积周期扣光](SFX_Dotting_Map.md#SFX-ENV-0008)
- [SFX-ENV-0009 尖刺击杀](SFX_Dotting_Map.md#SFX-ENV-0009)
- [SFX-ENV-0010 落石命中](SFX_Dotting_Map.md#SFX-ENV-0010)
- [SFX-ENV-0011 掉落死亡体积](SFX_Dotting_Map.md#SFX-ENV-0011)

### UI
- [SFX-UI-0001 保存按钮点击](SFX_Dotting_Map.md#SFX-UI-0001)
- [SFX-UI-0002 读取按钮点击](SFX_Dotting_Map.md#SFX-UI-0002)
- [SFX-UI-0003 删除按钮点击](SFX_Dotting_Map.md#SFX-UI-0003)

### Narrative / Meta
- [SFX-META-0001 对话开始提示](SFX_Dotting_Map.md#SFX-META-0001)
- [SFX-META-0002 光量耗尽](SFX_Dotting_Map.md#SFX-META-0002)
