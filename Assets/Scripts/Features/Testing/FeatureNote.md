# Feature: Testing

## 1. Purpose
- Provide editor/dev-only test helpers that should not be used by gameplay.
- Trigger run reset through the test-only reset path.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Testing/`
- Controllers:
  - `RunResetController.cs` 〞 test-only reset input (calls `RequestResetFromTest`)
- Systems: None
- Models: None
- Commands: None
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules: None.

### 3.2 Scene setup (Unity)
- Optional MonoBehaviours:
  - `RunResetController` on a scene object (editor/dev builds only)
- Inspector fields (if any):
  - `RunResetController.resetKey` 〞 key to request test reset
  - `RunResetController.requireFailed` 〞 only allow reset after failure

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- None.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- Example: Use `RunResetController` in dev scenes to trigger `RunResetEvent` via the test path.

## 6. Verify Checklist
1. Add `RunResetController` in a dev build or the editor.
2. Press `resetKey`; expect a `RunResetEvent` (if `requireFailed` is false).

## 7. UNVERIFIED (only if needed)
- None.
