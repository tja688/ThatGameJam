using System;
using UnityEngine;

namespace ThatGameJam.UI.Models
{
    [Serializable]
    public class ItemStack
    {
        public string ItemId;
        public string DisplayName;
        public Texture2D Icon;
        public int Quantity;
    }
}
