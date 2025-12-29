# Feature: FallingRockFromTrashCan

## 1. Purpose
- 玩家进入区域后按顺序生成落石，玩家离开后延迟停止生成。
- 落石生成时随机旋转/缩放/质量，落地触发特效音效后销毁。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/FallingRockFromTrashCan/`
- Controllers:
  - `FallingRockFromTrashCanController.cs` 〞 触发区检测与生成调度
  - `FallingRockProjectile.cs` 〞 落石随机化与落地销毁
- Events:
  - `FallingRockFromTrashCanStartedEvent.cs`
  - `FallingRockFromTrashCanEndedEvent.cs`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `FallingRockFromTrashCanController`（触发器区域）
  - `FallingRockProjectile`（挂在落石预制体上，需 Rigidbody2D + Collider2D）
- Inspector fields (if any):
  - `rockPrefab` / `spawnPoints` / `spawnInterval` / `stopDelaySeconds`
  - `floorLayerMask` / `angularSpeedRange` / `scaleRange` / `massRange`
  - `visualRoot` / `hitVfx` / `hitSfx`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `FallingRockFromTrashCanStartedEvent`
  - When fired: 玩家进入触发区，落石事件开始。
  - Payload: `AreaTransform`
  - Typical listener:
    ```csharp
    this.RegisterEvent<FallingRockFromTrashCanStartedEvent>(OnFallingStart)
        .UnRegisterWhenDisabled(gameObject);
    ```
- `FallingRockFromTrashCanEndedEvent`
  - When fired: 玩家离开触发区，落石事件结束。
  - Payload: `AreaTransform`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：进入落石区域时开启警示 UI，离开时关闭。
- 示例：监听结束事件后停止其他机关联动。

## 6. Verify Checklist
1. 在场景中放置 `FallingRockFromTrashCanController` 并配置生成点。
2. 玩家进入触发区后按顺序生成落石，离开后 5 秒停止生成。
3. 落石碰到 Floor 层后隐藏本体并播放特效/音效，1 秒后销毁。

## 7. UNVERIFIED (only if needed)
- None.
