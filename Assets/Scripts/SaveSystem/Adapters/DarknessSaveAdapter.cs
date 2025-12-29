using System;
using QFramework;
using ThatGameJam.Features.Darkness.Commands;
using ThatGameJam.Features.Darkness.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class DarknessSaveData
    {
        public bool isInDarkness;
    }

    public class DarknessSaveAdapter : SaveParticipant<DarknessSaveData>, IController
    {
        [SerializeField] private string saveKey = "player.darkness";

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "player.darkness";
        }

        protected override DarknessSaveData Capture()
        {
            var model = (DarknessModel)this.GetModel<IDarknessModel>();
            return new DarknessSaveData
            {
                isInDarkness = model.CurrentValue
            };
        }

        protected override void Restore(DarknessSaveData data)
        {
            if (data == null)
            {
                return;
            }

            this.SendCommand(new SetInDarknessCommand(data.isInDarkness));
        }
    }
}
