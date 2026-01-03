# UI Integration Notes

## Required Interfaces
Implement and register these in `UIServiceRegistry` (see `Assets/Scripts/UI/Services/UIServiceRegistry.cs`).

- `IGameFlowService`
  - `StartNewGame()` for main menu "Start New Game".
  - `ReturnToMainMenu()` for settings "Return To Main Menu".
- `ISaveService`
  - `SaveSlot(int slot)` / `LoadSlot(int slot)`.
  - `TryGetSlotInfo(int slot, out SaveSlotInfo info)` for save page details.
- `IAudioSettingsService`
  - `SetBgmVolume`, `SetSfxVolume`, `SetUiVolume`.
  - `GetBgmVolume`, `GetSfxVolume`, `GetUiVolume`.
  - `OnBgmChanged`, `OnSfxChanged`, `OnUiChanged` for UI refresh.
- `IInputRebindService`
  - `GetEntries(DeviceType device)` returns `RebindEntry` list.
  - `StartRebind(actionId, bindingIndex)` / `ResetBinding(actionId, bindingIndex)`.
  - `CancelRebind()` for ESC/cancel behavior.
  - `SaveOverrides()` / `LoadOverrides()` for persistence.
- `IPlayerStatusProvider`
  - `GetData()` + `OnChanged` for player panel left side.
- `IQuestLogProvider`
  - `GetQuests()` + `OnQuestChanged` for quest list + detail.
- `IInventoryProvider`
  - `GetSlots(maxSlots)` + `OnInventoryChanged` for 12-slot inventory.
- `IRebindPersistence` (optional placeholder)
  - `SaveOverrides()` / `LoadOverrides()` if you want a dedicated persistence adapter.

## Panel Subscriptions and Refresh Flow
- `MainMenuPanel`
  - Calls `IGameFlowService.StartNewGame()` and `ISaveService.LoadSlot(0)`.
- `SettingsPanel`
  - Subscribes to `IAudioSettingsService.On*Changed` and pulls current values on open.
  - Uses `ISaveService.SaveSlot/LoadSlot` and `TryGetSlotInfo` to refresh save info.
  - Opens `RebindModalPanel` via `IInputRebindService` (keyboard/gamepad/mouse).
- `RebindModalPanel`
  - Reads `IInputRebindService.GetEntries()` and calls `StartRebind/ResetBinding`.
  - `CancelRebind()` is invoked on ESC while rebinding.
- `PlayerPanel`
  - Subscribes to `IPlayerStatusProvider.OnChanged`.
  - Subscribes to `IQuestLogProvider.OnQuestChanged`.
  - Subscribes to `IInventoryProvider.OnInventoryChanged`.

## Button Wiring Summary
- Main Menu:
  - Start New Game -> `IGameFlowService.StartNewGame()` then closes UI.
  - Continue -> `ISaveService.LoadSlot(0)` then closes UI.
  - Settings -> `UIRouter.OpenSettings(SettingsOpenedFrom.MainMenu)`.
  - Quit -> `Application.Quit()` (Editor stops play mode).
- Settings:
  - Back/Return -> closes settings (returns to menu or game depending on entry).
  - Return To Main Menu -> `IGameFlowService.ReturnToMainMenu()` + main menu.
  - Save / Load -> `ISaveService.SaveSlot(0)` / `LoadSlot(0)`.
  - Bindings buttons -> `UIRouter.OpenRebindModal(DeviceType)`.
  - Return To Game -> closes all UI (resume game).
  - Exit Game -> `Application.Quit()` (Editor stops play mode).
- Player Panel:
  - Close -> closes panel (resume game).

## Rebind Persistence
- Mock implementation stores overrides in `PlayerPrefs` under `UI_MOCK_REBIND_OVERRIDES`.
- Replace with real Input System overrides (e.g., `InputActionAsset.SaveBindingOverridesAsJson()` and load on boot).
- Ensure `SaveOverrides()` is called after rebind completion and `LoadOverrides()` on startup.

## Removing Mock Mode
- Remove or disable `UIMockBootstrap` in the scene.
- Remove `Assets/Scripts/UI/Mock/` or switch to real implementations.
- The DEV button is wired in `Assets/Scripts/UI/Panels/SettingsPanel.cs`:
  - It is guarded by `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
  - Remove the button or update the macro when shipping.
