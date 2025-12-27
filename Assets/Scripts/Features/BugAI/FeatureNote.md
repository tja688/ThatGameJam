# Feature: BugAI

## 1. Purpose
- 提供虫子基础移动与简单行为组合。
- 支持怕光、趋光、踩踏弹跳与电源诱导。

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/BugAI/`
- Controllers:
  - `BugMovementBase.cs` 〞 目标优先级移动
  - `BugFearLight.cs` 〞 远离光源
  - `BugAttractLamp.cs` 〞 靠近灯光
  - `BugStompInteraction.cs` 〞 被踩回位 + 玩家弹跳
  - `ElectricLure.cs` 〞 强制目标吸引器

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours (按需组合):
  - `BugMovementBase`
  - `BugFearLight` / `BugAttractLamp` / `BugStompInteraction`
  - `ElectricLure`（触发体）
- Inspector fields (if any):
  - `moveSpeed` / `fearRadius` / `attractRadius` / `bounceVelocity`

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
- 示例：灯光吸引虫子，踩踏后虫子回到初始点并给玩家弹跳。

## 6. Verify Checklist
1. 给虫子挂 `BugMovementBase` + `BugAttractLamp`，靠近灯时向灯移动。
2. 改为 `BugFearLight`，靠近灯时远离。
3. 玩家踩踏触发 `BugStompInteraction`，虫子回位且玩家获得向上速度。
4. 放置 `ElectricLure`，虫子进入触发器后强制靠近诱饵。

## 7. UNVERIFIED (only if needed)
- None.
