# Feature: LightVitality

## 1. Purpose
- Maintain player light resource (current/max) and broadcast changes.
- Provide commands/queries for consuming, adding, and reading light.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/LightVitality/`
- Controllers:
  - `LightVitalityDebugController.cs` 〞 debug keybindings for light changes
  - `LightVitalityResetController.cs` 〞 legacy/optional reset listener for scene-only setups
- Systems:
  - `ILightVitalityResetSystem`, `LightVitalityResetSystem` 〞 listens for run reset/respawn and refills light
- Models:
  - `ILightVitalityModel`, `LightVitalityModel` 〞 current/max light bindables
- Commands:
  - `AddLightCommand`
  - `ConsumeLightCommand`
  - `SetLightCommand`
  - `SetMaxLightCommand`
- Queries:
  - `GetCurrentLightQuery`
  - `GetMaxLightQuery`
  - `GetLightPercentQuery`
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.LightVitality.Models.ILightVitalityModel`
  - System: `ThatGameJam.Features.LightVitality.Systems.ILightVitalityResetSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `LightVitalityDebugController` on a scene object (optional, for debug keys)
- Optional MonoBehaviours:
  - `LightVitalityResetController` (legacy, only if you want scene-only reset listeners)
- Inspector fields (if any):
  - `LightVitalityDebugController.addAmount`, `consumeAmount`, `addKey`, `consumeKey`, `setToMaxKey`

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct LightChangedEvent`
  - When fired: any command updates current/max light
  - Payload: `Current` (float), `Max` (float)
  - Typical listener: HUD
  - Example:
    ```csharp
    this.RegisterEvent<LightChangedEvent>(OnLightChanged)
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct LightConsumedEvent`
  - When fired: `ConsumeLightCommand`
  - Payload: `Amount` (float), `Reason` (ELightConsumeReason)
  - Typical listener: analytics/FX
- `struct LightDepletedEvent`
  - When fired: current light crosses from >0 to <=0
  - Payload: none
  - Typical listener: DeathRespawn (kills on depletion)

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `AddLightCommand`
  - What state it mutates: `ILightVitalityModel.CurrentLight`
  - Typical sender: Safe Zone regen system
- `ConsumeLightCommand`
  - What state it mutates: `ILightVitalityModel.CurrentLight`
  - Typical sender: Darkness drain system
- `SetLightCommand`
  - What state it mutates: `ILightVitalityModel.CurrentLight`
  - Typical sender: reset controller/debug
- `SetMaxLightCommand`
  - What state it mutates: `ILightVitalityModel.MaxLight` (optionally clamps current)
  - Typical sender: setup or progression

### 4.4 Queries (Read Path, optional)
- `GetCurrentLightQuery`
  - What it returns: current light value (float)
  - Typical caller: UI/debug
- `GetMaxLightQuery`
  - What it returns: max light value (float)
  - Typical caller: UI/debug
- `GetLightPercentQuery`
  - What it returns: current/max ratio (0..1)
  - Typical caller: UI fill

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `IReadonlyBindableProperty<float> CurrentLight`
  - `IReadonlyBindableProperty<float> MaxLight`
  - Usage notes: bind in UI for live updates

## 5. Typical Integrations
- Example: Drain light on hit ↙ send `ConsumeLightCommand`.
  ```csharp
  this.SendCommand(new ConsumeLightCommand(10f, ELightConsumeReason.Script));
  ```

## 6. Verify Checklist
1. Add `LightVitalityDebugController` to a scene and press the configured keys.
2. Expect `LightChangedEvent` logs and HUD updates on add/consume.
3. Drain to zero; expect `LightDepletedEvent` to fire.
4. Trigger `RunResetEvent` or `PlayerRespawnedEvent`; expect current light to refill to max.

## 7. UNVERIFIED (only if needed)
- None.
