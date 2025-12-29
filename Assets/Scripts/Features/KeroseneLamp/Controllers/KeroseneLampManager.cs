using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.AreaSystem.Events;
using ThatGameJam.Features.AreaSystem.Queries;
using ThatGameJam.Features.KeroseneLamp.Commands;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.KeroseneLamp.Queries;
using ThatGameJam.Features.PlayerCharacter2D.Controllers;
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
        [SerializeField] private Transform playerHoldPoint;
        [SerializeField] private Vector3 heldLampLocalOffset;
        [SerializeField] private Vector3 heldLampLocalEulerAngles;
        [SerializeField] private bool spawnHeldLampOnStart = true;

        private readonly Dictionary<int, KeroseneLampInstance> _lampInstances = new Dictionary<int, KeroseneLampInstance>();
        private readonly HashSet<KeroseneLampPreplaced> _registeredPreplaced = new HashSet<KeroseneLampPreplaced>();
        private int _nextLampId;
        private int _heldLampId = -1;
        private KeroseneLampInstance _heldLampInstance;
        private Transform _resolvedHoldPoint;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<PlayerRespawnedEvent>(OnPlayerRespawned)
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

        private void Start()
        {
            RegisterPreplacedLamps();
            SpawnHeldLampIfNeeded();
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (!DropHeldLamp(e.WorldPos))
            {
                SpawnLamp(e.WorldPos, null, string.Empty);
            }
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            SpawnHeldLampIfNeeded();
        }

        private void OnRunReset(RunResetEvent e)
        {
            ClearHeldLampState();
            ClearLamps();
            _nextLampId = 0;
            _registeredPreplaced.Clear();
            this.SendCommand(new ResetLampsCommand());
            RegisterPreplacedLamps();
            SpawnHeldLampIfNeeded();
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
                maxActivePerArea,
                false,
                true));
        }

        private void ClearLamps(bool includePreplaced = false)
        {
            foreach (var instance in _lampInstances.Values)
            {
                if (instance != null)
                {
                    var isPreplaced = instance.GetComponent<KeroseneLampPreplaced>() != null;
                    if (includePreplaced || !isPreplaced)
                    {
                        Destroy(instance.gameObject);
                    }
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
                if (lamp.LampId == _heldLampId)
                {
                    continue;
                }

                var shouldEnable = string.IsNullOrEmpty(e.CurrentAreaId) || lamp.AreaId == e.CurrentAreaId;
                if (lamp.VisualEnabled == shouldEnable)
                {
                    continue;
                }

                this.SendCommand(new SetLampVisualStateCommand(lamp.LampId, shouldEnable));
            }
        }

        private void SpawnHeldLampIfNeeded()
        {
            if (!spawnHeldLampOnStart || _heldLampInstance != null)
            {
                return;
            }

            var holdPoint = ResolveHoldPoint();
            if (holdPoint == null)
            {
                LogKit.W("KeroseneLampManager missing player hold point. Held lamp will not be spawned.");
                return;
            }

            if (lampPrefab == null)
            {
                LogKit.W("KeroseneLampManager missing lampPrefab. Held lamp will not be instantiated.");
                return;
            }

            var lampId = _nextLampId++;
            var instance = Instantiate(lampPrefab, holdPoint.position, holdPoint.rotation, lampParent);
            if (instance == null)
            {
                return;
            }

            var lampInstance = instance.GetComponent<KeroseneLampInstance>();
            if (lampInstance == null)
            {
                Destroy(instance);
                LogKit.W("KeroseneLampManager spawned lamp missing KeroseneLampInstance.");
                return;
            }

            _heldLampId = lampId;
            _heldLampInstance = lampInstance;
            _lampInstances[lampId] = lampInstance;

            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            var areaId = string.IsNullOrEmpty(currentAreaId) ? fallbackAreaId : currentAreaId;

            lampInstance.SetVisualEnabled(true);
            lampInstance.SetGameplayEnabled(true);
            AttachHeldLamp(lampInstance, holdPoint);

            this.SendCommand(new RecordLampSpawnedCommand(
                lampId,
                lampInstance.transform.position,
                areaId,
                instance,
                true,
                string.Empty,
                maxActivePerArea,
                true,
                false));
        }

        private bool DropHeldLamp(Vector3 worldPos)
        {
            if (_heldLampInstance == null || _heldLampId < 0)
            {
                return false;
            }

            var lampTransform = _heldLampInstance.transform;
            lampTransform.SetParent(lampParent, true);
            lampTransform.position = worldPos;
            _heldLampInstance.SetHeld(false);

            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            var areaId = string.IsNullOrEmpty(currentAreaId) ? fallbackAreaId : currentAreaId;

            this.SendCommand(new ConvertHeldLampToDroppedCommand(
                _heldLampId,
                areaId,
                worldPos,
                maxActivePerArea));

            _heldLampId = -1;
            _heldLampInstance = null;
            return true;
        }

        private void AttachHeldLamp(KeroseneLampInstance lampInstance, Transform holdPoint)
        {
            if (lampInstance == null || holdPoint == null)
            {
                return;
            }

            var lampTransform = lampInstance.transform;
            lampTransform.SetParent(holdPoint, false);
            lampTransform.localPosition = heldLampLocalOffset;
            lampTransform.localEulerAngles = heldLampLocalEulerAngles;
            lampInstance.SetHeld(true);
        }

        private Transform ResolveHoldPoint()
        {
            if (playerHoldPoint != null)
            {
                return playerHoldPoint;
            }

            if (_resolvedHoldPoint != null)
            {
                return _resolvedHoldPoint;
            }

            var controller = FindObjectOfType<PlatformerCharacterController>();
            if (controller != null)
            {
                _resolvedHoldPoint = controller.transform;
            }

            return _resolvedHoldPoint;
        }

        private void ClearHeldLampState()
        {
            _heldLampId = -1;
            _heldLampInstance = null;
        }

        private void RegisterPreplacedLamps()
        {
            var preplaced = Resources.FindObjectsOfTypeAll<KeroseneLampPreplaced>();
            if (preplaced == null || preplaced.Length == 0)
            {
                return;
            }

            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            for (var i = 0; i < preplaced.Length; i++)
            {
                var item = preplaced[i];
                if (item == null || _registeredPreplaced.Contains(item))
                {
                    continue;
                }

                if (!item.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!item.isActiveAndEnabled)
                {
                    continue;
                }

                var instance = item.GetComponent<KeroseneLampInstance>();
                if (instance == null)
                {
                    LogKit.W($"KeroseneLampPreplaced missing KeroseneLampInstance on {item.name}.");
                    continue;
                }

                var lampId = _nextLampId++;
                var areaId = string.IsNullOrEmpty(item.AreaId) ? fallbackAreaId : item.AreaId;
                var visualEnabled = string.IsNullOrEmpty(currentAreaId) || areaId == currentAreaId;

                instance.SetVisualEnabled(visualEnabled);
                instance.SetGameplayEnabled(true);

                _lampInstances[lampId] = instance;
                _registeredPreplaced.Add(item);

                this.SendCommand(new RecordLampSpawnedCommand(
                    lampId,
                    instance.transform.position,
                    areaId,
                    instance.gameObject,
                    visualEnabled,
                    string.Empty,
                    maxActivePerArea,
                    true,
                    false));
            }
        }

        public KeroseneLampSaveState CaptureSaveState()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            var infos = model.GetLampInfos(false);
            var state = new KeroseneLampSaveState
            {
                nextLampId = _nextLampId,
                heldLampId = _heldLampId
            };

            for (var i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                var isHeld = info.LampId == _heldLampId;
                var isPreplaced = false;

                if (model.TryGetLamp(info.LampId, out var record) && record.Instance != null)
                {
                    isPreplaced = record.Instance.GetComponent<KeroseneLampPreplaced>() != null;
                }

                state.lamps.Add(new KeroseneLampSaveEntry
                {
                    lampId = info.LampId,
                    worldPos = info.WorldPos,
                    areaId = info.AreaId ?? string.Empty,
                    spawnOrderInArea = info.SpawnOrderInArea,
                    visualEnabled = info.VisualEnabled,
                    gameplayEnabled = info.GameplayEnabled,
                    ignoreAreaLimit = info.IgnoreAreaLimit,
                    countInLampCount = info.CountInLampCount,
                    presetId = info.PresetId ?? string.Empty,
                    isHeld = isHeld,
                    isPreplaced = isPreplaced
                });
            }

            state.lamps.Sort((a, b) =>
            {
                var areaCompare = string.CompareOrdinal(a.areaId, b.areaId);
                if (areaCompare != 0) return areaCompare;
                var orderCompare = a.spawnOrderInArea.CompareTo(b.spawnOrderInArea);
                if (orderCompare != 0) return orderCompare;
                return a.lampId.CompareTo(b.lampId);
            });

            return state;
        }

        public void RestoreFromSave(KeroseneLampSaveState state)
        {
            if (state == null)
            {
                return;
            }
            if (state.lamps == null)
            {
                state.lamps = new List<KeroseneLampSaveEntry>();
            }

            ClearHeldLampState();
            ClearLamps();
            _registeredPreplaced.Clear();
            this.SendCommand(new ResetLampsCommand());

            var preplaced = Resources.FindObjectsOfTypeAll<KeroseneLampPreplaced>();
            var preplacedInstances = new List<KeroseneLampInstance>();
            if (preplaced != null)
            {
                for (var i = 0; i < preplaced.Length; i++)
                {
                    var item = preplaced[i];
                    if (item == null || !item.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    var instance = item.GetComponent<KeroseneLampInstance>();
                    if (instance != null)
                    {
                        preplacedInstances.Add(instance);
                    }
                }
            }

            var usedPreplaced = new HashSet<KeroseneLampInstance>();
            var holdPoint = ResolveHoldPoint();

            var maxLampId = -1;
            for (var i = 0; i < state.lamps.Count; i++)
            {
                var entry = state.lamps[i];
                if (entry == null)
                {
                    continue;
                }

                maxLampId = Mathf.Max(maxLampId, entry.lampId);

                KeroseneLampInstance instance = null;
                var usedSceneInstance = false;
                if (entry.isPreplaced)
                {
                    instance = FindMatchingPreplaced(entry.worldPos, preplacedInstances, usedPreplaced);
                    if (instance != null)
                    {
                        usedSceneInstance = true;
                        usedPreplaced.Add(instance);
                        instance.gameObject.SetActive(true);
                        var preplacedComponent = instance.GetComponent<KeroseneLampPreplaced>();
                        if (preplacedComponent != null)
                        {
                            _registeredPreplaced.Add(preplacedComponent);
                        }
                    }
                }

                if (instance == null)
                {
                    if (lampPrefab == null)
                    {
                        LogKit.W("KeroseneLampManager missing lampPrefab during restore.");
                        continue;
                    }

                    instance = Instantiate(lampPrefab, entry.worldPos, Quaternion.identity, lampParent)
                        .GetComponent<KeroseneLampInstance>();
                }

                if (instance == null)
                {
                    continue;
                }

                if (!entry.isHeld)
                {
                    var lampTransform = instance.transform;
                    if (lampParent != null && !usedSceneInstance)
                    {
                        lampTransform.SetParent(lampParent, true);
                    }
                    lampTransform.position = entry.worldPos;
                    instance.SetHeld(false);
                }

                instance.SetVisualEnabled(entry.visualEnabled);
                instance.SetGameplayEnabled(entry.gameplayEnabled);

                _lampInstances[entry.lampId] = instance;

                this.SendCommand(new RecordLampSpawnedCommand(
                    entry.lampId,
                    entry.worldPos,
                    entry.areaId ?? string.Empty,
                    instance.gameObject,
                    entry.visualEnabled,
                    entry.presetId ?? string.Empty,
                    maxActivePerArea,
                    entry.ignoreAreaLimit,
                    entry.countInLampCount));

                if (!entry.gameplayEnabled)
                {
                    this.SendCommand(new SetLampGameplayStateCommand(entry.lampId, false));
                }
                if (!entry.visualEnabled)
                {
                    this.SendCommand(new SetLampVisualStateCommand(entry.lampId, false));
                }

                if (entry.isHeld)
                {
                    _heldLampId = entry.lampId;
                    _heldLampInstance = instance;
                    if (holdPoint != null)
                    {
                        AttachHeldLamp(instance, holdPoint);
                    }
                    else
                    {
                        instance.SetHeld(true);
                    }
                }
            }

            if (preplacedInstances.Count > 0)
            {
                for (var i = 0; i < preplacedInstances.Count; i++)
                {
                    var instance = preplacedInstances[i];
                    if (instance != null && !usedPreplaced.Contains(instance))
                    {
                        instance.gameObject.SetActive(false);
                    }
                }
            }

            _nextLampId = Mathf.Max(state.nextLampId, maxLampId + 1);
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            model.UpdateNextLampId(_nextLampId);
        }

        private static KeroseneLampInstance FindMatchingPreplaced(
            Vector3 worldPos,
            List<KeroseneLampInstance> preplacedInstances,
            HashSet<KeroseneLampInstance> used)
        {
            if (preplacedInstances == null || preplacedInstances.Count == 0)
            {
                return null;
            }

            const float maxDistanceSqr = 0.25f * 0.25f;
            KeroseneLampInstance best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < preplacedInstances.Count; i++)
            {
                var candidate = preplacedInstances[i];
                if (candidate == null || used.Contains(candidate))
                {
                    continue;
                }

                var dist = (candidate.transform.position - worldPos).sqrMagnitude;
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = candidate;
                }
            }

            if (best != null && bestDistance <= maxDistanceSqr)
            {
                return best;
            }

            return null;
        }

    }
}
