using System;

namespace ThatGameJam.UI.Models
{
    [Serializable]
    public class RebindEntry
    {
        public string ActionId;
        public string DisplayName;
        public string BindingDisplay;
        public string DefaultBinding;
        public int BindingIndex;
        public bool CanReset = true;
    }
}
