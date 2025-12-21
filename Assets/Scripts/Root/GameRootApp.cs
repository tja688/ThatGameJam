using QFramework;

public class GameRootApp : Architecture<GameRootApp>
{
    protected override void Init()
    {
        // Register global Systems/Models/Utilities here.
        this.RegisterModel<ThatGameJam.Features.PlayerCharacter2D.Models.IPlayerCharacter2DModel>(new ThatGameJam.Features.PlayerCharacter2D.Models.PlayerCharacter2DModel());
        this.RegisterSystem<ThatGameJam.Features.PlayerCharacter2D.Systems.IPlayerCharacter2DSystem>(new ThatGameJam.Features.PlayerCharacter2D.Systems.PlayerCharacter2DSystem());
        this.RegisterModel<ThatGameJam.Features.LightVitality.Models.ILightVitalityModel>(new ThatGameJam.Features.LightVitality.Models.LightVitalityModel());
        this.RegisterModel<ThatGameJam.Features.Darkness.Models.IDarknessModel>(new ThatGameJam.Features.Darkness.Models.DarknessModel());
        this.RegisterSystem<ThatGameJam.Features.Darkness.Systems.IDarknessSystem>(new ThatGameJam.Features.Darkness.Systems.DarknessSystem());
        this.RegisterModel<ThatGameJam.Features.SafeZone.Models.ISafeZoneModel>(new ThatGameJam.Features.SafeZone.Models.SafeZoneModel());
        this.RegisterSystem<ThatGameJam.Features.SafeZone.Systems.ISafeZoneSystem>(new ThatGameJam.Features.SafeZone.Systems.SafeZoneSystem());
    }
}
