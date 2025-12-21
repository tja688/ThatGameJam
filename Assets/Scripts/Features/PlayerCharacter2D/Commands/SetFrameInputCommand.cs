using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Models;

namespace ThatGameJam.Features.PlayerCharacter2D.Commands
{
    public class SetFrameInputCommand : AbstractCommand
    {
        private readonly PlatformerFrameInput _input;
        private readonly float _time;

        public SetFrameInputCommand(PlatformerFrameInput input, float time)
        {
            _input = input;
            _time = time;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<IPlayerCharacter2DModel>();
            model.FrameInput = _input;
            model.Time = _time;

            if (_input.JumpDown)
            {
                model.JumpToConsume = true;
                model.TimeJumpWasPressed = _time;
            }
        }
    }
}
