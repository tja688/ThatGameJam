using QFramework;
using ThatGameJam.Features.LightVitality.Models;

namespace ThatGameJam.Features.LightVitality.Queries
{
    public class GetCurrentLightQuery : AbstractQuery<float>
    {
        protected override float OnDo()
        {
            var model = this.GetModel<ILightVitalityModel>();
            return model.CurrentLight.Value;
        }
    }
}
