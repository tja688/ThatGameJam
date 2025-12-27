using QFramework;
using ThatGameJam.Features.Checkpoint.Models;

namespace ThatGameJam.Features.Checkpoint.Commands
{
    public class RegisterCheckpointNodeCommand : AbstractCommand
    {
        private readonly CheckpointNodeInfo _info;

        public RegisterCheckpointNodeCommand(CheckpointNodeInfo info)
        {
            _info = info;
        }

        protected override void OnExecute()
        {
            var model = (CheckpointModel)this.GetModel<ICheckpointModel>();
            model.RegisterNode(_info);
        }
    }
}
