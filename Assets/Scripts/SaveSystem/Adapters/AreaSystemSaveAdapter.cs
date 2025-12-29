using System;
using QFramework;
using ThatGameJam.Features.AreaSystem.Commands;
using ThatGameJam.Features.AreaSystem.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class AreaSystemSaveData
    {
        public string currentAreaId;
    }

    public class AreaSystemSaveAdapter : SaveParticipant<AreaSystemSaveData>, IController
    {
        [SerializeField] private string saveKey = "area.current";

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "area.current";
        }

        protected override AreaSystemSaveData Capture()
        {
            var model = (AreaModel)this.GetModel<IAreaModel>();
            return new AreaSystemSaveData
            {
                currentAreaId = model.CurrentAreaValue
            };
        }

        protected override void Restore(AreaSystemSaveData data)
        {
            if (data == null)
            {
                return;
            }

            this.SendCommand(new SetCurrentAreaCommand(data.currentAreaId));
        }
    }
}
