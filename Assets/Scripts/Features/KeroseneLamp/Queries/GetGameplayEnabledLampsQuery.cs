using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.KeroseneLamp.Models;

namespace ThatGameJam.Features.KeroseneLamp.Queries
{
    public class GetGameplayEnabledLampsQuery : AbstractQuery<List<LampInfo>>
    {
        protected override List<LampInfo> OnDo()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            return model.GetLampInfos(true);
        }
    }
}
