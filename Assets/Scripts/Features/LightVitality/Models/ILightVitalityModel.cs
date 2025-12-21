using QFramework;

namespace ThatGameJam.Features.LightVitality.Models
{
    public interface ILightVitalityModel : IModel
    {
        IReadonlyBindableProperty<float> CurrentLight { get; }
        IReadonlyBindableProperty<float> MaxLight { get; }
    }
}
