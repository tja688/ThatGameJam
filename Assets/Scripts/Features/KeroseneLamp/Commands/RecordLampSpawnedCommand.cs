using QFramework;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class RecordLampSpawnedCommand : AbstractCommand
    {
        private readonly int _lampId;
        private readonly Vector3 _worldPos;
        private readonly string _areaId;
        private readonly GameObject _instance;
        private readonly bool _visualEnabled;
        private readonly string _presetId;
        private readonly int _maxActivePerArea;
        private readonly bool _ignoreAreaLimit;
        private readonly bool _countInLampCount;

        public RecordLampSpawnedCommand(
            int lampId,
            Vector3 worldPos,
            string areaId,
            GameObject instance,
            bool visualEnabled,
            string presetId,
            int maxActivePerArea,
            bool ignoreAreaLimit,
            bool countInLampCount)
        {
            _lampId = lampId;
            _worldPos = worldPos;
            _areaId = areaId ?? string.Empty;
            _instance = instance;
            _visualEnabled = visualEnabled;
            _presetId = presetId ?? string.Empty;
            _maxActivePerArea = Mathf.Max(1, maxActivePerArea);
            _ignoreAreaLimit = ignoreAreaLimit;
            _countInLampCount = countInLampCount;
        }

        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            var spawnOrder = model.GetNextSpawnOrder(_areaId);

            var record = new KeroseneLampModel.LampRecord
            {
                Info = new LampInfo
                {
                    LampId = _lampId,
                    WorldPos = _worldPos,
                    AreaId = _areaId,
                    SpawnOrderInArea = spawnOrder,
                    VisualEnabled = _visualEnabled,
                    GameplayEnabled = true,
                    IgnoreAreaLimit = _ignoreAreaLimit,
                    CountInLampCount = _countInLampCount,
                    PresetId = _presetId
                },
                Instance = _instance
            };

            model.RegisterLamp(record);
            model.AddAreaOrder(_areaId, _lampId);
            if (!_ignoreAreaLimit)
            {
                model.IncrementActiveCount(_areaId);
            }

            var nextCount = model.CountValue + (_countInLampCount ? 1 : 0);
            if (model.CountValue != nextCount)
            {
                model.SetCount(nextCount);
            }
            model.UpdateNextLampId(_lampId + 1);

            this.SendEvent(new LampSpawnedEvent
            {
                LampId = _lampId,
                WorldPos = _worldPos
            });

            if (_countInLampCount)
            {
                this.SendEvent(new LampCountChangedEvent
                {
                    Count = nextCount
                });
            }

            if (!_ignoreAreaLimit)
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
