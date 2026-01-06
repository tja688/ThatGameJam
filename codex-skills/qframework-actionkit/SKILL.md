---
name: qframework-actionkit
description: Use QFramework ActionKit only when the user explicitly requests ActionKit for sequencing, delays, timers, or ActionKit APIs (Sequence, Delay, Parallel, Repeat). Do not use when the user wants Unity coroutines or avoids QFramework toolkits. Use alongside qframework-playbook and qframework-core-architecture in this repo.
---

# QFramework ActionKit

## Overview

Apply ActionKit for time-based sequencing and orchestration of gameplay actions.

## Workflow

1. Confirm explicit request for QFramework ActionKit; if the user wants Unity coroutines or avoids QFramework toolkits, stop and follow the override.
2. Apply `qframework-playbook` and `qframework-core-architecture` rules.
3. Use ActionKit building blocks (Delay, Sequence, Parallel, Repeat, DelayFrame) as documented.
4. Start actions with appropriate lifecycles (Component, GameObject, or global) and ensure they stop when the owner is destroyed.

## Verification

- Verify APIs in local docs first; mark unknowns with `UNVERIFIED` and provide `NEXT_SEARCH`.

## Resources

- `references/actionkit_rules.md` for source paths, key APIs, and lookup keywords.
