using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.AreaSystem.Events;
using ThatGameJam.Features.AreaSystem.Queries;
using ThatGameJam.Features.KeroseneLamp.Commands;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.KeroseneLamp.Queries;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Controllers
{
    public class KeroseneLampManager : MonoBehaviour, IController
    {
        [SerializeField] private GameObject lampPrefab;
        [SerializeField] private Transform lampParent;
        [SerializeField] private int maxActivePerArea = 3;
        [SerializeField] private string fallbackAreaId = "Unknown";

        private readonly Dictionary<int, KeroseneLampInstance> _lampInstances = new Dictionary<int, KeroseneLampInstance>();
        private int _nextLampId;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<RunResetEvent>(OnRunReset)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<RequestSpawnLampEvent>(OnRequestSpawnLamp)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<LampGameplayStateChangedEvent>(OnLampGameplayStateChanged)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<LampVisualStateChangedEvent>(OnLampVisualStateChanged)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<AreaChangedEvent>(OnAreaChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            SpawnLamp(e.WorldPos, null, string.Empty);
        }

        private void OnRunReset(RunResetEvent e)
        {
            ClearLamps();
            _nextLampId = 0;
            this.SendCommand(new ResetLampsCommand());
        }

        private void OnRequestSpawnLamp(RequestSpawnLampEvent e)
        {
            SpawnLamp(e.WorldPos, e.PrefabOverride, e.PresetId);
        }

        private void SpawnLamp(Vector3 worldPos, GameObject prefabOverride, string presetId)
        {
            var lampId = _nextLampId++;

            var prefab = prefabOverride != null ? prefabOverride : lampPrefab;
            GameObject instance = null;
            if (prefab != null)
            {
                instance = Instantiate(prefab, worldPos, Quaternion.identity, lampParent);
            }
            else
            {
                LogKit.W("KeroseneLampManager missing lampPrefab. Lamp will not be instantiated.");
            }

            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            var areaId = string.IsNullOrEmpty(currentAreaId) ? fallbackAreaId : currentAreaId;
            var visualEnabled = true;

            KeroseneLampInstance lampInstance = null;
            if (instance != null)
            {
                lampInstance = instance.GetComponent<KeroseneLampInstance>();
                if (lampInstance != null)
                {
                    lampInstance.SetVisualEnabled(visualEnabled);
                    lampInstance.SetGameplayEnabled(true);
                }
            }

            if (lampInstance != null)
            {
                _lampInstances[lampId] = lampInstance;
            }

            this.SendCommand(new RecordLampSpawnedCommand(
                lampId,
                worldPos,
                areaId,
                instance,
                visualEnabled,
                presetId,
                maxActivePerArea));
        }

        private void ClearLamps()
        {
            foreach (var instance in _lampInstances.Values)
            {
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                }
            }

            _lampInstances.Clear();
        }

        private void OnLampGameplayStateChanged(LampGameplayStateChangedEvent e)
        {
            if (_lampInstances.TryGetValue(e.LampId, out var instance) && instance != null)
            {
                instance.SetGameplayEnabled(e.GameplayEnabled);
            }
        }

        private void OnLampVisualStateChanged(LampVisualStateChangedEvent e)
        {
            if (_lampInstances.TryGetValue(e.LampId, out var instance) && instance != null)
            {
                instance.SetVisualEnabled(e.VisualEnabled);
            }
        }

        private void OnAreaChanged(AreaChangedEvent e)
        {
            var lamps = this.SendQuery(new GetAllLampInfosQuery());
            for (var i = 0; i < lamps.Count; i++)
            {
                var lamp = lamps[i];
                var shouldEnable = string.IsNullOrEmpty(e.CurrentAreaId) || lamp.AreaId == e.CurrentAreaId;
                if (lamp.VisualEnabled == shouldEnable)
                {
                    continue;
                }

                this.SendCommand(new SetLampVisualStateCommand(lamp.LampId, shouldEnable));
            }
        }

    }
}
