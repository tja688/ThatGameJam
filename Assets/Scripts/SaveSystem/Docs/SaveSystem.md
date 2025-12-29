# SaveSystem (Easy Save 3)

## Overview
- Single-slot snapshot save/load using Easy Save 3 (ES3).
- Centralized entry point: `SaveManager` (`Save`, `Load`, `Delete`, `HasSave`).
- Participants provide data via `ISaveParticipant` (recommended: `SaveParticipant<T>`).
- Data is stored as a root `SaveSnapshot` with per-feature JSON blocks.

## Files & Structure
- `Assets/Scripts/SaveSystem/SaveManager.cs` 〞 main entry point.
- `Assets/Scripts/SaveSystem/SaveSnapshot.cs` 〞 root data (version, timestamp, blocks).
- `Assets/Scripts/SaveSystem/SaveKeys.cs` 〞 file name, key, version.
- `Assets/Scripts/SaveSystem/SaveParticipant.cs` 〞 base helper for JSON capture/restore.
- `Assets/Scripts/SaveSystem/PersistentId.cs` 〞 stable ID component for scene objects.
- `Assets/Scripts/SaveSystem/SaveButtonsUI.cs` 〞 simple UI hook for Save/Load buttons.
- `Assets/Scripts/SaveSystem/Adapters/*` 〞 feature adapters.

## How to Hook UI
1. Add a `SaveManager` to a scene object (e.g., `SaveSystem`).
2. Add `SaveButtonsUI` to the same object (or your UI root).
3. Wire button `OnClick` events:
   - Save button -> `SaveButtonsUI.Save`
   - Load button -> `SaveButtonsUI.Load`

## Save/Load Flow (Runtime)
### Save
1. `SaveManager.Save()` gathers all `ISaveParticipant`.
2. Each participant returns a JSON block.
3. Snapshot saved via `ES3.Save(SaveKeys.SnapshotKey, snapshot, SaveKeys.Settings)`.

### Load
1. `SaveManager.Load()` checks `HasSave()`.
2. Loads `SaveSnapshot` from ES3.
3. For each participant, if a block exists, restore from JSON.
4. Missing blocks log a warning but do not stop load.

`SaveManager.IsRestoring` is `true` during restore. If a system needs to pause behavior during load, it can poll this flag or subscribe to `RestoreStarted` / `RestoreFinished`.

## Key Conventions
- `player.core`
- `player.light`
- `player.safezone`
- `player.darkness`
- `checkpoint.current`
- `door.gates`
- `lamp.kerosene`
- `story.flags`
- `area.current`

Add new keys with readable, grep-friendly names: `feature.<FeatureName>.<Subsystem>`.

## Integrated Features (Current)
- Player (`player.core`)
  - Position, Rigidbody2D velocity.
- Light Vitality (`player.light`)
  - Current, max.
- Safe Zone (`player.safezone`)
  - Safe zone count.
- Darkness (`player.darkness`)
  - In-darkness state.
- Checkpoint (`checkpoint.current`)
  - Current checkpoint node id.
- Door Gate (`door.gates`)
  - Open/close state, flower counts, config snapshot.
- Kerosene Lamp (`lamp.kerosene`)
  - All lamp entries, states, held lamp, preplaced matching.
- Story Flags (`story.flags`)
  - All flag ids.
- Area System (`area.current`)
  - Current area id.

## Not Yet Saved (Pending)
- Mechanisms: `VineMechanism2D`, `GhostMechanism2D`, `SpikeHazard2D`.
- IceBlock (`IceBlock2D`) melt progress.
- Falling rock (`FallingRockFromTrashCan`).
- Bug AI runtime state.

If these systems affect gameplay persistence, add adapters or explicit save hooks.

## Adding a New Save Participant
Use `SaveParticipant<T>` for most features:

```csharp
using System;
using ThatGameJam.SaveSystem;
using UnityEngine;

[Serializable]
public class ExampleSaveData
{
    public int count;
    public Vector3 pos;
}

public class ExampleSaveAdapter : SaveParticipant<ExampleSaveData>
{
    [SerializeField] private string saveKey = "feature.example";
    public override string SaveKey => saveKey;

    protected override ExampleSaveData Capture()
    {
        return new ExampleSaveData
        {
            count = 1,
            pos = transform.position
        };
    }

    protected override void Restore(ExampleSaveData data)
    {
        transform.position = data.pos;
    }
}
```

## PersistentId Usage
For fixed scene objects that need stable identity:
1. Add `PersistentId` to the object.
2. Use `PersistentId.Id` to build SaveKey or map data to objects.

## Debugging
- Toggle `SaveManager.logEnabled` to see saved blocks and missing blocks.
- If ES3 errors occur, inspect plugin source under `Assets/Plugins/Easy Save 3`.

## Self-Test Checklist
1. Move player -> Save -> move elsewhere -> Load -> player returns.
2. Open a door via BellFlower -> Save -> force close/move -> Load -> door open.
3. Spawn/move lamps -> Save -> change lamp state -> Load -> lamp states restore.
4. Change light vitality -> Save -> drain -> Load -> restored.
5. Trigger multiple changes (player + door + lamp + light + checkpoint) -> Save -> change -> Load -> all restore.

