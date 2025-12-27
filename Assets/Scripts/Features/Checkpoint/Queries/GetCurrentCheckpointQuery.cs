using QFramework;
using ThatGameJam.Features.Checkpoint.Models;

namespace ThatGameJam.Features.Checkpoint.Queries
{
    public class GetCurrentCheckpointQuery : AbstractQuery<CheckpointSnapshot>
    {
        protected override CheckpointSnapshot OnDo()
        {
            var model = (CheckpointModel)this.GetModel<ICheckpointModel>();
            var nodeId = model.CurrentNodeId.Value;
            if (string.IsNullOrEmpty(nodeId))
            {
                return default;
            }

            if (!model.TryGetNode(nodeId, out var info))
            {
                return new CheckpointSnapshot
                {
                    IsValid = false,
                    NodeId = nodeId
                };
            }

            return new CheckpointSnapshot
            {
                IsValid = true,
                NodeId = info.NodeId,
                AreaId = info.AreaId,
                SpawnPoint = info.SpawnPoint
            };
        }
    }
}
