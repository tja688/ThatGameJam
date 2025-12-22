using QFramework;

namespace ThatGameJam.Features.RunFailReset.Models
{
    public interface IRunFailResetModel : IModel
    {
        IReadonlyBindableProperty<bool> IsFailed { get; }
    }
}
