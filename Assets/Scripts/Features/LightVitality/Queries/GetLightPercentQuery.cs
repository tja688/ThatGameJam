using QFramework;
using ThatGameJam.Features.LightVitality.Models;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Queries
{
    public class GetLightPercentQuery : AbstractQuery<float>
    {
        protected override float OnDo()
        {
            var model = this.GetModel<ILightVitalityModel>();
            var max = model.MaxLight.Value;
            if (max <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(model.CurrentLight.Value / max);
        }
    }
}
