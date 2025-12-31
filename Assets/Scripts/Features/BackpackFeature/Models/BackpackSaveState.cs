using System;
using System.Collections.Generic;

namespace ThatGameJam.Features.BackpackFeature.Models
{
    [Serializable]
    public class BackpackSaveState
    {
        public int selectedIndex = -1;
        public int heldIndex = -1;
        public List<BackpackSaveEntry> items = new List<BackpackSaveEntry>();
    }

    [Serializable]
    public class BackpackSaveEntry
    {
        public string itemId;
        public int quantity;
        public int instanceId = -1;
    }
}
