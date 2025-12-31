using QFramework;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class ConvertHeldLampToDroppedCommand : AbstractCommand
    {
        private readonly int _lampId;
        private readonly string _areaId;
        private readonly Vector3 _worldPos;
        private readonly int _maxActivePerArea;

        public ConvertHeldLampToDroppedCommand(int lampId, string areaId, Vector3 worldPos, int maxActivePerArea)
        {
            _lampId = lampId;
            _areaId = areaId ?? string.Empty;
            _worldPos = worldPos;
            _maxActivePerArea = Mathf.Max(1, maxActivePerArea);
        }

        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            if (!model.TryGetLamp(_lampId, out var record))
            {
                return;
            }

            var info = record.Info;
            var wasIgnored = info.IgnoreAreaLimit;
            var wasCounted = info.CountInLampCount;
            var wasGameplayEnabled = info.GameplayEnabled;

            info.WorldPos = _worldPos;
            info.AreaId = _areaId;
            info.IgnoreAreaLimit = false;
            info.CountInLampCount = true;
            info.GameplayEnabled = true;

            if (wasIgnored)
            {
                info.SpawnOrderInArea = model.GetNextSpawnOrder(_areaId);
                model.AddAreaOrder(_areaId, _lampId);
                model.IncrementActiveCount(_areaId);
            }
            else if (!wasGameplayEnabled)
            {
                model.IncrementActiveCount(_areaId);
            }

            record.Info = info;
            model.RegisterLamp(record);

            this.SendEvent(new LampSpawnedEvent
            {
                LampId = _lampId,
                WorldPos = _worldPos
            });

            if (!wasCounted)
            {
                var nextCount = model.CountValue + 1;
                if (model.CountValue != nextCount)
                {
                    model.SetCount(nextCount);
                }

                this.SendEvent(new LampCountChangedEvent
                {
                    Count = nextCount
                });
            }

            if (wasIgnored || !wasGameplayEnabled)
            {
                var activeCount = model.GetActiveCountForArea(_areaId);
                if (activeCount > _maxActivePerArea)
                {
                    var oldestLampId = model.FindOldestActiveLampId(_areaId);
                    if (oldestLampId >= 0 && oldestLampId != _lampId)
                    {
                        model.SetLampGameplayEnabled(oldestLampId, false);
                        this.SendEvent(new LampGameplayStateChangedEvent
                        {
                            LampId = oldestLampId,
                            GameplayEnabled = false
                        });
                    }
                }
            }
        }
    }
}
