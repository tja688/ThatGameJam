using System;
using System.Collections.Generic;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services.Interfaces;

namespace ThatGameJam.UI.Mock
{
    public class MockInventoryProvider : IInventoryProvider
    {
        public event Action OnInventoryChanged;

        private readonly List<ItemStack> _slots = new List<ItemStack>();

        public IReadOnlyList<ItemStack> GetSlots(int maxSlots = 12)
        {
            if (_slots.Count > maxSlots)
            {
                return _slots.GetRange(0, maxSlots);
            }

            return _slots;
        }

        public void SetSlots(List<ItemStack> slots)
        {
            _slots.Clear();
            if (slots != null)
            {
                _slots.AddRange(slots);
            }

            OnInventoryChanged?.Invoke();
        }
    }
}
