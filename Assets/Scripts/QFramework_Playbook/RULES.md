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

## 4. Extension Toolkits Policy (BANNED)

### 4.1 Hard Ban (New Code)

- **MUST NOT**: In any new code, introduce, reference, call, or require any QFramework extension toolkits / solution components, including (but not limited to) the following identifiers:
  - `UIKit`, `UIPanel`, `IUIData`, `UIData`
  - `ResKit`, `ResLoader`
  - `AudioKit`
  - `ActionKit`
  - `CodeGenKit`
  - `LoaderPool`, `PanelLoaderPool`, `AudioLoaderPool`
  - `UIKitWithResKitInit`, `AudioKitWithResKitInit`, `*WithResKitInit`
- **MUST**: UI MUST use Unity native solutions only (recommended: UGUI — `Canvas` + `MonoBehaviour` + `UnityEngine.UI`).
- **EXCEPTION**: Only if the user explicitly requests “allow using a specific toolkit”, the ban may be lifted for that request; the Agent MUST label it in the output as `EXCEPTION granted by user`.
- **Compatibility (legacy code)**: If the repo already contains historical usage of banned toolkits, maintenance policy is: **do not add new usages, do not expand dependency surface, do not refactor more code to depend on them**, unless the user explicitly requests it.
- Source: Project Playbook policy (maintenance directive, 2025-12-20; game-jam stability-first).

