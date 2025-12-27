using QFramework;
using ThatGameJam.Features.AreaSystem.Models;

namespace ThatGameJam.Features.AreaSystem.Queries
{
    public class GetCurrentAreaIdQuery : AbstractQuery<string>
    {
        protected override string OnDo()
        {
            var model = this.GetModel<IAreaModel>();
            return model.CurrentAreaId.Value;
        }
    }
}
