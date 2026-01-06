---
name: qframework-reskit
description: Use QFramework ResKit only when the user explicitly requests QFramework ResKit or ResLoader-based asset loading, AssetBundle workflows, or ResKit APIs. Do not use when the user requests Unity built-in asset loading or avoids QFramework toolkits. Use alongside qframework-playbook and qframework-core-architecture in this repo.
---

# QFramework ResKit

## Overview

Apply ResKit and ResLoader patterns for resource loading, scene loading, and loader lifecycle management.

## Workflow

1. Confirm explicit request for QFramework ResKit; if the user wants Unity built-in loading or avoids QFramework toolkits, stop and follow the override.
2. Apply `qframework-playbook` and `qframework-core-architecture` rules.
3. Initialize ResKit before any ResKit loads when required by the scenario.
4. Allocate a ResLoader per loading unit and recycle it when the unit is done.
5. Use ResLoader sync/async load APIs and scene load APIs as documented.

## Verification

- Verify APIs in local docs first; mark unknowns with `UNVERIFIED` and provide `NEXT_SEARCH`.

## Resources

- `references/reskit_rules.md` for source paths, key APIs, and lookup keywords.
