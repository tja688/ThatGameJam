# RULES.md — Project Execution Contract

### Attention, if you have not read AGENTS.md yet, please read AGENTS.md before reading this document

## 1. Environment & Scope Rules

### 1.1 Forbidden Modification of Framework Core
- **MUST NOT** modify files in `Assets/QFramework/**`.
- **Reason**: Preserve framework integrity and update compatibility. Manual list required for user if changes are needed.

### 1.2 RootApp Architecture
- **MUST** use a single `GameRootApp` (inheriting from `Architecture<GameRootApp>`) as the global entry point.
- **MUST NOT** create multiple Architecture roots for individual features unless it's an isolated demo.
- **Reason**: Simplifies cross-feature communication and module discovery.

### 1.3 Folder Strategy: Vertical Slice
- **MUST** organize features under `Assets/Scripts/Features/[FeatureName]/`.
- **Reason**: Improves maintainability and reduces merge conflicts.

### 1.4 Evidence-First Policy
- **MUST** provide evidence/reasoning for any architectural decision.
- Priority: Handbook Evidence > Official QFramework Docs/Source > Logic/Reasoning.

---

## 2. Architectural Layering (QFramework 4-Layers)

### 2.1 The Four Layers
1. **Controller (`IController`)**: MonoBehaviour / UI entry. Handles input and display.
2. **System (`ISystem`)**: Cross-feature logic (e.g., AchievementSystem, GameFlowSystem).
3. **Model (`IModel`)**: Shared state + domain policy. **MUST NOT** perform persistence I/O (PlayerPrefs / file / network).
4. **Utility (`IUtility`)**: Infrastructure (Storage, Network, SDK wrappers). **MUST** own persistence I/O implementations.

### 2.2 Dependency Rules
- **MUST**: Upwards dependency only (Controller → System/Model → Utility).
- **MUST NOT**: Downwards reference (System/Model must not hold Controller references).
- **DO**: Use Events/BindableProperties for downwards notification (Down → Up).

### 2.3 CQRS (Command Query Responsibility Segregation)
- **MUST**: Any state mutation MUST go through a `Command`.
- **MUST**: `ICommand` and `IQuery` objects MUST be stateless.
- **MUST**: Persistence I/O MUST be done in Utilities. Commands/Systems may call Utilities, but Models MUST remain I/O-free.
- **Reason**: Ensures predictable state changes and testability.
- **MUST NOT**: Systems/Controllers directly set Model state (e.g., `BindableProperty.Value = ...`).
  - Exception: Model initialization inside `OnInit()` may set initial values.

---

## 3. Communication & Lifecycle

### 3.1 Event Symmetry
- **MUST**: Every event subscription MUST have a corresponding unsubscription.
- **DO**: Use `.UnRegisterWhenGameObjectDestroyed(gameObject)` or `.UnRegisterWhenDisabled(gameObject)` for MonoBehaviours.

### 3.2 Singletons
- **PROHIBITED**: Service locator style global singletons used to bypass Architecture boundaries.
- **DO**: Register modules in the Architecture.
- **MAY**: Use `SingletonKit` for low-level Utility infrastructure (Utilities layer only).

---

## 4. UI & Toolkit Rules

### 4.1 UIKit Field Access
- **MUST**: Prefer using generated binding fields (Designer files) in UI Panels.
- **MUST NOT** use `transform.Find` to access child components unless the object is dynamically generated.
- **Reason**: Type safety and performance.

### 4.2 Code Generation Protection
- **MUST NOT** write business logic in `*.Designer.cs` or `*.Generated.cs` files.
- **MUST** write logic in the partial class file.

---

## 5. Toolkit Initialization (Preserved)

### 5.1 Toolkit Initialization Routine (Project Hard Rule)

- **Context**:
  - UIKit / AudioKit 默认会在 `BeforeSceneLoad` 把 LoaderPool 设为 ResKit（框架侧自动行为）。
  - 官方自定义加载示例在 `Start()` 设置一次，并建议注释掉 `*WithResKitInit`；但本项目禁止修改 `Assets/QFramework/**`，因此不可走“注释源码”的路线。

- **MUST** (Startup Determinism):
  1) 若项目需要“自定义 UIKit PanelLoaderPool / AudioKit AudioLoaderPool”（非 ResKit 默认）：
     - **MUST** 在“任何 UIKit/AudioKit 首次使用之前”完成覆盖设置（越早越好）。
     - **MUST** 提供一个项目侧的 `ProjectToolkitBootstrap`（放在 `Assets/Scripts/...`），使用
       `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`
       来设置：
       - `UIKit.Config.PanelLoaderPool = ...;`
       - `AudioKit.Config.AudioLoaderPool = ...;`
     - **NOTE**：因为框架会先把它们设为 ResKit，所以项目侧必须保证覆盖发生在你任何调用 UIKit/AudioKit API 之前。

  2) 若项目使用 ResKit 且会在启动期加载资源/场景：
     - **MUST** 在任何 ResKit 资源加载前调用一次 `ResKit.Init()` 或（需要时）`ResKit.InitAsync()`。
     - WebGL + AB 等需要异步初始化的场景，按官方说明使用 `InitAsync()`。

- **Evidence (Official)**:
  - UIKit 自定义加载：`UIKit.Config.PanelLoaderPool = ...`（示例在 Start 设置一次）。
  - UIKit 默认 ResKit 初始化：`UIKitWithResKitInit`（BeforeSceneLoad）。
  - AudioKit 自定义加载：`AudioKit.Config.AudioLoaderPool = ...`（示例在 Start 设置一次）。
  - AudioKit 默认 ResKit 初始化：`AudioKitWithResKitInit`（BeforeSceneLoad）。
  - ResKit 初始化：`ResKit.Init()` / `ResKit.InitAsync()`（快速入门示例 + WebGL 说明）。

