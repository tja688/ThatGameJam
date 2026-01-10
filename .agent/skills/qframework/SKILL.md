---
name: qframework
description: QFramework (QF) architecture, CQRS, and toolkit usage for Unity work. Use when asked to implement new features, refactor existing code, or analyze code in QF terms (Architecture, Controller, System, Model, Utility, Command, Query, Event, BindableProperty), or when working in ThatGameJam where QF is the default unless the user explicitly opts out.
---

# QFramework

## Overview

Apply QFramework architecture and QF-first toolkit usage for coding, refactoring, and analysis. Use only the bundled references in this skill (no external path dependencies).

## Defaults and constraints

- Assume QF is required unless the user explicitly opts out or mandates another tech choice.
- Prefer QFramework toolkits/APIs and best practices. If QF is not used, explain the reason and note a QF alternative.
- Do not pause to ask about tech selection. Choose the best path, proceed, and document the rationale.
- Treat QFramework vendor code as read-only; do not edit framework/toolkit sources.
- Use LogKit for logging; never use UnityEngine.Debug.*.

## Workflow

1. Identify scope (new feature, refactor, analysis) and map responsibilities to QF layers.
2. Load the minimum needed references from this skill:
   - references/QFramework/PLAYBOOK_v4_CN.md (project rules)
   - references/QFramework/TEMPLATES_v4_CN.md (code skeletons)
   - references/QFramework/QFramework_Docs_API.md (API reference)
   - references/QFramework/QFramework_Docs_User_Manual.md (manual)
   - references/QFramework/Framework/Scripts/QFramework.cs (core architecture and CQRS types)
   - references/QFramework/Toolkits/** (toolkit implementation details)
3. Apply architecture rules and CQRS invariants before writing or refactoring.
4. Verify any QFramework API usage against the local references. If not found, mark it UNVERIFIED and include NEXT_SEARCH hints.

## Architecture rules to enforce

- Single root architecture: GameRootApp : Architecture<GameRootApp>.
- Root/Bootstrap changes only for global module registration or init order, never feature logic.
- Commands and Queries are stateless objects.
- Controllers and Systems must not mutate Model state directly. Use Commands for state changes.
- State change notifications use Events or BindableProperty.
- Lower layers do not reference higher layers; upward signals use Events.
- Every event subscription must be unregistered; prefer UnRegisterWhenDisabled / UnRegisterWhenGameObjectDestroyed.
- Avoid service-locator singletons across layers; allow only Utility-layer infrastructure singletons.

## QF-first toolkit selection

- Use QF toolkits by default (AudioKit, ResKit, UIKit, ActionKit, BindableKit, etc.).
- If a non-QF approach is necessary, explain the tradeoff and include a "QF alternative" note.

## Analysis/report expectations

When asked to analyze existing code, produce a QF-style report with at least:
- Architecture map (component -> layer)
- CQRS paths (state change flows)
- Event/subscription hygiene
- Toolkit usage and possible QF substitutions
- Findings/violations with evidence references
- Recommended refactors or fixes

## Search hints

Use rg within references/QFramework to locate APIs or rules:
- rg -n "Architecture<|IController|ISystem|IModel|IUtility|ICommand|IQuery" references/QFramework/Framework/Scripts/QFramework.cs
- rg -n "LogKit|Bind" references/QFramework/QFramework_Docs_API.md
- rg -n "Playbook|Single Root|Command" references/QFramework/PLAYBOOK_v4_CN.md
- rg -n "AudioKit|ResKit|UIKit|ActionKit" references/QFramework
