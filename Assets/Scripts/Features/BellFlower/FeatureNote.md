# Feature: BellFlower

## 1. Purpose
- 花在持续光照达到阈值后点亮，发送激活事件。
- 不直接开门，只负责广播 `FlowerActivatedEvent`。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/BellFlower/`
- Controllers:
  - `BellFlower2D.cs`
- Events:
  - `FlowerActivatedEvent`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `BellFlower2D`（花物体上）
- Inspector fields (if any):
  - `doorId` / `flowerId`
  - `lightAffectRadius` / `activationSeconds` / `minLampCount`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct FlowerActivatedEvent`
  - Payload: `DoorId`, `FlowerId`, `IsActive`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- 示例：DoorGate 监听 `FlowerActivatedEvent` 进行开门计数。

## 6. Verify Checklist
1. 在花附近放置 GameplayEnabled 灯。
2. 持续光照达到 `activationSeconds` 后触发 `FlowerActivatedEvent(IsActive=true)`。
3. 熄灭光源时（允许关闭时）触发 `IsActive=false`。

## 7. UNVERIFIED (only if needed)
- None.
