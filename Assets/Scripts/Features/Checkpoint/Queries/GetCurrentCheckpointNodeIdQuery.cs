using QFramework;
using ThatGameJam.Features.Checkpoint.Models;

namespace ThatGameJam.Features.Checkpoint.Queries
{
    public class GetCurrentCheckpointNodeIdQuery : AbstractQuery<string>
    {
        protected override string OnDo()
        {
            var model = this.GetModel<ICheckpointModel>();
            return model.CurrentNodeId.Value;
        }
    }
}
