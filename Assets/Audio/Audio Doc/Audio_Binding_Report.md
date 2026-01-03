# Audio Binding Report

## 已关联（来自 Assets/Audio/音效表_数据表_附件）
- SFX-PLR-0001 起跳 -> `Assets/Audio/音效表_数据表_附件/起跳.mp3`
- SFX-PLR-0002 落地 -> `Assets/Art/Audio/碰撞.mp3`（原有） + `Assets/Audio/音效表_数据表_附件/落地.mp3`
- SFX-PLR-0003 攀爬开始/结束 -> `Assets/Audio/音效表_数据表_附件/攀爬.mp3`
- SFX-PLR-0004 死亡 -> `Assets/Audio/音效表_数据表_附件/死亡.mp3`
- SFX-PLR-0005 复活 -> `Assets/Audio/音效表_数据表_附件/复活.mp3`
- SFX-ENM-0001 虫子追光开始 -> `Assets/Audio/音效表_数据表_附件/虫子追光.mp3`
- SFX-ENM-0003 虫子抓取/放手 -> `Assets/Audio/音效表_数据表_附件/抓取虫子.mp3`
- SFX-ENM-0005 幽灵漂浮循环 -> `Assets/Audio/音效表_数据表_附件/幽灵漂浮.mp3`
- SFX-INT-0001 风铃花激活/熄灭 -> `Assets/Audio/音效表_数据表_附件/风铃花.mp3`
- SFX-INT-0002 门开关（逻辑门） -> `Assets/Audio/音效表_数据表_附件/开关门.mp3`
- SFX-INT-0004 藤蔓生长/回缩 -> `Assets/Audio/音效表_数据表_附件/藤蔓生长_回缩.mp3`
- SFX-INT-0005 冰块消融循环 -> `Assets/Audio/音效表_数据表_附件/冰块消融，消融完成，凝结.mp3`
- SFX-INT-0006 冰块消融完成/凝结 -> `Assets/Audio/音效表_数据表_附件/冰块消融，消融完成，凝结.mp3`
- SFX-INT-0007 灯生成/点亮 -> `Assets/Audio/音效表_数据表_附件/灯生成.mp3`
- SFX-INT-0011 存档点触发 -> `Assets/Audio/音效表_数据表_附件/存档点触发.mp3` + `Assets/Audio/音效表_数据表_附件/存档点触发(1).mp3`
- SFX-INT-0012 剧情触发门开关 -> `Assets/Audio/音效表_数据表_附件/开关门.mp3`
- SFX-INT-0013 剧情触发生成灯 -> `Assets/Audio/音效表_数据表_附件/灯生成.mp3`
- SFX-ENV-0010 落石命中 -> `Assets/Audio/音效表_数据表_附件/落石_击中.mp3` + `Assets/Audio/音效表_数据表_附件/落石_击中(1).mp3`
- SFX-UI-0001 保存按钮点击 -> `Assets/Audio/音效表_数据表_附件/按钮点击.mp3`
- SFX-UI-0002 读取按钮点击 -> `Assets/Audio/音效表_数据表_附件/按钮点击.mp3`
- SFX-UI-0003 删除按钮点击 -> `Assets/Audio/音效表_数据表_附件/按钮点击.mp3`

## 未匹配/待补充音效（数据库内仍为空）
- SFX-ENM-0002 虫子退回巢穴/冷却
- SFX-ENM-0004 虫子踩踏反弹
- SFX-ENM-0006 幽灵接触吸光
- SFX-ENV-0001 进入/离开黑暗
- SFX-ENV-0002 黑暗扣光循环
- SFX-ENV-0003 进入/离开安全区
- SFX-ENV-0004 安全区回光循环
- SFX-ENV-0005 区域切换提示
- SFX-ENV-0006 危险体积秒杀
- SFX-ENV-0007 危险体积持续扣光
- SFX-ENV-0008 伤害体积周期扣光
- SFX-ENV-0009 尖刺击杀
- SFX-ENV-0011 掉落死亡体积
- SFX-INT-0003 门体运动循环（表现层）
- SFX-INT-0008 灯拾起/放下
- SFX-INT-0009 灯玩法失效/熄灭
- SFX-INT-0010 灯视觉显隐切换
- SFX-INT-0014 垃圾桶落石事件开始/结束
- SFX-META-0001 对话开始提示
- SFX-META-0002 光量耗尽

## 未绑定的附件音频（暂无对应事件）
- `Assets/Audio/音效表_数据表_附件/背景音乐.mp3`
- `Assets/Audio/音效表_数据表_附件/结局一音乐.mp3`
- `Assets/Audio/音效表_数据表_附件/结局二音乐.mp3`
- `Assets/Audio/音效表_数据表_附件/（回访NPC）剧情音乐.mp3`
- `Assets/Audio/音效表_数据表_附件/（到达塔顶）演出音乐.mp3`

## 手动操作/确认事项
- 冰块消融相关同一文件被绑定到 `SFX-INT-0005` 与 `SFX-INT-0006`，建议在 Audio Binding Panel 里设置裁切时间区间（见下条）以区分段落。
- `SFX-PLR-0002 落地` 目前保留了原有 `Assets/Art/Audio/碰撞.mp3`，如果只想用新音效可手动移除旧 Clip。
- 已新增可选裁切功能：`ClipStartTime` / `ClipEndTime`（秒）。在 `Tools/Audio/Audio Binding Panel` 里可直接设置。当前仅对非 Loop 播放做结束裁切；Loop 会忽略结束时间但会应用起始时间。
