# Feature: BugAI

## 1. Purpose
- 以虫子状态机（Loiter/Chase/Return）驱动行为与移动。
- 支持光源扫描、追光、回巢与巢内乱飞。
- 虫子以运动学方式移动，不受玩家物理影响。
- 巢内乱飞保持默认朝向，追光/回巢时按正朝向对齐目标方向。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/BugAI/`
- Controllers:
  - `BugMovementBase.cs` 〞 主状态机 + 运动管线 + 光源扫描
  - `BugGrabInteraction.cs` 〞 抓取触发（OnPlayerGrab/OnPlayerRelease）
  - `BugStompInteraction.cs` 〞 踩踏弹跳 + 重置回巢

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `BugMovementBase` 〞 每只虫子的状态机核心
- Optional MonoBehaviours:
  - `BugGrabInteraction` 〞 若要支持抓取与放手冷却
  - `BugStompInteraction` 〞 若要踩踏弹跳
- Inspector fields (关键项):
  - `BugMovementBase.moveBounds` 〞 活动区域 Collider2D
  - `BugMovementBase.homeBounds` 〞 巢穴范围 Collider2D
  - `BugMovementBase.homeCenter/homeEnterRadius/homeExitRadius` 〞 巢穴位置与滞回半径
  - `BugMovementBase.scanTrigger/attentionRadius` 〞 扫描触发器与备用半径
  - `BugMovementBase.scanInterval/releaseScanCooldown` 〞 扫描与放手冷却
  - `BugMovementBase.turnSpeed/loiterSpeed/chaseSpeed/returnSpeed` 〞 转向与移动速度
  - `BugMovementBase.loiterJitterRadius` 〞 巢内乱飞偏移半径
  - `BugMovementBase.facingAxis/facingAngleOffset` 〞 正朝向叠加设置
  - `BugGrabInteraction.requireGrabHeld/autoGrabOnTouch` 〞 抓取触发方式
  - `BugStompInteraction.enableStompResponse` 〞 是否启用踩踏反馈

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- None.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None (内部仅调用 `GetGameplayEnabledLampsQuery` 读取灯光)。

### 4.5 Model Read Surface
- None.

### 4.6 Unity API (Direct Calls, optional)
- `BugMovementBase.NotifyPlayerGrabbed()` / `NotifyPlayerReleased()` 〞 外部脚本直接驱动抓取状态

## 5. Typical Integrations
- 示例：虫子挂 `BugMovementBase`，配置 MoveBounds/HomeBounds/巢穴中心；场景内放置灯后自动巢内乱飞→追光→回巢。
- 示例：虫子挂 `BugGrabInteraction`（触发器），玩家按住抓取键触发回巢与巢内乱飞，放手后进入 5s 冷却再扫描光源。
 - 示例：需要踩踏效果时，再开启 `BugStompInteraction.enableStompResponse`。

## 6. Verify Checklist
1. 场景内添加 `BugMovementBase`，设置 `moveBounds`、`homeBounds` 或 `homeCenter`，进入播放后虫子在巢内乱飞。
2. 配置 `scanTrigger` 或 `attentionRadius`，放置可用灯光（GameplayEnabled + VisualEnabled），虫子每 3 秒扫描一次并进入追光。
3. 关闭灯光或灯光移出活动范围后，虫子停止追光并回巢，进入巢内乱飞。
4. 给虫子挂 `BugGrabInteraction` 触发器，按住抓取键进入回巢与巢内乱飞；放手后 5 秒内不再扫描光源。
