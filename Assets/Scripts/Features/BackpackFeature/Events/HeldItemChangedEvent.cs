using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Events
{
    public struct HeldItemChangedEvent
    {
        public int HeldIndex;
        public ItemDefinition Definition;
        public int Quantity;
    }
}
