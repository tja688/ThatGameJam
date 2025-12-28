using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Events;
using ThatGameJam.Features.PlayerCharacter2D.Models;

namespace ThatGameJam.Features.PlayerCharacter2D.Commands
{
    public class ResetClimbStateCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = this.GetModel<IPlayerCharacter2DModel>();

            if (model.IsClimbing.Value)
            {
                model.IsClimbing.Value = false;
                this.SendEvent(new PlayerClimbStateChangedEvent { IsClimbing = false });
            }

            model.WallContactTimer = 0f;
            model.RegrabLockoutTimer = 0f;
            model.ClimbWallSide = 0f;
            model.ClimbIsHorizontal = false;
            model.ClimbJumpProtected = false;
        }
    }
}
