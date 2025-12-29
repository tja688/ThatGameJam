using System;
using QFramework;
using ThatGameJam.Features.SafeZone.Commands;
using ThatGameJam.Features.SafeZone.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class SafeZoneSaveData
    {
        public int safeZoneCount;
    }

    public class SafeZoneSaveAdapter : SaveParticipant<SafeZoneSaveData>, IController
    {
        [SerializeField] private string saveKey = "player.safezone";

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "player.safezone";
        }

        protected override SafeZoneSaveData Capture()
        {
            var model = (SafeZoneModel)this.GetModel<ISafeZoneModel>();
            return new SafeZoneSaveData
            {
                safeZoneCount = model.CountValue
            };
        }

        protected override void Restore(SafeZoneSaveData data)
        {
            if (data == null)
            {
                return;
            }

            this.SendCommand(new SetSafeZoneCountCommand(data.safeZoneCount));
        }
    }
}
