# Feature: PlayerCharacter2D

## 1. Purpose
- Provide 2D platformer character movement, jumping, and grounded state.
- Provide wall grab/climb with tag-based wall detection and climb coyote time.
- Expose bindables/events for movement state and allow external input.
- Lock player input while dead and restore on respawn.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/PlayerCharacter2D/`
- Controllers:
  - `PlatformerCharacterController.cs` 〞 main movement driver (Update + FixedUpdate)
  - `PlatformerCharacterInput.cs` 〞 Input System reader (implements `IPlatformerFrameInputSource`)
- Systems:
  - `IPlayerCharacter2DSystem`, `PlayerCharacter2DSystem` 〞 empty orchestration hook
- Models:
  - `IPlayerCharacter2DModel`, `PlayerCharacter2DModel` 〞 movement state and bindables
- Commands:
  - `SetFrameInputCommand`
  - `ResetClimbStateCommand`
  - `TickFixedStepCommand`
- Queries:
  - `GetDesiredVelocityQuery`
- Events:
  - `PlayerGroundedChangedEvent`
  - `PlayerClimbStateChangedEvent`
  - `PlayerJumpedEvent`
- Configs:
  - `PlatformerCharacterStats.cs` 〞 ScriptableObject movement + climb tuning
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.PlayerCharacter2D.Models.IPlayerCharacter2DModel`
  - System: `ThatGameJam.Features.PlayerCharacter2D.Systems.IPlayerCharacter2DSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlatformerCharacterController` on the player (requires Rigidbody2D + CapsuleCollider2D)
  - `PlatformerCharacterInput` on the same GameObject (or any `IPlatformerFrameInputSource`)
- Inspector fields (if any):
  - `PlatformerCharacterController._stats` 〞 `PlatformerCharacterStats` asset
  - `PlatformerCharacterController._useExternalInput` / `_externalInput` 〞 bypass input source
  - `PlatformerCharacterController._inputSource` 〞 optional explicit input source
  - `PlatformerCharacterController._climbSensor` 〞 trigger collider used for wall detection
  - `PlatformerCharacterController._climbableTag` 〞 tag for climbable walls (default: "Climbable")
  - `PlatformerCharacterController._climbableLayerMask` 〞 layer filter for climbable walls (default: Everything)
  - `PlatformerCharacterInput._move` / `_jump` 〞 InputActionReference bindings
  - `PlatformerCharacterInput._grab` 〞 InputActionReference for grab/hold
  - `PlatformerCharacterStats.ClimbLockSecondaryAxis` 〞 是否锁定攀爬次轴（默认 false）
  - `PlatformerCharacterStats.ClimbSecondaryAxisMultiplier` 〞 次轴速度倍率（默认 0.35）

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct PlayerGroundedChangedEvent`
  - When fired: grounded state toggles in `TickFixedStepCommand`
  - Payload: `Grounded` (bool), `ImpactSpeed` (float)
  - Typical listener: VFX, audio, camera shake
  - Example:
    ```csharp
    this.RegisterEvent<PlayerGroundedChangedEvent>(OnGrounded)
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct PlayerJumpedEvent`
  - When fired: jump executed in `TickFixedStepCommand`
  - Payload: none
  - Typical listener: SFX/animation
- `struct PlayerClimbStateChangedEvent`
  - When fired: climb state toggles in `TickFixedStepCommand` or `ResetClimbStateCommand`
  - Payload: `IsClimbing` (bool)
  - Typical listener: SFX/animation

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetFrameInputCommand`
  - What state it mutates: `IPlayerCharacter2DModel.FrameInput`, time, jump buffer flags
  - Typical sender: `PlatformerCharacterController`
- `ResetClimbStateCommand`
  - What state it mutates: `IPlayerCharacter2DModel.IsClimbing`, climb timers
  - Typical sender: `PlatformerCharacterController` (death/respawn)
