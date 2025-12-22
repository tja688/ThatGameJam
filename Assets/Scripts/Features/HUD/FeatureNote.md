# Feature: HUD

## 1. Purpose
- Display gameplay state (light, darkness, safe zone, lamps, fail state) via UI.
- Bind to feature models and update Unity UI components.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/HUD/`
- Controllers:
  - `HUDController.cs` 〞 binds to multiple models and updates UI
- Systems: None
- Models: None (consumes models from other features)
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - None (HUD only reads existing models)

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `HUDController` on a UI GameObject (Canvas child)
- Inspector fields (if any):
  - `lightText`, `lightFill`, `darknessText`, `safeZoneText`, `lampText`, `failText` 〞 UI references

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
- Bindables / readonly properties:
  - `ILightVitalityModel.CurrentLight`, `ILightVitalityModel.MaxLight`
  - `IDarknessModel.IsInDarkness`
  - `ISafeZoneModel.IsSafe`, `ISafeZoneModel.SafeZoneCount`
  - `IKeroseneLampModel.LampCount`
  - `IRunFailResetModel.IsFailed`
  - Usage notes: HUDController registers with `RegisterWithInitValue`

## 5. Typical Integrations
- Example: Custom HUD element ↙ bind to `ILightVitalityModel.CurrentLight` in your own controller.
  ```csharp
  var model = this.GetModel<ILightVitalityModel>();
  model.CurrentLight.Register(v => { /* update UI */ })
      .UnRegisterWhenDisabled(gameObject);
  ```

## 6. Verify Checklist
1. Add `HUDController` to a Canvas and assign all UI fields.
2. Enter a Safe Zone and darkness zone; expect text updates for Safe/Darkness.
3. Drain light to zero; expect fail text to appear (via RunFailReset/DeathRespawn logic).

## 7. UNVERIFIED (only if needed)
- None.
