# Planner Check Report

## 1) EndingMenu UI unlock persists

### Findings
- `EndingMenuUnlockState` saves a standalone ES3 key (`feature.endingmenu.unlocked`) into `save.es3` under `PersistentDataPath`.
- `SequencerCommandUnlockEndingMenu` always writes `true` when the sequence runs.
- `SaveManager.Delete()` only deletes the snapshot key and does not clear this UI unlock key, so it survives scene reloads and new builds.

Refs:
- `Assets/Scripts/Independents/EndingMenuUnlockController.cs`
- `Assets/Scripts/Independents/Dialogue Sequence/SequencerCommandUnlockEndingMenu.cs`
- `Assets/Scripts/SaveSystem/SaveKeys.cs`
- `Assets/Scripts/SaveSystem/SaveManager.cs`

### Plan
- Add an explicit clear path for this key (e.g., `EndingMenuUnlockState.Clear()` -> `ES3.DeleteKey(SaveKey, SaveKeys.Settings)`) and call it on “New Game” or before showing main menu in release.
- Optionally add a `SaveManager.DeleteAll()` that deletes the whole `save.es3` file if “no saves kept” is a requirement.
- For clean builds, either version the file name by build version or clear `PersistentDataPath` as part of QA/build verification.

## 2) First run after returning to main menu loses lamp

### Likely causes (need confirm)
- `GameRootApp` models are static; on scene reload the model state (Backpack/KeroseneLamp) persists while scene instances are destroyed.
- `KeroseneLampManager.SpawnHeldLampIfNeeded()` stops early if a lamp item exists in backpack. If the backpack entry survived but its instance was destroyed, it won’t spawn a new lamp.
- `SpawnHeldLampIfNeeded()` only runs on `Start()` and `PlayerRespawnedEvent`. If hold point is not resolved at `Start()` (player inactive or not found), it never retries until death.

Refs:
- `Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampManager.cs`
- `Assets/Scripts/Features/BackpackFeature/Commands/SetHeldItemCommand.cs`
- `Assets/Scripts/Features/BackpackFeature/Commands/ResetBackpackCommand.cs`
- `Assets/Scripts/Root/GameRootApp.cs`

### Plan
- On “ReturnToMainMenu” or “StartNewGame”, explicitly reset runtime models (Backpack/Lamp/Light/Area/etc.) or dispatch `RunResetEvent` so lamps are rebuilt.
- Add a retry path in `KeroseneLampManager` (e.g., coroutine/late check) that spawns held lamp once hold point is available.
- If backpack contains lamp entries with missing instances, clear or rebuild them before `SpawnHeldLampIfNeeded()`.
- Add a debug log for missing hold point and null lamp instances to confirm the path.

## 3) Dialogue System UI remains on main menu

### Findings
- Dialogue System UI can persist across scenes (Dialogue System Controller typically uses persistence), so UI elements can remain when UI Toolkit main menu is on top.
- `UIRouter.MainMenuVisibilityChanged` already provides a hook to clean external UI.

Refs:
- `Assets/Scripts/UI/UIRouter.cs`
- `Assets/Plugins/Pixel Crushers/Dialogue System/Wrappers/Manager/DialogueSystemController.cs`

### Plan
- Add a “MainMenuUIJanitor” listener on `MainMenuVisibilityChanged` to:
  - Stop active conversations (`DialogueManager.StopAllConversations()`),
  - Disable Dialogue UI root canvas / conversation UI GameObjects.
- If Dialogue System Controller is marked persistent, consider disabling its UI when main menu is visible, and re-enable when gameplay starts.

## 4) Ghost SFX after reset + main menu

### Findings
- `GhostMechanism2D.OnEnable()` plays loop `SFX-ENM-0005` and stops it in `OnDisable()`.
- Scene reload or object re-enable during “ReturnToMainMenu” causes `OnEnable()` to fire even when the menu is on top.
- Audio docs confirm SFX-ENM-0005/0006 are bound to `GhostMechanism2D`.

Refs:
- `Assets/Scripts/Features/Mechanisms/Controllers/GhostMechanism2D.cs`
- `Assets/Audio/Audio Doc/SFX_Dotting_Map.md`
- `Assets/Audio/Audio Doc/SFX_Insertion_Checklist.md`

### Plan
- Gate ghost audio by gameplay state: when main menu is on top, stop loop and prevent play; resume only after gameplay starts.
- Alternatively, disable ghost objects or a gameplay root while in menu.
- If needed, add a global “mute gameplay SFX” on main menu (bus level), then restore on game start.

## Verification Checklist
- Clear `save.es3` and confirm ending menu unlock does not persist into a new build.
- Repro “return to main menu -> start new game”: lamp spawns on first life without death.
- Main menu overlay shows no Dialogue System UI.
- No ghost loop audible while main menu is on top.
