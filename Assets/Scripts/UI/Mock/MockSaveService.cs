using System;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;

namespace ThatGameJam.UI.Mock
{
    public class MockSaveService : ISaveService
    {
        private SaveSlotInfo? _slotInfo;

        public void SaveSlot(int slot)
        {
            if (slot != 0)
            {
                return;
            }

            _slotInfo = new SaveSlotInfo
            {
                SavedAt = DateTime.Now,
                AreaName = "Mock Area",
                DeathsInArea = UnityEngine.Random.Range(0, 3),
                TotalDeaths = UnityEngine.Random.Range(1, 10),
                LightValue = UnityEngine.Random.Range(20f, 90f),
                Summary = "Saved by mock service"
            };

            Debug.Log("[Mock] SaveSlot(0) called.");
        }

        public void LoadSlot(int slot)
        {
            Debug.Log($"[Mock] LoadSlot({slot}) called.");
        }

        public bool TryGetSlotInfo(int slot, out SaveSlotInfo info)
        {
            if (slot == 0 && _slotInfo.HasValue)
            {
                info = _slotInfo.Value;
                return true;
            }

            info = default;
            return false;
        }

        public void SetSlotInfo(SaveSlotInfo info)
        {
            _slotInfo = info;
        }
    }
}
