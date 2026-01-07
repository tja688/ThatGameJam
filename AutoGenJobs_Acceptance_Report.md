# AutoGenJobs System Acceptance Report

Scope: Assets/Editor/AutoGenJobs (queue/runner, guards, commands, and tests).

## Overall assessment
Status: PARTIAL PASS (high-risk items found that can cause race/partial-read issues and prefab context leakage).

## High-risk checkpoints

### 1) Half-write and race conditions
Verdict: PARTIAL FAIL
- Inbox scan only targets `*.job.json`, so `.pending` files are ignored.
- The runner reads the job file before moving it to `working/`. If a producer writes directly to `.job.json` (non-atomic), a partial read can parse-fail and the job is moved to `dead/`.
- Move-to-working is attempted after preconditions; if move fails, the job is skipped (good for multi-runner contention).
Evidence:
- `Assets/Editor/AutoGenJobs/Core/JobQueue.cs`: `GetPendingJobs()` uses `*.job.json`.
- `Assets/Editor/AutoGenJobs/Core/JobRunner.cs`: `File.ReadAllText(jobFilePath)` happens before `_queue.MoveToWorking(...)`.

### 2) WAITING logic and results
Verdict: MOSTLY PASS (one caveat)
- When preconditions fail with WAITING, the job stays in inbox and a WAITING `result.json` is written; it is not moved to `dead/`.
- Caveat: `Tick()` exits early when Unity is compiling/updating, so WAITING results are not written during those frames (external retry logic sees no new WAITING until compilation ends).
Evidence:
- `Assets/Editor/AutoGenJobs/Core/JobRunner.cs`: WAITING branch writes result and returns without moving.
- `Assets/Editor/AutoGenJobs/Core/JobRunner.cs`: early return if compiling/updating before scanning jobs.

### 3) Domain reload / crash recovery
Verdict: PASS (minimal strategy), with risk
- On initialization, all `working/` jobs are moved back to inbox. This prevents permanent loss after crashes/reloads.
- Risk: no timeout/state check. A partially executed job will re-run on startup and may duplicate side effects.
Evidence:
- `Assets/Editor/AutoGenJobs/Core/JobRunner.cs`: `RecoverWorkingJobs()` on init.
- `Assets/Editor/AutoGenJobs/Core/JobQueue.cs`: `RecoverWorkingJobs()` moves all working jobs back to inbox.

### 4) Prefab nested edits ($prefabRoot and context isolation)
Verdict: PARTIAL PASS
- `CreateOrEditPrefab` uses `PrefabUtility.LoadPrefabContents`, `SaveAsPrefabAsset`, and `UnloadPrefabContents` (correct lifecycle).
- `$prefabRoot` is injected into the shared `CommandContext` so nested edits can target it.
- No context isolation: nested edits share the same variable table; nested outputs persist and can overwrite outer vars.
- Target resolution does not default to prefab root. Commands that rely on scene path (or use `GameObject.Find`) can hit the active scene instead of the prefab contents scene.
- `CreateGameObject` uses `parentPath` + `FindGameObjectByPath()` (scene-based), so nested prefab edits can create or parent in the active scene, not the prefab.
Evidence:
- `Assets/Editor/AutoGenJobs/Commands/Builtins/Cmd_CreateOrEditPrefab.cs`: `$prefabRoot` injection and Load/Save/Unload usage.
- `Assets/Editor/AutoGenJobs/Commands/IJobCommand.cs`: shared `CommandContext` var table.
- `Assets/Editor/AutoGenJobs/Commands/Builtins/Cmd_CreateGameObject.cs`: scene-based parenting.

### 5) SetSerializedProperty coverage and correctness
Verdict: PARTIAL PASS
- Uses `SerializedObject.ApplyModifiedProperties()` and `EditorUtility.SetDirty()` (correct).
- ObjectReference supports `ref`, `assetGuid`, `assetPath` (GUID prioritized in `AssetRef.Load`).
- Missing coverage for some common serialized types: `AnimationCurve`, `Gradient`, `RectInt`, `BoundsInt`, `ExposedReference`, `ManagedReference`, etc.
- For ObjectReference, plain string path is not supported; must be `{ assetPath: ... }` or `{ assetGuid: ... }`.
- Missing property path fails unless `ignoreMissing` is true.
Evidence:
- `Assets/Editor/AutoGenJobs/Commands/Builtins/Cmd_SetSerializedProperty.cs`.
- `Assets/Editor/AutoGenJobs/Commands/Builtins/SerializedPropertyHelper.cs`.
- `Assets/Editor/AutoGenJobs/Core/AssetRef.cs`.

### 6) Time slicing and editor responsiveness
Verdict: MOSTLY PASS (budget is coarse)
- Tick has `MaxJobsPerTick` and `MaxMsPerTick` and checks budget before each job.
- Long-running commands are not preempted, so a single job can still exceed the budget and block the editor.
- Asset save/refresh is not forced per command (only via `SaveAssets` command or specific operations like prefab save).
Evidence:
- `Assets/Editor/AutoGenJobs/Core/JobRunner.cs`.
- `Assets/Editor/AutoGenJobs/Commands/Builtins/Cmd_SaveAssets.cs`.

### 7) Idempotence and duplicate delivery
Verdict: PARTIAL PASS
- If a DONE result already exists, the job is skipped and the job file is deleted.
- Jobs with FAILED/WAITING results are re-run; non-idempotent commands (e.g., `CreateGameObject`) can duplicate scene objects if re-executed after a crash/recovery.
- Asset creation commands are safer (`CreateScriptableObject` returns existing asset when `overwrite=false`; `CreateOrEditPrefab` edits existing).
Evidence:
- `Assets/Editor/AutoGenJobs/Core/JobRunner.cs`: `IsJobCompleted()` check.
- `Assets/Editor/AutoGenJobs/Commands/Builtins/Cmd_CreateGameObject.cs`.
- `Assets/Editor/AutoGenJobs/Commands/Builtins/Cmd_CreateScriptableObject.cs`.
- `Assets/Editor/AutoGenJobs/Commands/Builtins/Cmd_CreateOrEditPrefab.cs`.

## Recommendations (priority order)
1) Move job file to `working/` (or lock) before reading to avoid partial reads and race duplication. Consider reading from `working/` only.
2) Add a working-job timeout or state file to reduce re-run risk after crash/reload.
3) Add prefab-context-aware target resolution (default to `$prefabRoot` for nested edits; restrict scene-path lookups during prefab edit).
4) Expand `SerializedPropertyHelper` to cover `AnimationCurve`, `Gradient`, `RectInt/BoundsInt`, `ManagedReference`, etc., and document required formats for ObjectReference values.
5) Optionally emit WAITING results even when tick is skipped due to compilation (if external retry logic relies on the file).

## Tests reviewed
- `Assets/Editor/AutoGenJobs/Tests/EditMode/JobRunnerTests.cs` (basic command coverage; no tests for ingest races, WAITING result behavior during compile, or crash recovery).
