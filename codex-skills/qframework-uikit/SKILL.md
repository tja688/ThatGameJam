---
name: qframework-uikit
description: Use QFramework UIKit only when the user explicitly requests QFramework UI panel management or mentions UIKit, UIPanel, UIRoot, UIElement, or QFramework panel lifecycle APIs. Do not use when the user requests Unity built-in UI or says to avoid QFramework toolkits. Use alongside qframework-playbook and qframework-core-architecture in this repo.
---

# QFramework UIKit

## Overview

Apply QFramework UIKit for UI panel lifecycle, panel stack management, and codegen-driven bindings.

## Workflow

1. Confirm explicit request for QFramework UIKit; if the user wants Unity UI or avoids QFramework toolkits, stop and follow the override.
2. Apply `qframework-playbook` and `qframework-core-architecture` rules.
3. Use UIKit panel APIs for open/close/show/hide and panel stack navigation; follow UIPanel lifecycle semantics.
4. Keep UI logic in partial classes, not in generated designer files.
5. If using custom panel loaders, set `UIKit.Config.PanelLoaderPool` in `ProjectToolkitBootstrap` before first UIKit use.

## Verification

- Verify APIs in local docs first; mark unknowns with `UNVERIFIED` and provide `NEXT_SEARCH`.

## Resources

- `references/uikit_rules.md` for source paths, key APIs, and lookup keywords.
