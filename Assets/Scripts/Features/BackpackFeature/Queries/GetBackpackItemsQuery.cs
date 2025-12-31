using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Queries
{
    public class GetBackpackItemsQuery : AbstractQuery<List<BackpackItemInfo>>
    {
        protected override List<BackpackItemInfo> OnDo()
        {
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            var result = new List<BackpackItemInfo>();

            if (model == null)
            {
                return result;
            }

            for (var i = 0; i < model.Items.Count; i++)
            {
                var entry = model.Items[i];
                result.Add(new BackpackItemInfo
                {
                    Definition = entry.Definition,
                    Quantity = entry.Quantity,
                    Instance = entry.Instance
                });
            }

            return result;
        }
    }
}
