---
name: qframework-codegen
description: Use QFramework CodeGenKit only when the user explicitly requests QFramework code generation, Designer files, or CodeGenKit workflows for UI/GameObject binding. Do not use when the user requests Unity built-in workflows or avoids QFramework toolkits. Use alongside qframework-playbook and qframework-core-architecture in this repo.
---

# QFramework CodeGenKit

## Overview

Apply CodeGenKit workflows to generate partial classes and designer bindings for UI/GameObject components.

## Workflow

1. Confirm explicit request for QFramework CodeGenKit or Designer file workflows; otherwise follow Unity built-in flow.
2. Apply `qframework-playbook` and `qframework-core-architecture` rules.
3. Keep logic in the non-Designer partial class; never edit `*.Designer.cs` or other generated files.
4. Follow CodeGenKit settings for namespace and output directories when required.
5. If the task involves UIKit panels, coordinate with `qframework-uikit` and keep panel-prefab conventions from local docs.

## Verification

- Verify CodeGenKit behavior in local docs; mark unknowns with `UNVERIFIED` and provide `NEXT_SEARCH`.

## Resources

- `references/codegen_rules.md` for sources, key workflows, and lookup keywords.
