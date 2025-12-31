using System.Collections.Generic;
using ThatGameJam.Features.BackpackFeature.Models;
using UnityEngine;

namespace ThatGameJam.Features.BackpackFeature.Controllers
{
    public class BackpackItemDatabase : MonoBehaviour
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> _cache;

        public bool TryGetDefinition(string id, out ItemDefinition definition)
        {
            BuildCacheIfNeeded();
            return _cache.TryGetValue(id, out definition);
        }

        private void BuildCacheIfNeeded()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = new Dictionary<string, ItemDefinition>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                _cache[item.Id] = item;
            }
        }
    }
}
