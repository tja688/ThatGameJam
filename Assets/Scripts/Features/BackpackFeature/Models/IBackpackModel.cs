using QFramework;

namespace ThatGameJam.Features.BackpackFeature.Models
{
    public interface IBackpackModel : IModel
    {
        IReadonlyBindableProperty<int> SelectedIndex { get; }
        IReadonlyBindableProperty<int> HeldIndex { get; }
    }
}
