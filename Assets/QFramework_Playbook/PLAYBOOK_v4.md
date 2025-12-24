# QFramework Project Playbook (v4)

This repository follows **QFramework Core CQRS + 4-Layer Architecture** with a **single Root Architecture** (`GameRootApp`).

This document is the **single entry contract** for both humans and AI agents.

---

## Table of Contents

- [1. How to use this Playbook](#1-how-to-use-this-playbook)
- [2. Rule precedence and conflict handling](#2-rule-precedence-and-conflict-handling)
- [3. Non-negotiables](#3-non-negotiables)
- [4. Project foundation](#4-project-foundation)
- [5. Folder strategy](#5-folder-strategy)
- [6. Architecture rules](#6-architecture-rules)
- [7. CQRS rules](#7-cqrs-rules)
- [8. Communication and lifecycle](#8-communication-and-lifecycle)
- [9. Singletons policy](#9-singletons-policy)
- [10. UI and code generation rules](#10-ui-and-code-generation-rules)
- [11. Toolkit initialization policy](#11-toolkit-initialization-policy)
- [12. Evidence and API verification policy](#12-evidence-and-api-verification-policy)
- [13. Task router](#13-task-router)
- [14. Playbook maintenance](#14-playbook-maintenance)

---

## 1. How to use this Playbook

### Required reading order (AI and humans)

1. **This file:** `PLAYBOOK.md`
2. **Workflows:** `RECIPES.md`
3. **Code skeletons:** `TEMPLATES.md`

### Required response format (AI)

When implementing gameplay code, the agent MUST output:

- `PLAN` (use the Preflight Plan in `RECIPES.md`)
- `CHANGES` (files + summary)
- `VERIFY` (how to test in Unity)
- `MANUAL_ACTIONS` (only if a forbidden zone must be edited)
- `FEATURE_NOTE` (write/update `Assets/Scripts/Features/[FeatureName]/FeatureNote.md`)
- `NOTES` (risks, assumptions, UNVERIFIED items)

---

## 2. Rule precedence and conflict handling

### 2.1 Precedence (highest → lowest)

1. **Project contract:** `PLAYBOOK.md` (this file) + `RECIPES.md` + `TEMPLATES.md`
2. **Local QFramework docs / local source code** (read-only)
3. **Online references** (only as supporting material, may not match this repo version)
4. **Freeform inference** (must be marked as `UNVERIFIED`)

### 2.2 Conflict handling (MUST)

If you find a conflict:

1. State the conflict precisely (which rules / sentences)
2. State which rule wins (based on precedence above)
3. Proceed using the winning rule
4. If the Playbook itself is wrong or unclear, propose a maintenance change (see [Playbook maintenance](#14-playbook-maintenance))

---

## 3. Non-negotiables

### 3.1 Forbidden zones (read-only)

Agents MUST NOT modify:

- `Assets/QFramework/**`
- Third-party packages / vendor code

If a change in a forbidden zone is required, the agent MUST output a `MANUAL_ACTIONS` checklist instead.

### 3.2 Evidence-first decisions

Any architectural decision that changes module placement, boundaries, or initialization MUST include a short evidence note:

- cite a rule section (from this Playbook), or
- cite a local QFramework doc path, or
- mark as `UNVERIFIED` + provide `NEXT_SEARCH`.

### 3.3 Logging and Debugging (MUST)

- **PROHIBITED:** Direct use of `UnityEngine.Debug.Log`, `LogWarning`, `LogError`.
- **REQUIRED:** Use `QFramework.LogKit` APIs (Static: `LogKit.I/W/E` or Fluent: `.LogInfo()/.LogWarning()/.LogError()`).
- **Rationale:** `LogKit` handles Unity Console stack trace navigation (click-to-jump) correctly via `[OnOpenAsset]` and supports global log level filtering (`LogKit.Level`).

---


### 3.4 Feature notes deliverable (MUST)

For every completed Feature (or any meaningful change to an existing Feature), agents MUST create/update a Feature-level note file:

- `Assets/Scripts/Features/[FeatureName]/FeatureNote.md`

This note is treated as the **public integration surface** of the Feature:

- **Purpose:** what the Feature does (1–3 lines)
- **Runtime wiring:** what Controllers must exist in the Unity Scene + what is registered in `GameRootApp`
- **Public API surface:** Events / Commands / Queries / Bindables exposed to other Features
- **Minimal usage examples:** 1–2 short code snippets for listeners/senders

If the Feature is not yet fully verified, the note MUST include an `UNVERIFIED` section with concrete next steps.

## 4. Project foundation

### 4.1 Single Root Architecture

- MUST use **one** Root Architecture:
  - `GameRootApp : Architecture<GameRootApp>`
- MUST NOT create multiple Root Architectures for individual features (except isolated demos).

### 4.2 Foundation idempotency (one-time setup)

Before creating or editing any “foundation” entry files, you MUST check if these already exist:

- `Assets/Scripts/Root/GameRootApp.cs`
- `Assets/Scripts/Root/ProjectToolkitBootstrap.cs`

If they already exist:

- MUST reuse them
- MUST NOT create a second entry point with the same responsibility

### 4.3 Foundation change gate (when you are allowed to touch Root/Bootstrap)

You MAY edit `GameRootApp` / `ProjectToolkitBootstrap` only for:

1. Registering a new **global** module used by multiple features (Model/System/Utility)
2. Adjusting startup determinism / initialization order
3. Configuring project-side toolkit initialization (without modifying `Assets/QFramework/**`)

You MUST NOT put feature-specific gameplay logic into Root/Bootstrap.
Feature logic MUST live under `Assets/Scripts/Features/...`.

---

## 5. Folder strategy

### 5.1 Vertical slice organization (MUST)

All gameplay features MUST be organized under:

- `Assets/Scripts/Features/[FeatureName]/...`

- Each Feature root MUST include: `FeatureNote.md`

A recommended baseline structure is in `TEMPLATES.md` (“Feature Skeleton”).

---

## 6. Architecture rules

This project uses QFramework’s 4-layer model:

1. **Controller (`IController`)**: Unity entry points (MonoBehaviour / UI). Input & presentation.
2. **System (`ISystem`)**: orchestration / cross-feature rules.
3. **Model (`IModel`)**: shared state + domain policy.
4. **Utility (`IUtility`)**: infrastructure (storage, network, SDK wrappers).

### 6.1 Dependency direction (MUST)

- Controllers may depend on Systems/Models/Utilities (via Architecture).
- Systems/Models MUST NOT hold references to Controllers.
- Downward notification MUST use Events / Bindables, not direct references.

### 6.2 Model purity (MUST)

- Models MUST NOT perform persistence I/O (PlayerPrefs / file / network).
- Persistence and external I/O MUST be implemented in Utilities.

---

## 7. CQRS rules

### 7.1 State mutation path (MUST)

- Any state mutation MUST go through a **Command**.
- Commands and Queries MUST be **stateless objects**.

### 7.2 Prohibited writes (MUST NOT)

- Controllers and Systems MUST NOT directly set Model state (e.g., `BindableProperty.Value = ...`).
- Exception: Model initialization inside `OnInit()` may set initial values.

### 7.3 Reads (SHOULD)

- Reads SHOULD go through Queries.
- Direct reads are allowed only when it clearly stays read-only and does not leak layering.

---

## 8. Communication and lifecycle

### 8.1 Event symmetry (MUST)

Every subscription MUST have a corresponding unsubscription.

Preferred patterns:

- QFramework auto-unregister helpers (`UnRegisterWhenDisabled`, `UnRegisterWhenGameObjectDestroyed`)
- Otherwise store `IUnRegister` and unregister in `OnDisable` / `OnDestroy`.

### 8.2 No leaks (MUST)

- Avoid long-lived subscriptions from short-lived objects.
- When in doubt, bind subscriptions to the GameObject lifecycle.

---

## 9. Singletons policy

- PROHIBITED: global service-locator singletons to bypass Architecture boundaries.
- ALLOWED: low-level infrastructure singletons **inside Utilities only** (e.g., `SingletonKit` for caching).

---

## 10. UI and code generation rules

### 10.1 UIKit access rules (MUST)

- Prefer generated binding fields (Designer files).
- MUST NOT use `transform.Find` for static UI elements.

### 10.2 Codegen protection (MUST)

- MUST NOT write business logic in:
  - `*.Designer.cs`
  - `*.Generated.cs`
- Write logic in the partial class (`[Panel].cs`).

---

## 11. Toolkit initialization policy

This section applies only if your project uses these toolkits.

### 11.1 UIKit / AudioKit loader pool overrides (MUST, if overriding)

If you require a custom `PanelLoaderPool` or `AudioLoaderPool` (not the default ResKit pool):

- MUST set overrides **before first use** of UIKit/AudioKit APIs
- MUST do it in `ProjectToolkitBootstrap` using:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
```

### 11.2 ResKit initialization (MUST, if using ResKit on startup)

If your project loads assets/scenes during startup using ResKit:

- MUST call `ResKit.Init()` (or `ResKit.InitAsync()` when required) before any ResKit load happens.

---

## 12. Evidence and API verification policy

When implementation touches any QFramework API (class/method/extension/config):

### 12.1 Verification priority (highest → lowest)

1. **Local API reference (authoritative for this repo):**
   - `Assets/QFramework/QFramework_Docs_API.md`
2. QFramework source (read-only):
   - `Assets/QFramework/Framework/Scripts/...`
3. Online references (must warn about version mismatch)

### 12.2 If you cannot verify

You MUST mark the usage as:

- `UNVERIFIED:` why it cannot be confirmed
- `NEXT_SEARCH:` how to confirm inside this repo (file paths / keywords)

### 12.3 Useful source entry points (read-only)

These are common symbols to locate in QFramework source:

- `Architecture<T>`
- `IController`, `IModel`, `ISystem`, `IUtility`
- `AbstractCommand`, `AbstractQuery`
- `TypeEventSystem`
- `UnRegisterExtension` (`UnRegisterWhenGameObjectDestroyed`, etc.)

---

## 13. Task router

Use this section to quickly jump to the right workflow/template.

### Add a new gameplay feature

- Workflow: `RECIPES.md` → “Create a New Feature”
- Templates: `TEMPLATES.md` → “Feature Skeleton”, “RootApp Registration Snippet”

### Extend an existing feature

- Workflow: `RECIPES.md` → “Adding System / Model / Command / Query”
- Rules: this file → [Architecture rules](#6-architecture-rules) + [CQRS rules](#7-cqrs-rules)

### Create UI (UIKit panel)

- Workflow: `RECIPES.md` → “Create a UI Panel (UIKit)”
- Templates: `TEMPLATES.md` → “UIKit Panel Logic Skeleton”

### Events / messaging / lifecycle

- Workflow: `RECIPES.md` → “Event Subscription”
- Rules: this file → [Communication and lifecycle](#8-communication-and-lifecycle)

### Bootstrap / initialization order

- Workflow: `RECIPES.md` → “Bootstrap / Initialization Overrides”
- Templates: `TEMPLATES.md` → “ProjectToolkitBootstrap”

---

## 14. Playbook maintenance

Only use this section when the task is to change the Playbook itself (not gameplay code).

### 14.1 Allowed write targets

- `PLAYBOOK.md`
- `RECIPES.md`
- `TEMPLATES.md`

### 14.2 Evidence policy

Any new **MUST/MUST NOT/REQUIRED** rule MUST include:

- `Source:` (local path + symbol, or an official doc link), OR
- `UNVERIFIED:` + `NEXT_SEARCH:` (1–3 concrete verification steps)

### 14.3 Maintenance workflow (MUST follow)

1. Read the file(s) end-to-end.
2. Output a maintenance plan:
   - Intent (1 sentence)
   - Files to change
   - Rule impact (new MUST/MUST NOT? evidence or UNVERIFIED)
   - Compatibility risk (does it break workflows/templates?)
3. Implement with consistent style (prefer whole-section replacements).
4. Self-check:
   - no contradictions across Playbook files
   - links valid
   - every new MUST/MUST NOT has evidence or UNVERIFIED markers
5. Report:
   - files changed
   - summary
   - UNVERIFIED list + NEXT_SEARCH list

### 14.4 Changelog

Add newest entries at the top.

- **YYYY-MM-DD**
  - Summary:
  - Files:
  - Notes:
