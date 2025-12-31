using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Queries
{
    public class FindItemsByTagQuery : AbstractQuery<List<BackpackItemInfo>>
    {
        private readonly string _tag;

        public FindItemsByTagQuery(string tag)
        {
            _tag = tag;
        }

        protected override List<BackpackItemInfo> OnDo()
        {
            var result = new List<BackpackItemInfo>();
            if (string.IsNullOrEmpty(_tag))
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
                if (entry.Definition == null || entry.Definition.Tags == null)
                {
                    continue;
                }

                var tags = entry.Definition.Tags;
                for (var t = 0; t < tags.Count; t++)
                {
                    if (tags[t] == _tag)
                    {
                        result.Add(new BackpackItemInfo
                        {
                            Definition = entry.Definition,
                            Quantity = entry.Quantity,
                            Instance = entry.Instance
                        });
                        break;
                    }
                }
            }

            return result;
        }
    }
}
