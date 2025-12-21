# RECIPES — Executable Workflows (v3)

> Read `PLAYBOOK.md` first.  
> This document contains repeatable workflows. Agents MUST use the Preflight Plan before implementing.

---

## 0. Agent Preflight Plan (MUST)

Before writing code, the agent MUST output a `PLAN` using this exact structure:

### PLAN

- **Intent (1 sentence):**
- **Placement decision:**
  - Primary: Controller / Model / System / Utility / Command / Query / Event
  - Rationale: (1–3 bullets; cite relevant sections in `PLAYBOOK.md` when possible)
- **Files to add/modify (full paths):**
- **Foundation impact (MUST):** None / Modify `GameRootApp` / Modify `ProjectToolkitBootstrap`
- **Cross-layer interaction map:**
  - **State change path:** (Commands → who triggers → what changes)
  - **Notify path:** (Event/Bindable → who listens)
  - **Read path:** (Query or direct read → who reads)
- **Lifecycle & cleanup:**
  - Subscription/unsubscription plan (OnEnable/OnDisable or QF auto helpers)
- **Verification plan:**
  - How to trigger in Unity
  - What logs/visible outcomes prove success
- **Self-check (MUST):**
  - **Write-path audit:** list every “write to state” point (file path + symbol/method + 1 sentence)
  - **CQRS proof:** for each write point, explain why it complies with `PLAYBOOK.md` → “CQRS rules”
  - **Forbidden patterns check:** confirm you avoided:
    - writing Model state directly in a System/Controller (e.g., `BindableProperty.Value = ...`)
    - doing PlayerPrefs/file/network I/O inside a Model
- **Risks / assumptions:**
  - anything uncertain or version-dependent (mark as `UNVERIFIED` + add `NEXT_SEARCH` if needed)

Agents may output PLAN and Implementation in a single response to maintain workflow continuity:

- `CHANGES` (files + summary)
- `VERIFY` (steps)
- `MANUAL_ACTIONS` (only if forbidden zones require edits)
- `NOTES` (risks, assumptions, unverified points)

---

## 1. Create a New Feature

### When to use

You are introducing a new vertical slice feature.

### Steps

1. **Preflight Plan (MUST)**  
   Use section `0. Agent Preflight Plan`.

2. **Create directory**

- `Assets/Scripts/Features/[FeatureName]/`

3. **Create layers (recommended baseline)**

- Model: `I[Feature]Model` + `[Feature]Model`
- System: `I[Feature]System` + `[Feature]System`
- Controller: a MonoBehaviour implementing `IController`

4. **Wire into Architecture**

- Register new Model/System/Utility in the Root Architecture init (`GameRootApp.Init()`).
- Controllers MUST return the correct architecture via `GetArchitecture()`.

5. **Implement interactions using CQRS**

- All state mutation MUST go through Commands.
- Reads SHOULD go through Queries (or direct read only if it is clearly safe).
- Notifications via Events or Bindables.

6. **Report**

- Output `CHANGES`, `VERIFY`, `NOTES`.

---

## 2. Adding System / Model / Command / Query

### 2.1 Add a System

1. Preflight Plan (MUST)
2. Add interface + implementation
3. Register in `GameRootApp.Init()`
4. Confirm no forbidden dependencies (see `PLAYBOOK.md` → “Architecture rules”)

### 2.2 Add a Model

1. Preflight Plan (MUST)
2. Add interface + implementation
3. Register in `GameRootApp.Init()`
4. Keep Model pure:
   - MUST NOT do persistence I/O
   - use a Utility for storage/network

### 2.3 Add a Command

1. Preflight Plan (MUST)
2. Create a Command that performs state change
3. Trigger from Controller/System (as appropriate)
4. Verify no direct state mutation from Controllers/Systems

### 2.4 Add a Query

1. Preflight Plan (MUST)
2. Create Query for read-only access
3. Use from Controller/UI to fetch state
4. Verify query has no side effects

---

## 3. Create a UI Panel (UIKit)

### Steps

1. Preflight Plan (MUST)
2. Create the panel following the UIKit pattern (see `TEMPLATES.md`)
3. Bind UI events → Commands (preferred) or Systems
4. Ensure lifecycle cleanup is correct

### Notes

- Prefer explicit open/close paths.
- Avoid long-lived subscriptions without an explicit unregister strategy.

---

## 4. Event Subscription (OnEnable / OnDisable)

### Goal

No event leaks. Subscriptions MUST be paired with unsubscription.

### Pattern A — QFramework auto-unregister helpers (preferred)

```csharp
private void OnEnable()
{
    this.RegisterEvent<SomeEvent>(OnSomeEvent)
        .UnRegisterWhenDisabled(gameObject);
}

private void OnSomeEvent(SomeEvent e)
{
}
```

### Pattern B — Manual unregistration (when you need explicit control)

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

private void OnSomeEvent(SomeEvent e)
{
}
```

### MUST checklist

- One subscription path per lifecycle (avoid duplicate subscriptions on re-enable)
- No “fire-and-forget” registrations
- Unregister in `OnDisable` / `OnDestroy` (or use auto helpers)

---

## 5. Bootstrap / Initialization Overrides (ProjectToolkitBootstrap)

### When to use

You need deterministic init order or project-side startup wiring without modifying `Assets/QFramework/**`.

### Steps

1. Preflight Plan (MUST)
2. Add/modify a bootstrap entry point (e.g., `ProjectToolkitBootstrap`)
3. Ensure it executes early enough (`BeforeSceneLoad` if needed)
4. Keep bootstrap responsibilities minimal:
   - environment setup
   - toolkit configuration
   - safe overrides
5. Verify in Unity:
   - confirm bootstrap runs first
   - confirm desired systems behave as expected

### Output requirement

If any step requires touching a forbidden zone, output `MANUAL_ACTIONS` instead of making the edit.
