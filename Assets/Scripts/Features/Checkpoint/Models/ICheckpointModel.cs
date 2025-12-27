using QFramework;

namespace ThatGameJam.Features.Checkpoint.Models
{
    public interface ICheckpointModel : IModel
    {
        IReadonlyBindableProperty<string> CurrentNodeId { get; }
    }
}
