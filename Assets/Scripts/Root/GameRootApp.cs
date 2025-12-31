using QFramework;

public class GameRootApp : Architecture<GameRootApp>
{
    protected override void Init()
    {
        // Register global Systems/Models/Utilities here.
        this.RegisterModel<ThatGameJam.Features.PlayerCharacter2D.Models.IPlayerCharacter2DModel>(new ThatGameJam.Features.PlayerCharacter2D.Models.PlayerCharacter2DModel());
        this.RegisterSystem<ThatGameJam.Features.PlayerCharacter2D.Systems.IPlayerCharacter2DSystem>(new ThatGameJam.Features.PlayerCharacter2D.Systems.PlayerCharacter2DSystem());
        this.RegisterModel<ThatGameJam.Features.Checkpoint.Models.ICheckpointModel>(new ThatGameJam.Features.Checkpoint.Models.CheckpointModel());
        this.RegisterSystem<ThatGameJam.Features.Checkpoint.Systems.ICheckpointSystem>(new ThatGameJam.Features.Checkpoint.Systems.CheckpointSystem());
        this.RegisterModel<ThatGameJam.Features.AreaSystem.Models.IAreaModel>(new ThatGameJam.Features.AreaSystem.Models.AreaModel());
        this.RegisterSystem<ThatGameJam.Features.AreaSystem.Systems.IAreaSystem>(new ThatGameJam.Features.AreaSystem.Systems.AreaSystem());
        this.RegisterModel<ThatGameJam.Features.LightVitality.Models.ILightVitalityModel>(new ThatGameJam.Features.LightVitality.Models.LightVitalityModel());
        this.RegisterSystem<ThatGameJam.Features.LightVitality.Systems.ILightVitalityResetSystem>(new ThatGameJam.Features.LightVitality.Systems.LightVitalityResetSystem());
        this.RegisterModel<ThatGameJam.Features.Darkness.Models.IDarknessModel>(new ThatGameJam.Features.Darkness.Models.DarknessModel());
        this.RegisterSystem<ThatGameJam.Features.Darkness.Systems.IDarknessSystem>(new ThatGameJam.Features.Darkness.Systems.DarknessSystem());
        this.RegisterModel<ThatGameJam.Features.SafeZone.Models.ISafeZoneModel>(new ThatGameJam.Features.SafeZone.Models.SafeZoneModel());
        this.RegisterSystem<ThatGameJam.Features.SafeZone.Systems.ISafeZoneSystem>(new ThatGameJam.Features.SafeZone.Systems.SafeZoneSystem());
        this.RegisterModel<ThatGameJam.Features.DeathRespawn.Models.IDeathRespawnModel>(new ThatGameJam.Features.DeathRespawn.Models.DeathRespawnModel());
        this.RegisterSystem<ThatGameJam.Features.DeathRespawn.Systems.IDeathRespawnSystem>(new ThatGameJam.Features.DeathRespawn.Systems.DeathRespawnSystem());
        this.RegisterModel<ThatGameJam.Features.KeroseneLamp.Models.IKeroseneLampModel>(new ThatGameJam.Features.KeroseneLamp.Models.KeroseneLampModel());
        this.RegisterModel<ThatGameJam.Features.BackpackFeature.Models.IBackpackModel>(new ThatGameJam.Features.BackpackFeature.Models.BackpackModel());
        this.RegisterSystem<ThatGameJam.Features.RunFailReset.Systems.IRunFailResetSystem>(new ThatGameJam.Features.RunFailReset.Systems.RunFailResetSystem());
        this.RegisterModel<ThatGameJam.Features.DoorGate.Models.IDoorGateModel>(new ThatGameJam.Features.DoorGate.Models.DoorGateModel());
        this.RegisterSystem<ThatGameJam.Features.DoorGate.Systems.IDoorGateSystem>(new ThatGameJam.Features.DoorGate.Systems.DoorGateSystem());
        this.RegisterSystem<ThatGameJam.Features.Hazard.Systems.IHazardSystem>(new ThatGameJam.Features.Hazard.Systems.HazardSystem());
        this.RegisterModel<ThatGameJam.Features.StoryTasks.Models.IStoryFlagsModel>(new ThatGameJam.Features.StoryTasks.Models.StoryFlagsModel());
    }
}
