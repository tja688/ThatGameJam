using System;
using QFramework;
using ThatGameJam.Features.Checkpoint.Commands;
using ThatGameJam.Features.Checkpoint.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class CheckpointSaveData
    {
        public string currentNodeId;
    }

    public class CheckpointSaveAdapter : SaveParticipant<CheckpointSaveData>, IController
    {
        [SerializeField] private string saveKey = "checkpoint.current";

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "checkpoint.current";
        }

        protected override CheckpointSaveData Capture()
        {
            var model = (CheckpointModel)this.GetModel<ICheckpointModel>();
            return new CheckpointSaveData
            {
                currentNodeId = model.CurrentNodeId.Value
            };
        }

        protected override void Restore(CheckpointSaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(data.currentNodeId))
            {
                this.SendCommand(new SetCurrentCheckpointCommand(data.currentNodeId));
            }
        }
    }
}
