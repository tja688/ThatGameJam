# Feature: RunFailReset

## 1. Purpose
- Track whether the current run is failed and broadcast fail/reset events.
- Provide a reset trigger (key press or external call).

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/RunFailReset/`
- Controllers:
  - `RunResetController.cs` 〞 listens for reset input and calls system
- Systems:
  - `IRunFailResetSystem`, `RunFailResetSystem` 〞 requests reset and detects failure
- Models:
  - `IRunFailResetModel`, `RunFailResetModel` 〞 `IsFailed` bindable state
- Commands:
  - `MarkRunFailedCommand`
  - `ResetRunCommand`
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.RunFailReset.Models.IRunFailResetModel`
  - System: `ThatGameJam.Features.RunFailReset.Systems.IRunFailResetSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `RunResetController` on a scene object (listens for reset key)
- Inspector fields (if any):
  - `RunResetController.resetKey` 〞 key to request reset
  - `RunResetController.requireFailed` 〞 only allow reset after failure

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct RunFailedEvent`
  - When fired: `MarkRunFailedCommand` executes
  - Payload: none
  - Typical listener: UI/HUD
  - Example:
    ```csharp
    this.RegisterEvent<RunFailedEvent>(_ => { /* show failed */ })
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct RunResetEvent`
  - When fired: `RunFailResetSystem.RequestReset()` executes
  - Payload: none
  - Typical listener: features that need to reset state

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `MarkRunFailedCommand`
  - What state it mutates: `IRunFailResetModel.IsFailed`
  - Typical sender: `RunFailResetSystem` (lamp count overflow)
- `ResetRunCommand`
  - What state it mutates: `IRunFailResetModel.IsFailed`
  - Typical sender: `RunFailResetSystem`

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<bool> IsFailed`
  - Usage notes: HUD or state gating

## 5. Typical Integrations
- Example: External UI button ↙ call `IRunFailResetSystem.RequestReset()`.
  ```csharp
  this.GetSystem<IRunFailResetSystem>().RequestReset();
  ```

## 6. Verify Checklist
1. Add `RunResetController` and press the reset key after failure.
2. Force lamp count over max to trigger `RunFailedEvent`.
3. Press reset; expect `RunResetEvent` and `IsFailed` to reset to false.

## 7. UNVERIFIED (only if needed)
- None.
