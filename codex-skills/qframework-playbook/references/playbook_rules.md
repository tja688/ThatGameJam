# Playbook Rules Summary

Sources:
- `Assets/Scripts/QFramework_Playbook/PLAYBOOK_v4.md`
- `Assets/Scripts/QFramework_Playbook/RECIPES_v4.md`
- `Assets/Scripts/QFramework_Playbook/TEMPLATES_v4.md`

Non-negotiables:
- Forbidden zones: `Assets/QFramework/**` and third-party packages (use `MANUAL_ACTIONS` if edits are required).
- Logging: use `QFramework.LogKit` only; never `UnityEngine.Debug.*`.
- Single root: `GameRootApp : Architecture<GameRootApp>`.
- Feature notes: `Assets/Scripts/Features/[FeatureName]/FeatureNote.md`.

Folder and foundation rules:
- Place gameplay features under `Assets/Scripts/Features/[FeatureName]/...`.
- Edit `GameRootApp` / `ProjectToolkitBootstrap` only for global module registration or init order.
- Avoid feature logic inside Root/Bootstrap.

Response contract:
- When implementing gameplay work, output: `PLAN`, `CHANGES`, `VERIFY`, `MANUAL_ACTIONS` (if needed), `FEATURE_NOTE`, `NOTES`.

Evidence and precedence:
- Playbook stack > local QFramework docs/source > online refs > inference.
- Mark unknowns as `UNVERIFIED` and add `NEXT_SEARCH`.

Toolkit selection override (project-specific):
- Default to Unity built-in solutions.
- Use QFramework toolkits only when explicitly requested by the user.
- Honor explicit "do not use QFramework toolkit" or external library requests.

Search hints:
- "Non-negotiables"
- "Forbidden zones"
- "Feature notes deliverable"
- "Single Root Architecture"
- "Toolkit initialization policy"
- "Evidence and API verification policy"
