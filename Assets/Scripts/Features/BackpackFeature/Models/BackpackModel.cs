using System.Collections.Generic;
using QFramework;

namespace ThatGameJam.Features.BackpackFeature.Models
{
    public class BackpackModel : AbstractModel, IBackpackModel
    {
        internal class BackpackItemEntry
        {
            public ItemDefinition Definition;
            public int Quantity;
            public IBackpackItemInstance Instance;
        }

        private readonly BindableProperty<int> _selectedIndex = new BindableProperty<int>(-1);
        private readonly BindableProperty<int> _heldIndex = new BindableProperty<int>(-1);

        internal readonly List<BackpackItemEntry> Items = new List<BackpackItemEntry>();

        public IReadonlyBindableProperty<int> SelectedIndex => _selectedIndex;
        public IReadonlyBindableProperty<int> HeldIndex => _heldIndex;

        internal int SelectedIndexValue
        {
            get => _selectedIndex.Value;
            set => _selectedIndex.Value = value;
        }

        internal int HeldIndexValue
        {
            get => _heldIndex.Value;
            set => _heldIndex.Value = value;
        }

        internal void Clear()
        {
            Items.Clear();
            _selectedIndex.Value = -1;
            _heldIndex.Value = -1;
        }

        protected override void OnInit()
        {
            Items.Clear();
            _selectedIndex.Value = -1;
            _heldIndex.Value = -1;
        }
    }
}
