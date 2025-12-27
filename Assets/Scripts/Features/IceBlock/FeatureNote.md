Feature: IceBlock (Updated)
1. Purpose
提供一种具有代价的、有时效性的路径阻挡机制。

玩家触发后，冰块通过“消融”表现由实变虚，允许通行。

在指定时间后，冰块通过“凝结”表现恢复物理碰撞。

新增： 若玩家在冰块凝结完成瞬间仍处于内部，则触发环境死亡。

2. Folder & Key Files
Root: Assets/Scripts/Features/IceBlock/

Controllers:

IceBlock2D.cs —— 核心逻辑控制，处理消融、凝结、光量消耗与死亡判定。

Dependencies:

LightVitality (Command/Query): 处理光量扣除。

DeathRespawn (Command): 处理挤压死亡判定。

3. Runtime Wiring
3.1 Scene setup (Unity Inspector)
Sprite Renderer: 必须关联冰块的视觉渲染器，用于执行 Color/Alpha 渐变。

Trigger Collider: 勾选了 Is Trigger 的感应器，负责监听玩家进入。

Solid Collider: 负责物理阻挡的碰撞体。

Transition Duration: 消融/凝结的渐变时间（默认 2s）。

Wait Duration: 物理消失持续时间（默认 5s）。

Light Cost Ratio: 触发消耗比例（默认 0.25）。

3.2 Logic Flow (Timeline)
玩家进入触发区：发出 ConsumeLightCommand。

消融阶段 (2s)：Solid Collider 依然生效，视觉开始向 Melted Color 渐变。

窗口阶段 (5s)：Solid Collider 禁用，玩家可通行。

凝结阶段 (2s)：Solid Collider 依然禁用，视觉向 Original Color 恢复（作为撤离提示）。

恢复瞬间：Solid Collider 启用，并执行 OverlapCollider 检查。

若检测到 Player Tag，发送 MarkPlayerDeadCommand(EDeathReason.Environment)。

4. Public API Surface
4.1 Commands (Used)
ConsumeLightCommand: 扣除相应比例的光量。

MarkPlayerDeadCommand: 当玩家被冻在冰块内时触发死亡。

4.2 Queries (Used)
GetMaxLightQuery: 用于计算扣除量的基数。

5. Typical Integrations
环境解谜：利用 5 秒的窗口期通过原本无法穿过的区域。

高风险博弈：玩家需预判 2 秒的凝结时间，否则会被直接强制死亡。

6. Verify Checklist
检查 IceBlock2D 物体在触发后，父物体是否保持 Active（确保协程运行）。

验证消融阶段（前 2s）玩家是否依然被物理阻挡。

验证凝结结束后，若玩家重叠在冰块内，是否正确触发了 DeathRespawn 系统的复活逻辑。

验证触发 RunResetEvent 后，冰块是否立即恢复初始颜色和物理碰撞。