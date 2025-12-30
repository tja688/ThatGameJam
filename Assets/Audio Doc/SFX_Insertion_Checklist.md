# SFX Insertion Checklist

## Player
- [x] SFX-PLR-0001 `Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs` jump events
- [x] SFX-PLR-0002 `Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs` landing event
- [x] SFX-PLR-0003 `Assets/Scripts/Features/PlayerCharacter2D/Commands/TickFixedStepCommand.cs` climb start/stop
- [x] SFX-PLR-0004 `Assets/Scripts/Features/DeathRespawn/Commands/MarkPlayerDeadCommand.cs` death
- [x] SFX-PLR-0005 `Assets/Scripts/Features/DeathRespawn/Commands/MarkPlayerRespawnedCommand.cs` respawn

## Enemy
- [x] SFX-ENM-0001 `Assets/Scripts/Features/BugAI/Controllers/BugMovementBase.cs` chase start
- [x] SFX-ENM-0002 `Assets/Scripts/Features/BugAI/Controllers/BugMovementBase.cs` return/loiter
- [x] SFX-ENM-0003 `Assets/Scripts/Features/BugAI/Controllers/BugGrabInteraction.cs` grab start/stop
- [x] SFX-ENM-0004 `Assets/Scripts/Features/BugAI/Controllers/BugStompInteraction.cs` stomp
- [x] SFX-ENM-0005 `Assets/Scripts/Features/Mechanisms/Controllers/GhostMechanism2D.cs` ghost loop
- [x] SFX-ENM-0006 `Assets/Scripts/Features/Mechanisms/Controllers/GhostMechanism2D.cs` ghost drain loop

## Interactable
- [x] SFX-INT-0001 `Assets/Scripts/Features/BellFlower/Controllers/BellFlower2D.cs` activate/deactivate
- [x] SFX-INT-0002 `Assets/Scripts/Features/DoorGate/Commands/UpdateDoorProgressCommand.cs` door open/close
- [x] SFX-INT-0003 `Assets/Scripts/Features/Mechanisms/Controllers/DoorMechanism2D.cs` door move loop
- [x] SFX-INT-0004 `Assets/Scripts/Features/Mechanisms/Controllers/VineMechanism2D.cs` vine grow/shrink
- [x] SFX-INT-0005 `Assets/Scripts/Features/IceBlock/Controllers/IceBlock2D.cs` melt loop
- [x] SFX-INT-0006 `Assets/Scripts/Features/IceBlock/Controllers/IceBlock2D.cs` melt complete/refreeze
- [x] SFX-INT-0007 `Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampManager.cs` lamp spawn
- [x] SFX-INT-0008 `Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampManager.cs` lamp pick/drop
- [x] SFX-INT-0009 `Assets/Scripts/Features/KeroseneLamp/Controllers/KeroseneLampInstance.cs` lamp extinguish
- [x] SFX-INT-0010 `Assets/Scripts/Features/KeroseneLamp/Commands/SetLampVisualStateCommand.cs` lamp visual toggle
- [x] SFX-INT-0011 `Assets/Scripts/Features/Checkpoint/Commands/SetCurrentCheckpointCommand.cs` checkpoint trigger
- [x] SFX-INT-0012 `Assets/Scripts/Features/StoryTasks/Controllers/StoryTaskTrigger2D.cs` story gate
- [x] SFX-INT-0013 `Assets/Scripts/Features/StoryTasks/Controllers/StoryTaskTrigger2D.cs` story spawn lamp
- [x] SFX-INT-0014 `Assets/Scripts/Features/FallingRockFromTrashCan/Controllers/FallingRockFromTrashCanController.cs` rockfall loop

## Environment
- [x] SFX-ENV-0001 `Assets/Scripts/Features/Darkness/Commands/SetInDarknessCommand.cs` enter/exit darkness
- [x] SFX-ENV-0002 `Assets/Scripts/Features/Darkness/Systems/DarknessSystem.cs` darkness drain loop
- [x] SFX-ENV-0003 `Assets/Scripts/Features/SafeZone/Commands/SetSafeZoneCountCommand.cs` safe zone enter/exit
- [x] SFX-ENV-0004 `Assets/Scripts/Features/SafeZone/Systems/SafeZoneSystem.cs` safe zone regen loop
- [x] SFX-ENV-0005 `Assets/Scripts/Features/AreaSystem/Commands/SetCurrentAreaCommand.cs` area change
- [x] SFX-ENV-0006 `Assets/Scripts/Features/Hazard/Controllers/HazardVolume2D.cs` instant kill
- [x] SFX-ENV-0007 `Assets/Scripts/Features/Hazard/Controllers/HazardVolume2D.cs` hazard drain loop
- [x] SFX-ENV-0008 `Assets/Scripts/Features/Hazard/Controllers/DamageVolume2D.cs` damage tick
- [x] SFX-ENV-0009 `Assets/Scripts/Features/Mechanisms/Controllers/SpikeHazard2D.cs` spike kill
- [x] SFX-ENV-0010 `Assets/Scripts/Features/FallingRockFromTrashCan/Controllers/FallingRockProjectile.cs` rock hit
- [x] SFX-ENV-0011 `Assets/Scripts/Features/DeathRespawn/Controllers/KillVolume2D.cs` fall death

## UI
- [x] SFX-UI-0001 `Assets/Scripts/SaveSystem/SaveButtonsUI.cs` save click
- [x] SFX-UI-0002 `Assets/Scripts/SaveSystem/SaveButtonsUI.cs` load click
- [x] SFX-UI-0003 `Assets/Scripts/SaveSystem/SaveButtonsUI.cs` delete click

## Meta
- [x] SFX-META-0001 `Assets/Scripts/Features/StoryTasks/Controllers/StoryTaskTrigger2D.cs` dialogue start
- [x] SFX-META-0002 `Assets/Scripts/Features/LightVitality/Commands/LightVitalityCommandUtils.cs` light depleted
