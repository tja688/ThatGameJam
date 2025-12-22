using QFramework;

namespace ThatGameJam.Features.DeathRespawn.Models
{
    public interface IDeathRespawnModel : IModel
    {
        IReadonlyBindableProperty<bool> IsAlive { get; }
        IReadonlyBindableProperty<int> DeathCount { get; }
    }
}
