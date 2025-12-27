# Feature: FallingRockFromTrashCan

## 1. Purpose
- 触发后生成落石（一次性或循环）。
- 使用对象池避免频繁 Instantiate。
- 落石伤害统一走 HazardSystem。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/FallingRockFromTrashCan/`
- Controllers:
  - `FallingRockFromTrashCanController.cs` 〞 生成与调度
  - `FallingRockProjectile.cs` 〞 落石行为与伤害
- Utilities:
  - `SimpleGameObjectPool.cs`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `FallingRockFromTrashCanController`（触发器）
  - `FallingRockProjectile`（挂在落石预制体上）
- Inspector fields (if any):
  - `rockPrefab` / `spawnPoints` / `preloadCount`
  - `spawnInterval` / `spawnCount` / `loopSpawn`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- None.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：垃圾桶触发器触发落石，落石碰到玩家即死亡。

## 6. Verify Checklist
1. 在场景中放置 `FallingRockFromTrashCanController` 并配置生成点。
2. 触发后生成落石，确认使用对象池复用对象。
3. 落石命中玩家触发死亡（统一走 `HazardSystem`）。

## 7. UNVERIFIED (only if needed)
- None.
