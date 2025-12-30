using System;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services.Interfaces;

namespace ThatGameJam.UI.Mock
{
    public class MockPlayerStatusProvider : IPlayerStatusProvider
    {
        public event Action<PlayerPanelData> OnChanged;

        private PlayerPanelData _data;

        public PlayerPanelData GetData() => _data;

        public void SetData(PlayerPanelData data)
        {
            _data = data;
            OnChanged?.Invoke(_data);
        }
    }
}
