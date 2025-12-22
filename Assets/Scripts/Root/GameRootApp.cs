using QFramework;

public class GameRootApp : Architecture<GameRootApp>
{
    protected override void Init()
    {
        // Register global Systems/Models/Utilities here.
        this.RegisterModel<ThatGameJam.Features.PlayerCharacter2D.Models.IPlayerCharacter2DModel>(new ThatGameJam.Features.PlayerCharacter2D.Models.PlayerCharacter2DModel());
        this.RegisterSystem<ThatGameJam.Features.PlayerCharacter2D.Systems.IPlayerCharacter2DSystem>(new ThatGameJam.Features.PlayerCharacter2D.Systems.PlayerCharacter2DSystem());
        this.RegisterModel<ThatGameJam.Features.LightVitality.Models.ILightVitalityModel>(new ThatGameJam.Features.LightVitality.Models.LightVitalityModel());
        this.RegisterSystem<ThatGameJam.Features.LightVitality.Systems.ILightVitalityResetSystem>(new ThatGameJam.Features.LightVitality.Systems.LightVitalityResetSystem());
        this.RegisterModel<ThatGameJam.Features.Darkness.Models.IDarknessModel>(new ThatGameJam.Features.Darkness.Models.DarknessModel());
        this.RegisterSystem<ThatGameJam.Features.Darkness.Systems.IDarknessSystem>(new ThatGameJam.Features.Darkness.Systems.DarknessSystem());
        this.RegisterModel<ThatGameJam.Features.SafeZone.Models.ISafeZoneModel>(new ThatGameJam.Features.SafeZone.Models.SafeZoneModel());
        this.RegisterSystem<ThatGameJam.Features.SafeZone.Systems.ISafeZoneSystem>(new ThatGameJam.Features.SafeZone.Systems.SafeZoneSystem());
        this.RegisterModel<ThatGameJam.Features.DeathRespawn.Models.IDeathRespawnModel>(new ThatGameJam.Features.DeathRespawn.Models.DeathRespawnModel());
        this.RegisterSystem<ThatGameJam.Features.DeathRespawn.Systems.IDeathRespawnSystem>(new ThatGameJam.Features.DeathRespawn.Systems.DeathRespawnSystem());
        this.RegisterModel<ThatGameJam.Features.KeroseneLamp.Models.IKeroseneLampModel>(new ThatGameJam.Features.KeroseneLamp.Models.KeroseneLampModel());
        this.RegisterModel<ThatGameJam.Features.RunFailReset.Models.IRunFailResetModel>(new ThatGameJam.Features.RunFailReset.Models.RunFailResetModel());
        this.RegisterSystem<ThatGameJam.Features.RunFailReset.Systems.IRunFailResetSystem>(new ThatGameJam.Features.RunFailReset.Systems.RunFailResetSystem());
    }
}
