# Feature: RunFailHandling

## 1. Purpose
- Handle run failure events with a placeholder flow (log + delay + reset) until real failure UX is implemented.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/RunFailHandling/`
- Controllers:
  - `RunFailHandlingController.cs` 〞 listens for `RunFailedEvent` and performs placeholder handling
- Systems: None
- Models: None
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules: None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `RunFailHandlingController` on a scene manager object (handles run failure flow)
- Inspector fields (if any):
  - `RunFailHandlingController.resetDelaySeconds` 〞 seconds to wait before issuing reset (default 3)

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
- Example: Trigger a failure ↙ send `MarkRunFailedCommand`, then `RunFailHandlingController` will log and reset after delay.
  ```csharp
  this.SendCommand(new MarkRunFailedCommand());
  ```

## 6. Verify Checklist
1. Add `RunFailHandlingController` to a scene.
2. Trigger `RunFailedEvent` (e.g., via lamp overflow).
3. Observe a `LogKit` info message and a reset after `resetDelaySeconds`.

## 7. UNVERIFIED (only if needed)
- None.
