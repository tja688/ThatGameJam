using System.Collections.Generic;
using System.Threading.Tasks;
using ThatGameJam.UI.Models;

namespace ThatGameJam.UI.Services.Interfaces
{
    public interface IInputRebindService
    {
        IEnumerable<RebindEntry> GetEntries(DeviceType device);
        Task StartRebind(string actionId, int bindingIndex);
        void ResetBinding(string actionId, int bindingIndex);
        void CancelRebind();
        void SaveOverrides();
        void LoadOverrides();
    }
}
