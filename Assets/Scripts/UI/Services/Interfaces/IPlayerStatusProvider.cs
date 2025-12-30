using System;
using ThatGameJam.UI.Models;

namespace ThatGameJam.UI.Services.Interfaces
{
    public interface IPlayerStatusProvider
    {
        PlayerPanelData GetData();
        event Action<PlayerPanelData> OnChanged;
    }
}
