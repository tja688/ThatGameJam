# INDEX_TASKS.md — Task Navigation (Fast Router)

### Attention, if you have not read AGENTS.md yet, please read AGENTS.md before reading this document

This file is a quick “where do I go” router.
For full execution details, follow the linked documents.

---

## If you want to add a new gameplay/business feature
Go to:
- `RECIPES.md` → “Create a New Feature”
- `TEMPLATES.md` → Feature skeletons / RootApp wiring

Expected output from the Agent:
- Preflight Plan (RECIPES “Agent Preflight Plan”)
- File list (paths)
- Placement decision (Controller/Model/System/Utility/Command/Query)

---

## If you want to add logic to an existing feature
Go to:
- `RECIPES.md` → “Adding System / Model / Command / Query”
- `RULES.md` → Layer boundaries & dependency rules

---

## If you want UI (Unity UGUI)
Go to:
- `RECIPES.md` → “Create a UI View (Unity UGUI)”
- `TEMPLATES.md` → Unity UGUI View template

---

## If you want events / messaging rules
Go to:
- `RECIPES.md` → “Event Subscription (OnEnable / OnDisable)”
- `RULES.md` → “No leaks” / lifecycle / registration rules

---

## If you need “where should this code live?”
Go to:
- `RULES.md` → Architecture layering & dependency constraints
- `RECIPES.md` → “Agent Preflight Plan” (placement decision)

---

## If you need bootstrap / initialization order
Go to:
- `RECIPES.md` → “Bootstrap / Initialization Overrides”
- `TEMPLATES.md` → ProjectToolkitBootstrap (or bootstrap pattern)

---

## If you need to change the Playbook itself
Go to:
- `PLAYBOOK_MAINTENANCE.md` (evidence & maintenance workflow)
