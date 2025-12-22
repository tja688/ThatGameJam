using QFramework;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class RecordLampSpawnedCommand : AbstractCommand
    {
        private readonly int _lampId;
        private readonly Vector3 _worldPos;

        public RecordLampSpawnedCommand(int lampId, Vector3 worldPos)
        {
            _lampId = lampId;
            _worldPos = worldPos;
        }

        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            var nextCount = model.CountValue + 1;
            model.SetCount(nextCount);
            model.UpdateNextLampId(_lampId + 1);

            this.SendEvent(new LampSpawnedEvent
            {
                LampId = _lampId,
                WorldPos = _worldPos
            });

            this.SendEvent(new LampCountChangedEvent
            {
                Count = nextCount
            });
        }
    }
}
