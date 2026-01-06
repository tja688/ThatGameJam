---
name: qframework-corekit-utils
description: Use QFramework CoreKit utilities when the user explicitly requests LogKit, PoolKit, SingletonKit, BindableProperty, TypeEventSystem, EasyEvent, or related QFramework utility APIs. Apply when QFramework utilities are needed in this repo; otherwise prefer Unity built-in equivalents. Use alongside qframework-playbook and qframework-core-architecture.
---

# QFramework CoreKit Utils

## Overview

Apply QFramework utility modules for logging, pooling, singleton patterns, and event/bindable helpers.

## Workflow

1. Confirm explicit request for QFramework utility APIs; otherwise follow Unity built-in equivalents.
2. Apply `qframework-playbook` and `qframework-core-architecture` rules.
3. Use LogKit for logging; never use UnityEngine.Debug APIs.
4. Use PoolKit for pooled allocations, SingletonKit for singleton patterns, and BindableProperty/TypeEventSystem/EasyEvent for event-driven data flow when requested.
5. Ensure event subscriptions are unregistered; prefer QFramework auto-unregister helpers where applicable.

## Verification

- Verify APIs in local docs first; mark unknowns with `UNVERIFIED` and provide `NEXT_SEARCH`.

## Resources

- `references/corekit_utils_rules.md` for sources, key APIs, and lookup keywords.
