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

        public RecordLampSpawnedCommand(int lampId, Vector3 worldPos, string areaId, GameObject instance, bool visualEnabled, string presetId, int maxActivePerArea)
        {
            _lampId = lampId;
            _worldPos = worldPos;
            _areaId = areaId ?? string.Empty;
            _instance = instance;
            _visualEnabled = visualEnabled;
            _presetId = presetId ?? string.Empty;
            _maxActivePerArea = Mathf.Max(1, maxActivePerArea);
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
                    PresetId = _presetId
                },
                Instance = _instance
            };

            model.RegisterLamp(record);
            model.AddAreaOrder(_areaId, _lampId);
            model.IncrementActiveCount(_areaId);

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
