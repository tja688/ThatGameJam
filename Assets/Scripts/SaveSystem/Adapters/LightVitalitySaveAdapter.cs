using System;
using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class LightVitalitySaveData
    {
        public float currentLight;
        public float maxLight;
    }

    public class LightVitalitySaveAdapter : SaveParticipant<LightVitalitySaveData>, IController
    {
        [SerializeField] private string saveKey = "player.light";

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "player.light";
        }

        protected override LightVitalitySaveData Capture()
        {
            var model = (LightVitalityModel)this.GetModel<ILightVitalityModel>();
            return new LightVitalitySaveData
            {
                currentLight = model.CurrentValue,
                maxLight = model.MaxValue
            };
        }

        protected override void Restore(LightVitalitySaveData data)
        {
            if (data == null)
            {
                return;
            }

            this.SendCommand(new SetMaxLightCommand(data.maxLight, false));
            this.SendCommand(new SetLightCommand(data.currentLight));
        }
    }
}
