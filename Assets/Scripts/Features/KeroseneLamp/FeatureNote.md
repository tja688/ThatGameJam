# Feature: KeroseneLamp

## 1. Purpose
- 记录灯的注册表（lampId → 位置/区域/状态/实例）。
- 每次死亡或外部请求生成灯。
- 每区域最多 3 盏“有效灯”（GameplayEnabled），超出则淘汰最早灯。
- 跨区域仅关闭视觉光（VisualEnabled），玩法仍有效。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/KeroseneLamp/`
- Controllers:
  - `KeroseneLampManager.cs` 〞 生成/清理灯并响应区域切换
  - `KeroseneLampInstance.cs` 〞 控制灯的视觉/玩法状态表现
- Models:
  - `IKeroseneLampModel`, `KeroseneLampModel` 〞 注册表与计数
  - `LampInfo.cs` 〞 查询用灯快照
- Commands:
  - `RecordLampSpawnedCommand`
  - `SetLampGameplayStateCommand`
  - `SetLampVisualStateCommand`
  - `ResetLampsCommand`
- Events:
  - `LampSpawnedEvent` / `LampCountChangedEvent`（`_Shared`）
  - `LampGameplayStateChangedEvent`
  - `LampVisualStateChangedEvent`
  - `RequestSpawnLampEvent`
- Queries:
  - `GetGameplayEnabledLampsQuery`
  - `GetAllLampInfosQuery`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.KeroseneLamp.Models.IKeroseneLampModel`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `KeroseneLampManager` 〞 负责生成/回收
- Inspector fields (if any):
  - `lampPrefab` 〞 灯预制体（推荐挂 `KeroseneLampInstance`）
  - `lampParent` 〞 生成父节点（可选）
  - `maxActivePerArea` 〞 每区域最多有效灯数（默认 3）

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct LampSpawnedEvent`
  - When fired: `RecordLampSpawnedCommand`
  - Payload: `LampId`, `WorldPos`
- `struct LampCountChangedEvent`
  - When fired: lamp count changes
- `struct LampGameplayStateChangedEvent`
  - When fired: 有效灯淘汰或手动切换
  - Payload: `LampId`, `GameplayEnabled`
- `struct LampVisualStateChangedEvent`
  - When fired: 区域切换导致视觉开关变化
  - Payload: `LampId`, `VisualEnabled`

### 4.2 Request Events (Inbound write requests, optional)
> 外部请求生成灯
- `struct RequestSpawnLampEvent`
  - Who sends it: StoryTasks 等
  - Who handles it: `KeroseneLampManager`

### 4.3 Commands (Write Path)
- `RecordLampSpawnedCommand`
  - What state it mutates: 注册表、LampCount、区域有效灯计数
- `SetLampGameplayStateCommand`
  - What state it mutates: `GameplayEnabled`
- `SetLampVisualStateCommand`
  - What state it mutates: `VisualEnabled`
- `ResetLampsCommand`
  - What state it mutates: 注册表与计数清空

### 4.4 Queries (Read Path, optional)
- `GetGameplayEnabledLampsQuery`
  - What it returns: GameplayEnabled 灯列表（用于花/藤蔓/虫子）
- `GetAllLampInfosQuery`
  - What it returns: 全部灯快照（区域切换处理）

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<int> LampCount`

## 5. Typical Integrations
- 示例：藤蔓/花通过 `GetGameplayEnabledLampsQuery` 判断是否被光照。

## 6. Verify Checklist
1. 场景中添加 `PlayerAreaSensor` + `AreaVolume2D` + `KeroseneLampManager`，灯预制体挂 `KeroseneLampInstance`。
2. 玩家在区域 A 死亡，生成灯并可见；切换到区域 B 后，区域 A 的灯视觉关闭但玩法仍有效。
3. 在同一区域生成第 4 盏灯，最早灯 `GameplayEnabled=false` 且出现明显反馈。
4. 触发 `RunResetEvent`（HardReset），灯全部清理，`LampCount` 归零。

## 7. UNVERIFIED (only if needed)
- None.
