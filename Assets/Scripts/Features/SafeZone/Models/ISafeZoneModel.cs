using QFramework;

namespace ThatGameJam.Features.SafeZone.Models
{
    public interface ISafeZoneModel : IModel
    {
        IReadonlyBindableProperty<int> SafeZoneCount { get; }
        IReadonlyBindableProperty<bool> IsSafe { get; }
    }
}
