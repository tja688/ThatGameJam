using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Queries
{
    public class GetSelectedIndexQuery : AbstractQuery<int>
    {
        protected override int OnDo()
        {
            var model = this.GetModel<IBackpackModel>();
            return model != null ? model.SelectedIndex.Value : -1;
        }
    }
}
