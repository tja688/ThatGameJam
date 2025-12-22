using QFramework;

namespace ThatGameJam.Features.KeroseneLamp.Models
{
    public interface IKeroseneLampModel : IModel
    {
        IReadonlyBindableProperty<int> LampCount { get; }
    }
}
