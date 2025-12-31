using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Queries
{
    public class ContainsItemQuery : AbstractQuery<bool>
    {
        private readonly string _itemId;

        public ContainsItemQuery(string itemId)
        {
            _itemId = itemId;
        }

        protected override bool OnDo()
        {
            if (string.IsNullOrEmpty(_itemId))
            {
                return false;
            }

            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return false;
            }

            for (var i = 0; i < model.Items.Count; i++)
            {
                var entry = model.Items[i];
                if (entry.Definition != null && entry.Definition.Id == _itemId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
