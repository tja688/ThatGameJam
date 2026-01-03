using System;
using System.Collections.Generic;
using ThatGameJam.UI.Models;

namespace ThatGameJam.UI.Services.Interfaces
{
    public interface IInventoryProvider
    {
        IReadOnlyList<ItemStack> GetSlots(int maxSlots = 12);
        event Action OnInventoryChanged;
    }
}
