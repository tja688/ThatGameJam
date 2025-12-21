# PLAYBOOK_MAINTENANCE.md — Playbook Maintenance & Evidence Contract

### Attention, if you have not read AGENTS.md yet, please read AGENTS.md before reading this document

This file governs how the Playbook itself is maintained.
Use this file only when the task is: **edit / refine / extend Playbook docs**.

If the task is to implement game features, follow:
- AGENTS.md (entry contract)
- RULES.md / RECIPES.md / TEMPLATES.md (execution)

---

## 0. Scope

### Allowed write targets (Playbook maintenance)
- Playbook directory files only:
  - `Assets/Scripts/QFramework_Playbook/AGENTS.md`
  - `Assets/Scripts/QFramework_Playbook/RULES.md`
  - `Assets/Scripts/QFramework_Playbook/RECIPES.md`
  - `Assets/Scripts/QFramework_Playbook/TEMPLATES.md`
  - `Assets/Scripts/QFramework_Playbook/INDEX_MAP_QFRAMEWORK_LOCATIONS.md`
  - `Assets/Scripts/QFramework_Playbook/INDEX_TASKS.md`
  - `Assets/Scripts/QFramework_Playbook/PLAYBOOK_MAINTENANCE.md`


### Forbidden targets (even during maintenance)
- `Assets/QFramework/**` is **read-only**
- Any third-party packages / vendor code are read-only unless explicitly stated by the user.

---

## 1. Evidence Policy

### 1.1 Evidence priority (highest → lowest)
1. Project-level contracts (RULES/RECIPES/TEMPLATES) already approved in this repo
2. Official QFramework docs / examples / tutorials
3. QFramework source code comments and signatures
4. Inference / best-practice reasoning (MUST be marked `UNVERIFIED`)

### 1.2 Every rule MUST be evidence-backed or explicitly marked
When adding or changing a rule that uses MUST / MUST NOT / REQUIRED:
- Provide a `Source:` line, OR
- Mark it as `UNVERIFIED:` and provide `NEXT_SEARCH:` pointers.

**Required format**
- `Source:` one of:
  - URL (docs / repo)
  - Local file path + symbol name
  - Screenshot filename if evidence is from images
- `UNVERIFIED:` explain why evidence is missing
- `NEXT_SEARCH:` list 1–3 concrete queries or code locations to verify later

Example:
- MUST: X
  - Source: QFramework docs “xxx” + path `Assets/QFramework/...` class `Foo`

Example (unverified):
- RECOMMENDED: Y
  - UNVERIFIED: Not found in official docs; inferred from usage patterns.
  - NEXT_SEARCH: "QFramework Y best practice", search `Assets/QFramework/**` for "Y"

---

## 2. Maintenance Workflow (MUST follow)

### Step 1 — Read
- Read the file(s) you will modify end-to-end.
- Identify if there are conflicts with higher-priority contracts.

### Step 2 — Plan (output before editing)
Output:
- Change intent (1 sentence)
- Files to change (full paths)
- Rule impact:
  - New MUST/MUST NOT? (Yes/No)
  - If Yes: evidence `Source:` or `UNVERIFIED` + `NEXT_SEARCH`
- Compatibility risk:
  - Does this change break existing recipes/templates? (Yes/No)
  - If Yes: list what you will update together

### Step 3 — Implement
- Apply changes with consistent style.
- Prefer “replace whole section” over scattered micro-edits.

### Step 4 — Self-check
- No contradictions across files (AGENTS/RULES/RECIPES/TEMPLATES/INDEX)
- Navigation links valid (relative links)
- No duplicate or conflicting terminology
- Every new MUST/MUST NOT has `Source:` or is marked `UNVERIFIED` with `NEXT_SEARCH`

### Step 5 — Report
Output:
- Files changed
- Summary of edits
- `UNVERIFIED` list (if any) + `NEXT_SEARCH` list

---

## 3. Deprecation & Versioning

- Do not delete old rules silently.
- If a rule becomes obsolete:
  - Keep it, mark as `DEPRECATED`, explain why, and point to the replacement.
- When a rule changes behavior, add a short entry to a changelog section in the same file.

---

## 4. Changelog (Playbook maintenance only)

Add newest entries at the top.

### YYYY-MM-DD
- Summary:
- Files:
- Notes:
