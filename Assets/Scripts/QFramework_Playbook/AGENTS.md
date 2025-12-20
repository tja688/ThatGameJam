# AGENTS.md — AI Agent Participation Contract (Project Standard)

This is the **single entry contract** for AI working in this repo.

---

## 0. Rule Priority Strategy (Precedence)

When developing for this project, Agents MUST follow this priority:

1) **Project Rules (`RULES.md`)** — absolute
2) **Project Workflows/Templates (`RECIPES.md` / `TEMPLATES.md`)** — standard procedures & boilerplates
3) **Official QFramework docs/source** — only when project contracts are silent
4) **Freeform development** — only if none of the above cover it, and MUST include reasoning + risks

### Conflict Resolution Mechanism (MUST)
If a conflict is found:
1) State the conflict precisely
2) State which rule wins (based on priority above)
3) Proceed with the winning rule
4) If needed, propose a Playbook maintenance change (do NOT silently “average” rules)

---

## 1. Forbidden Zones (Modify Prohibited)

Agents MUST NOT modify the following unless the user explicitly requests:
- `Assets/QFramework/**` (read-only)
- Third-party packages / vendor code

If a change is required in a forbidden zone, the Agent MUST output a `MANUAL_ACTIONS` list for the user.

Additional hard policy:
- QFramework extension toolkits / solution components are **BANNED** by `RULES.md` §4; do not propose or use them unless the user explicitly requests an exception.

---

## 2. Quick Task Index (Start Here)

If you don’t know where to begin, open:
- `INDEX_TASKS.md` — fast router by intent

---

## 3. Navigation Map

| File | Purpose |
|---|---|
| [`RULES.md`](./RULES.md) | Hard constraints (MUST/MUST NOT), architecture boundaries, dependency rules. |
| [`RECIPES.md`](./RECIPES.md) | Executable workflows. Includes the mandatory Preflight Plan format. |
| [`TEMPLATES.md`](./TEMPLATES.md) | Copy-pasteable code skeletons. |
| [`INDEX_TASKS.md`](./INDEX_TASKS.md) | Task navigation router (“where do I go”). |
| [`INDEX_MAP_QFRAMEWORK_LOCATIONS.md`](./INDEX_MAP_QFRAMEWORK_LOCATIONS.md) | Evidence/source anchors (QFramework docs/source pointers). |
| [`PLAYBOOK_MAINTENANCE.md`](./PLAYBOOK_MAINTENANCE.md) | How to change this Playbook (evidence/UNVERIFIED/next search). |

---

## 4. Development Workflow (Required Output Contract)

Agents MUST follow this loop:

1) Read `RULES.md` + relevant `RECIPES.md` section
2) Output **Preflight Plan** (see `RECIPES.md` → “Agent Preflight Plan (MUST)”)
3) Implement with correct placements (respect layer boundaries)
4) Self-check
5) Report with files changed + how to verify in Unity

### Minimum required headings in the Agent response
- `PLAN` (Preflight Plan output)
- `CHANGES` (file list + what changed)
- `VERIFY` (how to test in Unity)
- `MANUAL_ACTIONS` (only if forbidden zones require edits)
- `NOTES` (risks, assumptions, unverified points)

---

## 5. Maintenance Status: Frozen (with a safe maintenance lane)

This Playbook is in **Frozen Mode** for day-to-day development:
- Do not casually refactor contracts.
- Only update Playbook when you must resolve contradictions or add missing coverage.

When Playbook changes are required, follow:
- `PLAYBOOK_MAINTENANCE.md`

---

## 补充：API 查证规则（必须）

当实现中涉及任何 QFramework API（类/方法/扩展/配置项）时，AI 必须在输出中明确给出“查证来源”：

优先查证顺序：
1) `INDEX_MAP_QFRAMEWORK_LOCATIONS.md` 中记录的 **项目内 API Reference**
2) QFramework 源码（只读）
3) 互联网资料（仅作补充，且必须声明可能与本项目版本不一致）

如果无法查证，必须标记为 `UNVERIFIED` 并给出 `NEXT_SEARCH`（如何在项目内定位该 API 的建议）。
