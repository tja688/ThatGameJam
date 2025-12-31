using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Queries
{
    public class GetHeldItemQuery : AbstractQuery<BackpackItemInfo>
    {
        protected override BackpackItemInfo OnDo()
        {
            var result = new BackpackItemInfo();
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return result;
            }

            var heldIndex = model.HeldIndexValue;
            if (heldIndex < 0 || heldIndex >= model.Items.Count)
            {
                return result;
            }

            var entry = model.Items[heldIndex];
            result.Definition = entry.Definition;
            result.Quantity = entry.Quantity;
            result.Instance = entry.Instance;
            return result;
        }
    }
}
