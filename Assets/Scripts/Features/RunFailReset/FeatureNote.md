# Feature: RunFailReset

## 1. Purpose
- Track whether the current run is failed and broadcast fail/reset events.
- Trigger failure when death count reaches a configured threshold.
- Allow resets only from fail flow or test-only scripts.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/RunFailReset/`
- Controllers:
  - `RunFailSettingsController.cs` 〞 sets max deaths per level (optional)
- Systems:
  - `IRunFailResetSystem`, `RunFailResetSystem` 〞 listens for death count changes and emits fail/reset events
- Models:
  - `IRunFailResetModel`, `RunFailResetModel` 〞 `IsFailed` + `MaxDeaths` bindables
- Commands:
  - `MarkRunFailedCommand`
  - `ResetRunCommand`
  - `SetMaxDeathsCommand`
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.RunFailReset.Models.IRunFailResetModel`
  - System: `ThatGameJam.Features.RunFailReset.Systems.IRunFailResetSystem`

### 3.2 Scene setup (Unity)
- Optional MonoBehaviours:
  - `RunFailSettingsController` on a scene object (sets death threshold)
- Inspector fields (if any):
  - `RunFailSettingsController.maxDeathsPerLevel` 〞 failure threshold
  - `RunFailSettingsController.applyOnEnable` 〞 send `SetMaxDeathsCommand` on enable
- Test-only reset input lives in the Testing feature (`Assets/Scripts/Features/Testing/Controllers/RunResetController.cs`).

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct RunFailedEvent`
  - When fired: `MarkRunFailedCommand` executes
  - Payload: none
  - Typical listener: UI/HUD, fail handling
  - Example:
    ```csharp
    this.RegisterEvent<RunFailedEvent>(_ => { /* show failed */ })
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct RunResetEvent`
  - When fired: `IRunFailResetSystem.RequestResetFromFail/Test()` executes
  - Payload: none
  - Typical listener: features that need to reset state

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `MarkRunFailedCommand`
  - What state it mutates: `IRunFailResetModel.IsFailed`
  - Typical sender: `RunFailResetSystem` (death count threshold)
- `ResetRunCommand`
  - What state it mutates: `IRunFailResetModel.IsFailed`
  - Typical sender: `RunFailResetSystem`
- `SetMaxDeathsCommand`
  - What state it mutates: `IRunFailResetModel.MaxDeaths`
  - Typical sender: `RunFailSettingsController` or setup code

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<bool> IsFailed`
  - `IReadonlyBindableProperty<int> MaxDeaths`
  - Usage notes: HUD or fail gating

## 5. Typical Integrations
- Example: Configure death threshold ↙ send `SetMaxDeathsCommand`.
  ```csharp
  this.SendCommand(new SetMaxDeathsCommand(3));
  ```
- Example: Fail handling ↙ call `IRunFailResetSystem.RequestResetFromFail()` after `RunFailedEvent`.

## 6. Verify Checklist
1. Add `RunFailSettingsController` and set `maxDeathsPerLevel` (optional).
2. Trigger deaths until `DeathCountChangedEvent.Count` reaches `MaxDeaths`; expect `RunFailedEvent`.
3. Let `RunFailHandlingController` request reset; expect `RunResetEvent` and `IsFailed` to reset to false.
4. In editor/dev builds, use `Testing/RunResetController` to issue a test reset.

## 7. UNVERIFIED (only if needed)
- None.
