using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Events
{
    public struct BackpackSelectionChangedEvent
    {
        public int SelectedIndex;
        public ItemDefinition Definition;
        public int Quantity;
    }
}
