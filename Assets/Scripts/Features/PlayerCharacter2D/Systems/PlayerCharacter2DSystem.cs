using QFramework;

namespace ThatGameJam.Features.PlayerCharacter2D.Systems
{
    public interface IPlayerCharacter2DSystem : ISystem
    {
    }

    public class PlayerCharacter2DSystem : AbstractSystem, IPlayerCharacter2DSystem
    {
        protected override void OnInit()
        {
        }
    }
}
