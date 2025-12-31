using System;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Queries;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;

namespace ThatGameJam.Features.BackpackFeature.Controllers
{
    public class BackpackUIProvider : MonoBehaviour, IController, IInventoryProvider
    {
        public event Action OnInventoryChanged;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            UIServiceRegistry.SetInventory(this);

            this.RegisterEvent<BackpackChangedEvent>(_ => OnInventoryChanged?.Invoke())
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnDisable()
        {
            if (UIServiceRegistry.Inventory == this)
            {
                UIServiceRegistry.SetInventory(null);
            }
        }

        public IReadOnlyList<ItemStack> GetSlots(int maxSlots = 6)
        {
            var slots = new List<ItemStack>();
            var items = this.SendQuery(new GetBackpackItemsQuery());
            if (items == null || items.Count == 0)
            {
                return slots;
            }

            var count = Mathf.Min(maxSlots, items.Count);
            for (var i = 0; i < count; i++)
            {
                var entry = items[i];
                if (entry.Definition == null)
                {
                    continue;
                }

                slots.Add(new ItemStack
                {
                    ItemId = entry.Definition.Id,
                    DisplayName = entry.Definition.DisplayName,
                    Icon = entry.Definition.Icon,
                    Quantity = entry.Quantity
                });
            }

            return slots;
        }
    }
}
