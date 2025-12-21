using QFramework;

public class GameRootApp : Architecture<GameRootApp>
{
    protected override void Init()
    {
        // Register global Systems/Models/Utilities here.
        this.RegisterModel<ThatGameJam.Features.PlayerCharacter2D.Models.IPlayerCharacter2DModel>(new ThatGameJam.Features.PlayerCharacter2D.Models.PlayerCharacter2DModel());
        this.RegisterSystem<ThatGameJam.Features.PlayerCharacter2D.Systems.IPlayerCharacter2DSystem>(new ThatGameJam.Features.PlayerCharacter2D.Systems.PlayerCharacter2DSystem());
    }
}

