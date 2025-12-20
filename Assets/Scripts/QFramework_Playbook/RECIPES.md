# RECIPES.md — Executable Workflows

### Attention, if you have not read AGENTS.md yet, please read AGENTS.md before reading this document

This document contains repeatable workflows.
Agents MUST use the Preflight Plan before implementing.

---

## 0. Agent Preflight Plan (MUST)

Before writing code, the Agent MUST output a `PLAN` using this exact structure:

### PLAN

* **Intent (1 sentence):**
* **Placement decision:**

  * Primary: Controller / Model / System / Utility / Command / Query / Event
  * Rationale: (1–3 bullets, citing `RULES.md` section titles if applicable)
* **Files to add/modify (full paths):**
* **Cross-layer interaction map:**

  * **State change path:** (Commands → who handles → what changes)
  * **Notify path:** (Event/Bindable* → who listens)
  * **Read path:** (Query or direct read → who reads)
* **Lifecycle & cleanup:**

  * Registrations/unregistrations plan (OnEnable/OnDisable or QF auto helpers)
* **Verification plan:**

  * How to trigger in Unity
  * What logs/visible outcome proves success

* **Self-check (MUST):**
  * **Write-path audit:** 列出本次实现里所有“写状态点”（文件路径 + 符号名/方法名 + 1 句说明）
  * **Rule proof:** 明确说明每个写状态点为何符合 `RULES.md` → “2.3 CQRS”
  * **Forbidden patterns check:** 说明你已避免：
    - System/Controller 里直接 `model.SomeBindable.Value = ...`
    - Model 里直接调用 PlayerPrefs / 文件 IO / 网络 IO（除非 `RULES.md` 明确允许）

* **Risks / assumptions:**

  * Anything uncertain or “depends”

After implementation, the Agent MUST output:

* `CHANGES` (files + summary)
* `VERIFY` (steps)
* `MANUAL_ACTIONS` (if needed)

---

## 1. Create a New Feature

### When to use

You are introducing a new vertical slice feature.

### Steps

1. **Preflight Plan (MUST)**
   Use section `0. Agent Preflight Plan`.

2. **Create directory**

* `Assets/Scripts/Features/[FeatureName]/`

3. **Create layers (recommended baseline)**

* **Model:** `I[Feature]Model` + `[Feature]Model`
* **System:** `I[Feature]System` + `[Feature]System`
* **Controller:** MonoBehaviour implementing `IController`

4. **Wire into Architecture**

* Register the new Model/System/Utility in the project’s root Architecture init (see `TEMPLATES.md`).
* Controllers MUST return correct architecture via `GetArchitecture()`.

5. **Implement interactions using CQRS**

* State mutation MUST go through Commands.
* Reads SHOULD go through Queries (or direct read only if rules allow).
* Notifications via Event or Bindable*.
* After implementation, the Agent MUST perform the “Write-path audit” and include it in `NOTES` (Self-check subsection).

6. **Report**

* Output `CHANGES`, `VERIFY` and any caveats.

---

## 2. Adding System / Model / Command / Query

### 2.1 Add a System

1. Preflight Plan (MUST)
2. Add interface + implementation
3. Register in Architecture init
4. Ensure no forbidden dependencies (see `RULES.md`)

### 2.2 Add a Model

1. Preflight Plan (MUST)
2. Add interface + implementation
3. Register in Architecture init
4. Keep Model pure (state + minimal policy). **MUST NOT** do persistence I/O; use Utilities for storage/network.

### 2.3 Add a Command

1. Preflight Plan (MUST)
2. Create a Command that performs state change
3. Trigger from Controller/System (where appropriate)
4. Verify no direct state mutation from Controllers unless your `RULES.md` explicitly allows it

### 2.4 Add a Query

1. Preflight Plan (MUST)
2. Create Query for read-only access
3. Use from Controller/UI to fetch state
4. Verify query has no side effects

---

## 3. Create a UI View (Unity UGUI)

### Goal

Build UI using Unity native UGUI, while keeping all state changes strictly CQRS-compliant.

### Steps

1. **Preflight Plan (MUST)**
2. **Create a View `MonoBehaviour`**
   - Place it under your feature slice (recommended: `Views/`).
   - Implement `IController` and return `GameRootApp.Interface` from `GetArchitecture()`.
3. **Input → Command (write path)**
   - For any user input (`Button.onClick`, toggles, input fields), call `SendCommand(...)`.
   - All state mutations MUST happen inside Commands (see `RULES.md` → “2.3 CQRS”).
4. **UI refresh from Event / Bindable (notify path)**
   - Register UI updates via `BindableProperty.Register(...)` and/or `RegisterEvent<T>(...)`.
   - Ensure registrations are paired with unregistration using `OnEnable/OnDisable` OR QFramework auto helpers (`UnRegisterWhenDisabled`, `UnRegisterWhenGameObjectDestroyed`).
5. **Hard rule alignment**
   - This workflow MUST NOT introduce any QFramework extension toolkit UI APIs (see `RULES.md` → “4. Extension Toolkits Policy (BANNED)”).

---

## 4. Event Subscription (OnEnable / OnDisable)

### Goal

No event leaks. Subscriptions MUST be paired with unsubscription.

### Recommended patterns

#### Pattern A — Use QFramework auto-unregister helpers (preferred when available)

```csharp
private void OnEnable()
{
    this.RegisterEvent<SomeEvent>(OnSomeEvent)
        .UnRegisterWhenDisabled(gameObject);
}

// private void OnSomeEvent(SomeEvent e) { }
```

#### Pattern B — Manual unregistration (when you need explicit control)

```csharp
private IUnRegister _unregister;

private void OnEnable()
{
    _unregister = this.RegisterEvent<SomeEvent>(OnSomeEvent);
}

private void OnDisable()
{
    _unregister?.UnRegister();
    _unregister = null;
}

// private void OnSomeEvent(SomeEvent e) { }
```

### MUST checklist

* One subscription path per lifecycle (no duplicate subscribe on multiple enables)
* No “fire-and-forget” registrations
* Unregister in `OnDisable` / `OnDestroy` (or use QFramework auto helpers)

---

## 5. Bootstrap / Initialization Overrides (ProjectToolkitBootstrap)

### When to use

You need deterministic init order or project-side startup wiring without modifying `Assets/QFramework/**`.

### Steps

1. Preflight Plan (MUST)
2. Add/modify a bootstrap entry point (e.g., `ProjectToolkitBootstrap`)
3. Ensure it executes early enough (e.g., `BeforeSceneLoad` if needed)
4. Keep bootstrap responsibilities minimal:

   * environment setup
   * RootApp warmup / deterministic ordering
   * safe project-owned overrides (no framework edits)
5. Verify in Unity:

   * confirm bootstrap runs first
   * confirm desired systems behave as expected

### Output requirement

If any step requires touching a forbidden zone, output `MANUAL_ACTIONS` instead of making the edit.
