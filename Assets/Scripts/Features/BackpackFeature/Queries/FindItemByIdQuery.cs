using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Queries
{
    public class FindItemByIdQuery : AbstractQuery<BackpackItemInfo>
    {
        private readonly string _itemId;

        public FindItemByIdQuery(string itemId)
        {
            _itemId = itemId;
        }

        protected override BackpackItemInfo OnDo()
        {
            var result = new BackpackItemInfo();
            if (string.IsNullOrEmpty(_itemId))
            {
                return result;
            }

            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return result;
            }

            for (var i = 0; i < model.Items.Count; i++)
            {
                var entry = model.Items[i];
                if (entry.Definition != null && entry.Definition.Id == _itemId)
                {
                    result.Definition = entry.Definition;
                    result.Quantity = entry.Quantity;
                    result.Instance = entry.Instance;
                    return result;
                }
            }

            return result;
        }
    }
}