- `TickFixedStepCommand`
  - What state it mutates: `IPlayerCharacter2DModel.Grounded`, `Velocity`, climb state/timers, jump state
  - Typical sender: `PlatformerCharacterController` (FixedUpdate)

### 4.4 Queries (Read Path, optional)
- `GetDesiredVelocityQuery`
  - What it returns: `Vector2` velocity from model
  - Typical caller: `PlatformerCharacterController` to drive Rigidbody2D

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `BindableProperty<bool> Grounded`
  - `BindableProperty<bool> IsClimbing`
  - `BindableProperty<Vector2> Velocity`
  - Usage notes: model also exposes `FrameInput` and timing flags for advanced systems

## 5. Typical Integrations
- Example: Play landing dust ↙ listen to `PlayerGroundedChangedEvent`.
- Example: Switch climb animation ↙ listen to `PlayerClimbStateChangedEvent`.

## 6. Verify Checklist
1. Add `PlatformerCharacterController` + `PlatformerCharacterInput` to the player.
2. Assign a `PlatformerCharacterStats` asset and InputAction references.
3. Press jump; expect `PlayerJumpedEvent` and movement to update.
4. Hold grab near a "Climbable" wall; expect `PlayerClimbStateChangedEvent` and climb motion.
5. While climbing, press jump; expect a vertical jump without noticeable horizontal push.
6. After the climb jump, confirm you do not immediately re-grab the wall.
7. Trigger `PlayerDiedEvent`; expect input to stop until `PlayerRespawnedEvent` fires.
8. Press `KillMe` input; expect held item to drop and held slot to clear.

## 7. Climb Bugfix Notes (2025-12-24)
### 7.1 Root Cause Summary
- Grounded wall grab blocked in `TickFixedStepCommand`: `wantsClimb` required `!model.Grounded.Value`, and climb exit also triggered on `model.Grounded.Value`. Result: ground contact (or groundHit from internal tiles) forced `IsClimbing` false and prevented entry.
- Wall detection instability in `PlatformerCharacterController.TryGetClimbableWall`: `ClosestPoint` on an overlap returns the sensor center when inside a collider, so `wallSideSign` becomes 0 then falls back to bounds center, which can flip. Combined with `ContactFilter2D.useLayerMask = false`, overlap candidates can shift to unrelated colliders and briefly drop `wallDetected`, draining `WallContactTimer` and exiting climb even while the sensor still overlaps a wall.

### 7.2 Fix Strategy
- Allow wall grab entry on ground by removing the grounded gate from `wantsClimb` and stopping climb exits from `Grounded` alone. Grounded JumpDown now exits climb and uses normal jump logic (no forced horizontal detach).
- Stabilize wall detection: `TryGetClimbableWall` now uses `Physics2D.Distance` to pick the closest candidate and derive wall side from separation normal/points (handles "sensor inside collider"), with `_climbableLayerMask` filtering plus tag check. While climbing, wall side is only updated if it does not flip against the existing side.
- Preserve climb coyote via `WallCoyoteTime` and avoid regrab lockout on passive losses (lockout is only applied when the player releases grab or performs a climb jump).
- Debug logging is gated by `PlatformerCharacterStats.EnableClimbDebugLogs` and only emits on climb enter/exit.

### 7.3 Tuning Notes
- `PlatformerCharacterStats.WallCoyoteTime`: raise to ~0.12 if composite boundaries still cause brief wall detection loss.
- `PlatformerCharacterStats.ClimbRegrabLockout`: keep short (0.05-0.1) so regrab feels responsive after a climb jump.
- `PlatformerCharacterStats.EnableClimbDebugLogs`: enable to see one-line enter/exit diagnostics.

