using QFramework;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class SetLampInventoryFlagsCommand : AbstractCommand
    {
        private readonly int _lampId;
        private readonly bool _inInventory;

        public SetLampInventoryFlagsCommand(int lampId, bool inInventory)
        {
            _lampId = lampId;
            _inInventory = inInventory;
        }

        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            if (!model.TryGetLamp(_lampId, out var record))
            {
                return;
            }

            var info = record.Info;
            if (_inInventory)
            {
                if (!info.IgnoreAreaLimit && !string.IsNullOrEmpty(info.AreaId))
                {
                    model.DecrementActiveCount(info.AreaId);
                }

                info.IgnoreAreaLimit = true;

                if (info.CountInLampCount)
                {
                    info.CountInLampCount = false;
                    var nextCount = Mathf.Max(0, model.CountValue - 1);
                    if (model.CountValue != nextCount)
                    {
                        model.SetCount(nextCount);
                        this.SendEvent(new LampCountChangedEvent
                        {
                            Count = nextCount
                        });
                    }
                }
            }

            record.Info = info;
            model.RegisterLamp(record);
        }
    }
}
