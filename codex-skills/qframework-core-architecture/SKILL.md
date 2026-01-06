---
name: qframework-core-architecture
description: Apply QFramework core architecture and CQRS rules when the user mentions QFramework/QF or asks about Architecture, MVC, CQRS, Command, Query, Model, System, Utility, IController, or Architecture (generic type). Use alongside qframework-playbook for this repo.
---

# QFramework Core Architecture

## Overview

Use QFramework's 4-layer architecture and CQRS patterns to structure code and communication.

## Core Rules

- Use four layers: Controller (IController), System (ISystem), Model (IModel), Utility (IUtility).
- Enforce dependency direction: Controllers may call Systems/Models/Utilities; lower layers never reference Controllers; upward signals use Events or BindableProperty.
- Route state changes through Commands only; keep Commands and Queries stateless.
- Prefer Queries for reads; allow direct reads only when clearly read-only.
- Keep Models pure: no persistence or external I/O; put I/O in Utilities.

## Communication and Lifecycle

- Send state-change notifications via Events or BindableProperty.
- Ensure every subscription is unregistered; prefer QFramework auto-unregister helpers.

## Verification

- Verify API details against local QFramework docs/source; mark unverified usage with `UNVERIFIED` and include `NEXT_SEARCH`.

## Resources

- `references/architecture_rules.md` for sources, key APIs, and lookup keywords.
