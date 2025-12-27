using QFramework;

namespace ThatGameJam.Features.AreaSystem.Models
{
    public interface IAreaModel : IModel
    {
        IReadonlyBindableProperty<string> CurrentAreaId { get; }
    }
}
