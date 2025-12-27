using QFramework;
using ThatGameJam.Features.Checkpoint.Models;

namespace ThatGameJam.Features.Checkpoint.Commands
{
    public class UnregisterCheckpointNodeCommand : AbstractCommand
    {
        private readonly string _nodeId;

        public UnregisterCheckpointNodeCommand(string nodeId)
        {
            _nodeId = nodeId;
        }

        protected override void OnExecute()
        {
            var model = (CheckpointModel)this.GetModel<ICheckpointModel>();
            model.UnregisterNode(_nodeId);
        }
    }
}