### 7.4 Scene Verification Steps
1. Repro old bug: walk into a "Climbable" wall while grounded, hold or tap grab; previously did not enter climb. Now expect `PlayerClimbStateChangedEvent` and no slide away.
2. Repro composite exit: climb past a tilemap/composite wall that contains a non-colliding internal floor strip; previously climb exited mid-height. Now stay climbing as long as grab is held and wall remains detected.
3. While grounded and grabbing, press jump; expect a normal jump (no forced horizontal push).
4. Enable `EnableClimbDebugLogs` and verify enter/exit logs include groundHit/ceilingHit, wallDetected/side, timers, input, and collider identity.

## 8. UNVERIFIED (only if needed)
- None.

## 9. Change Log
- **Date**: 2025-12-30
- **Change**: `KillMe` input now drops the held item instead of triggering death.
- **Reason**: New backpack/held-slot flow uses the same input name to drop.
- **Behavior Now**:
  - Given a held item
  - When `KillMe` is pressed
  - Then the held item is dropped and the player remains alive
- **Config**:
  - None.
- **Risk & Regression**:
  - 影响范围：`KillMe` debug flow
  - 回归用例：`KillMe` no longer fires `MarkPlayerDeadCommand`.
- **Files Touched**:
  - `Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs`
- **Date**: 2025-12-28
- **Change**: 支持横向攀爬与次轴微调；攀爬表面方向自动判断。
- **Reason**: 新策划要求横向植物可攀爬，左右为主要移动，并允许微调。
- **Behavior Now**:
  - Given 具有 `Climbable` 标签的横向攀爬体
  - When 玩家抓住并输入左右
  - Then 以左右为主移动，且可按设置允许次轴微调
- **Config**:
  - `ClimbLockSecondaryAxis`：锁定次轴移动（true 为锁死）
  - `ClimbSecondaryAxisMultiplier`：次轴速度倍率
- **Risk & Regression**:
  - 影响范围：攀爬移动与吸附逻辑
  - 回归用例：竖墙攀爬上下正常；横向攀爬左右正常；攀爬跳仍可触发
- **Files Touched**:
  - `Assets/Scripts/Features/PlayerCharacter2D/Controllers/PlatformerCharacterController.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Configs/PlatformerCharacterStats.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Models/IPlayerCharacter2DModel.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Models/PlayerCharacter2DModel.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Commands/ResetClimbStateCommand.cs`
- **Date**: 2025-12-28
- **Change**: 攀爬状态跳跃改为优先触发，并按普通跳跃逻辑执行。
- **Reason**: 策划要求攀爬时只要按跳就必须起跳，且不影响平地控制。
- **Behavior Now**:
  - Given 玩家处于攀爬状态且按下跳跃
  - When 当前仍在抓取攀爬体
  - Then 立即退出攀爬并执行普通跳跃，不因抓取而卡住
- **Config**:
  - None.
- **Risk & Regression**:
  - 影响范围：攀爬跳与重新抓取时序
  - 回归用例：攀爬按跳必起跳；落地跳跃手感不变；跳后短时间不立即重抓
- **Files Touched**:
  - `Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs`
- **Date**: 2025-12-28
- **Change**: 增加攀爬跳跃保护，起跳上升阶段忽略抓取/吸附，落下后恢复。
- **Reason**: 策划要求持续按抓也必须能起跳，避免跳起瞬间被重新抓住。
- **Behavior Now**:
  - Given 玩家在攀爬中按下跳跃并持续抓取
  - When 角色处于上升态势
  - Then 忽略抓取/吸附逻辑，保证跳跃生效
- **Config**:
  - None.
- **Risk & Regression**:
  - 影响范围：攀爬跳后重抓时序，空中贴墙吸附
  - 回归用例：攀爬连跳稳定；落地后抓取恢复；平地跳跃不受影响
- **Files Touched**:
  - `Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Controllers/PlatformerCharacterController.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Models/IPlayerCharacter2DModel.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Models/PlayerCharacter2DModel.cs`
  - `Assets/Scripts/Features/PlayerCharacter2D/Commands/ResetClimbStateCommand.cs`
