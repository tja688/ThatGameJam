# Audio System Integration Guide

## One-Click Setup
- Menu: `Tools/Audio/Setup Audio (Audio Manager Pro)`
- Creates or reuses `AudioSystem` root GameObject.
- Ensures components:
  - `AudioService`
  - `AudioManagerProBackend`
  - `SFXManager`
  - `MusicManager`
- Creates assets (if missing):
  - `Assets/Audio/AudioEventDatabase.asset`
  - `Assets/Audio/AudioDebugSettings.asset`

## Audio Binding Panel
- Menu: `Tools/Audio/Audio Binding Panel`
- Use `Sync Docs` to read:
  - `Assets/Audio Doc/SFX_Dotting_Map.md`
  - `Assets/Audio Doc/SFX_Dotting_Index.md`
- Bind clips, play mode, loop, cooldown, spatial, and bus.
- Batch tools:
  - Apply bus/cooldown to category.
  - Export unbound events list.
- Preview clips using the panel Play/Stop buttons.

## Runtime Usage
- Trigger a one-shot:
```csharp
AudioService.Play("SFX-PLR-0001");
```
- Loop with owner-based stop:
```csharp
AudioService.Play("SFX-INT-0003", new AudioContext { Owner = transform });
AudioService.Stop("SFX-INT-0003", new AudioContext { Owner = transform });
```
- Apply volume scaling or UI routing:
```csharp
AudioService.Play("SFX-PLR-0002", new AudioContext { VolumeScale = 0.8f });
AudioService.Play("SFX-UI-0001", new AudioContext { IsUI = true });
```

## Adding a New SFX Event
1. Add the ID to `Assets/Audio Doc/SFX_Dotting_Map.md` (or `SFX_Dotting_Index.md`).
2. Open `Tools/Audio/Audio Binding Panel` and click `Sync Docs`.
3. Bind clips and set parameters in the database.
4. Insert `AudioService.Play/Stop` at the target code location.
5. Validate in a scene with the relevant trigger.

## Debug Logging
- Configure `Assets/Audio/AudioDebugSettings.asset`.
- Enable `LogPlay`, `LogStop`, `LogCooldown` as needed.
