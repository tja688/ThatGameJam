using QFramework;

namespace ThatGameJam.Features.Darkness.Models
{
    public interface IDarknessModel : IModel
    {
        IReadonlyBindableProperty<bool> IsInDarkness { get; }
    }
}
