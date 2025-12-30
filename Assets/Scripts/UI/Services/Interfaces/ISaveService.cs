using ThatGameJam.UI.Models;

namespace ThatGameJam.UI.Services.Interfaces
{
    public interface ISaveService
    {
        void SaveSlot(int slot);
        void LoadSlot(int slot);
        bool TryGetSlotInfo(int slot, out SaveSlotInfo info);
    }
}
