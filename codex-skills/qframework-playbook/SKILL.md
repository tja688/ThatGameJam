---
name: qframework-playbook
description: Enforce the ThatGameJam project Playbook and response contract for any gameplay or feature work in this repo, or when the user asks to organize code with QFramework/Playbook/GameRootApp. Apply forbidden-zone protections, logging rules, folder layout, FeatureNote requirements, evidence-first verification, and toolkit selection overrides (Unity built-in by default; QFramework toolkits only on explicit request).
---

# QFramework Playbook

## Overview

Apply the project's non-negotiable rules and response format whenever work touches gameplay/features in this repo.

## Workflow

1. Read the Playbook stack in order:
   - `Assets/Scripts/QFramework_Playbook/PLAYBOOK_v4.md`
   - `Assets/Scripts/QFramework_Playbook/RECIPES_v4.md`
   - `Assets/Scripts/QFramework_Playbook/TEMPLATES_v4.md`
2. Apply `qframework-core-architecture` rules whenever QFramework architecture is in scope.
3. Enforce non-negotiables:
   - Never edit `Assets/QFramework/**` or third-party packages; output `MANUAL_ACTIONS` if a change is required.
   - Use `QFramework.LogKit` only; never `UnityEngine.Debug.*`.
   - Keep a single root architecture (`GameRootApp`) and avoid feature logic in Root/Bootstrap.
   - Place gameplay work under `Assets/Scripts/Features/[FeatureName]/...` and update `FeatureNote.md`.
4. Follow the required response contract for gameplay work: `PLAN`, `CHANGES`, `VERIFY`, `MANUAL_ACTIONS` (if needed), `FEATURE_NOTE`, `NOTES`.
5. Apply toolkit selection policy:
   - Default to Unity built-in solutions unless the user explicitly requests a QFramework toolkit.
   - If the user says to avoid a QFramework toolkit or specifies an external library, honor it and label the choice as `TOOLKIT_OVERRIDE`.
   - If the request conflicts (for example, "use AudioKit" plus "do not use QFramework audio"), ask for clarification before coding.
6. Verify APIs and architectural decisions with local sources first; mark anything unverified with `UNVERIFIED` and provide `NEXT_SEARCH`.

## Evidence and Conflicts

- Use precedence: Playbook stack > local QFramework docs/source > online refs > inference.
- If rules conflict, state the conflict, select the winner by precedence, and continue.

## Resources

- `references/playbook_rules.md` for condensed rules, sources, and search keywords.
