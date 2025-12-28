using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Models
{
    public class KeroseneLampModel : AbstractModel, IKeroseneLampModel
    {
        internal struct LampRecord
        {
            public LampInfo Info;
            public GameObject Instance;
        }

        private readonly BindableProperty<int> _lampCount = new BindableProperty<int>(0);
        private readonly Dictionary<int, LampRecord> _lamps = new Dictionary<int, LampRecord>();
        private readonly Dictionary<string, List<int>> _areaOrder = new Dictionary<string, List<int>>();
        private readonly Dictionary<string, int> _areaActiveCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _areaSpawnIndex = new Dictionary<string, int>();

        public IReadonlyBindableProperty<int> LampCount => _lampCount;

        internal int CountValue => _lampCount.Value;
        internal int NextLampId { get; private set; }

        internal void SetCount(int value) => _lampCount.Value = value;
        internal void UpdateNextLampId(int nextId) => NextLampId = nextId > NextLampId ? nextId : NextLampId;
        internal void ResetNextLampId() => NextLampId = 0;

        internal bool TryGetLamp(int lampId, out LampRecord record) => _lamps.TryGetValue(lampId, out record);

        internal void RegisterLamp(LampRecord record)
        {
            _lamps[record.Info.LampId] = record;
        }

        internal void RemoveLamp(int lampId)
        {
            if (!_lamps.TryGetValue(lampId, out var record))
            {
                return;
            }

            _lamps.Remove(lampId);

            if (record.Info.GameplayEnabled && !record.Info.IgnoreAreaLimit && !string.IsNullOrEmpty(record.Info.AreaId))
            {
                var count = GetActiveCountForArea(record.Info.AreaId);
                _areaActiveCounts[record.Info.AreaId] = Mathf.Max(0, count - 1);
            }
        }

        internal void ResetAll()
        {
            _lamps.Clear();
            _areaOrder.Clear();
            _areaActiveCounts.Clear();
            _areaSpawnIndex.Clear();
            _lampCount.Value = 0;
            NextLampId = 0;
        }

        internal List<LampInfo> GetLampInfos(bool gameplayOnly)
        {
            var list = new List<LampInfo>(_lamps.Count);
            foreach (var record in _lamps.Values)
            {
                if (gameplayOnly && !record.Info.GameplayEnabled)
                {
                    continue;
                }

                var info = record.Info;
                if (record.Instance != null)
                {
                    info.WorldPos = record.Instance.transform.position;
                }

                list.Add(info);
            }

            return list;
        }

        internal int GetActiveCountForArea(string areaId)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                areaId = string.Empty;
            }

            return _areaActiveCounts.TryGetValue(areaId, out var count) ? count : 0;
        }

        internal void IncrementActiveCount(string areaId)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                areaId = string.Empty;
            }

            var count = GetActiveCountForArea(areaId);
            _areaActiveCounts[areaId] = count + 1;
        }

        internal void DecrementActiveCount(string areaId)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                areaId = string.Empty;
            }

            var count = GetActiveCountForArea(areaId);
            _areaActiveCounts[areaId] = Mathf.Max(0, count - 1);
        }

        internal int GetNextSpawnOrder(string areaId)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                areaId = string.Empty;
            }

            if (!_areaSpawnIndex.TryGetValue(areaId, out var index))
            {
                index = 0;
            }

            _areaSpawnIndex[areaId] = index + 1;
            return index;
        }

        internal void AddAreaOrder(string areaId, int lampId)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                areaId = string.Empty;
            }

            if (!_areaOrder.TryGetValue(areaId, out var list))
            {
                list = new List<int>();
                _areaOrder[areaId] = list;
            }

            list.Add(lampId);
        }

        internal int FindOldestActiveLampId(string areaId)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                areaId = string.Empty;
            }

            if (!_areaOrder.TryGetValue(areaId, out var list))
            {
                return -1;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var lampId = list[i];
                if (_lamps.TryGetValue(lampId, out var record)
                    && record.Info.GameplayEnabled
                    && !record.Info.IgnoreAreaLimit)
                {
                    return lampId;
                }
            }

            return -1;
        }

        internal void SetLampGameplayEnabled(int lampId, bool enabled)
        {
            if (!_lamps.TryGetValue(lampId, out var record))
            {
                return;
            }

            if (record.Info.GameplayEnabled == enabled)
            {
                return;
            }

            record.Info.GameplayEnabled = enabled;
            _lamps[lampId] = record;

            if (!record.Info.IgnoreAreaLimit && !string.IsNullOrEmpty(record.Info.AreaId))
            {
                var count = GetActiveCountForArea(record.Info.AreaId);
                _areaActiveCounts[record.Info.AreaId] = Mathf.Max(0, count + (enabled ? 1 : -1));
            }
        }

        internal void SetLampVisualEnabled(int lampId, bool enabled)
        {
            if (!_lamps.TryGetValue(lampId, out var record))
            {
                return;
            }

            if (record.Info.VisualEnabled == enabled)
            {
                return;
            }

            record.Info.VisualEnabled = enabled;
            _lamps[lampId] = record;
        }

        protected override void OnInit()
        {
            _lampCount.Value = 0;
            NextLampId = 0;
            _lamps.Clear();
            _areaOrder.Clear();
            _areaActiveCounts.Clear();
            _areaSpawnIndex.Clear();
        }
    }
}
