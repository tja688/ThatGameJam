using System.Collections;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.AreaSystem.Queries;
using ThatGameJam.Features.BackpackFeature.Commands;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.KeroseneLamp.Commands;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.KeroseneLamp.Models;
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
        [SerializeField] private float spawnRetryDelay = 0.1f;
        [SerializeField] private int spawnRetryMaxAttempts = 30;

        private readonly Dictionary<int, KeroseneLampInstance> _lampInstances = new Dictionary<int, KeroseneLampInstance>();
        private readonly HashSet<KeroseneLampPreplaced> _registeredPreplaced = new HashSet<KeroseneLampPreplaced>();
        private int _nextLampId;
        private Transform _resolvedHoldPoint;
        private Coroutine _spawnRetryRoutine;
        private bool _loggedMissingHoldPoint;

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
        }

        private void Start()
        {
            RegisterPreplacedLamps();
            SpawnHeldLampIfNeeded();
        }

        public bool TryGetLampInstance(int lampId, out KeroseneLampInstance instance)
        {
            return _lampInstances.TryGetValue(lampId, out instance);
        }

        public void NotifyLampDropped(KeroseneLampInstance instance, Vector3 worldPos)
        {
            if (instance == null || instance.InstanceId < 0)
            {
                return;
            }

            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            var areaId = string.IsNullOrEmpty(currentAreaId) ? fallbackAreaId : currentAreaId;

            this.SendCommand(new ConvertHeldLampToDroppedCommand(
                instance.InstanceId,
                areaId,
                worldPos,
                maxActivePerArea));
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            var heldLamp = FindHeldLampInstance();
            if (heldLamp != null)
            {
                this.SendCommand(new DropHeldItemCommand(e.WorldPos, true));
            }
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            SpawnHeldLampIfNeeded();
        }

        private void OnRunReset(RunResetEvent e)
        {
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
                    lampInstance.SetManager(this);
                    lampInstance.SetLampId(lampId);
                    lampInstance.SetHeldOffsets(heldLampLocalOffset, heldLampLocalEulerAngles);
                    lampInstance.SetState(KeroseneLampState.Dropped, null, worldPos);
                }
            }

            var lightController = lampInstance != null ? lampInstance.GetComponent<LampRegionLightController>() : null;
            if (lightController != null)
            {
                lightController.SetAreaId(areaId);
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
            if (!_lampInstances.TryGetValue(e.LampId, out var instance) || instance == null)
            {
                return;
            }

            instance.SetGameplayEnabled(e.GameplayEnabled);
            if (!e.GameplayEnabled && instance.State == KeroseneLampState.Dropped)
            {
                instance.SetState(KeroseneLampState.Disabled, null, instance.transform.position);
            }
        }

        private void SpawnHeldLampIfNeeded()
        {
            if (!spawnHeldLampOnStart)
            {
                return;
            }

            if (FindHeldLampInstance() != null)
            {
                return;
            }

            PruneInvalidLampBackpackEntries();

            var holdPoint = ResolveHoldPoint();
            if (holdPoint == null)
            {
                if (!_loggedMissingHoldPoint)
                {
                    LogKit.W("KeroseneLampManager missing player hold point. Held lamp will not be spawned yet.");
                    _loggedMissingHoldPoint = true;
                }

                ScheduleSpawnRetry();
                return;
            }

            var lampIndex = FindLampIndexInBackpack();
            if (lampIndex >= 0)
            {
                this.SendCommand(new SetHeldItemCommand(lampIndex, holdPoint));
                EnforceHeldLampAreaLimit();
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

            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            var areaId = string.IsNullOrEmpty(currentAreaId) ? fallbackAreaId : currentAreaId;

            lampInstance.SetManager(this);
            lampInstance.SetLampId(lampId);
            lampInstance.SetHeldOffsets(heldLampLocalOffset, heldLampLocalEulerAngles);

            var lightController = lampInstance.GetComponent<LampRegionLightController>();
            if (lightController != null)
            {
                lightController.SetAreaId(areaId);
            }

            _lampInstances[lampId] = lampInstance;

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

            if (lampInstance.Definition == null)
            {
                LogKit.W("KeroseneLampInstance missing ItemDefinition; cannot add to backpack.");
                return;
            }

            var addedIndex = this.SendCommand(new AddItemCommand(lampInstance.Definition, lampInstance, 1));
            if (addedIndex >= 0)
            {
                this.SendCommand(new SetHeldItemCommand(addedIndex, holdPoint));
                EnforceHeldLampAreaLimit();
            }
        }

        private void ScheduleSpawnRetry()
        {
            if (_spawnRetryRoutine != null || !isActiveAndEnabled)
            {
                return;
            }

            _spawnRetryRoutine = StartCoroutine(SpawnRetryRoutine());
        }

        private IEnumerator SpawnRetryRoutine()
        {
            int attempts = 0;
            while (attempts < spawnRetryMaxAttempts)
            {
                attempts++;
                yield return new WaitForSeconds(spawnRetryDelay);
                if (!isActiveAndEnabled)
                {
                    break;
                }

                var holdPoint = ResolveHoldPoint();
                if (holdPoint != null)
                {
                    _spawnRetryRoutine = null;
                    _loggedMissingHoldPoint = false;
                    SpawnHeldLampIfNeeded();
                    yield break;
                }
            }

            _spawnRetryRoutine = null;
        }

        private bool PruneInvalidLampBackpackEntries()
        {
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null || model.Items.Count == 0)
            {
                return false;
            }

            var previousSelected = model.SelectedIndexValue;
            var previousHeld = model.HeldIndexValue;
            var removedAny = false;

            for (var i = model.Items.Count - 1; i >= 0; i--)
            {
                var entry = model.Items[i];
                if (entry == null || entry.Definition == null)
                {
                    continue;
                }

                if (entry.Instance != null)
                {
                    continue;
                }

                var prefab = entry.Definition.DropPrefab;
                if (prefab == null || prefab.GetComponent<KeroseneLampInstance>() == null)
                {
                    continue;
                }

                model.Items.RemoveAt(i);
                removedAny = true;
            }

            if (!removedAny)
            {
                return false;
            }

            NormalizeBackpackIndices(model);
            this.SendEvent(new BackpackChangedEvent { Count = model.Items.Count });

            if (model.SelectedIndexValue != previousSelected)
            {
                var selectedEntry = model.SelectedIndexValue >= 0 ? model.Items[model.SelectedIndexValue] : null;
                this.SendEvent(new BackpackSelectionChangedEvent
                {
                    SelectedIndex = model.SelectedIndexValue,
                    Definition = selectedEntry != null ? selectedEntry.Definition : null,
                    Quantity = selectedEntry != null ? selectedEntry.Quantity : 0
                });
            }

            if (model.HeldIndexValue != previousHeld)
            {
                var heldEntry = model.HeldIndexValue >= 0 ? model.Items[model.HeldIndexValue] : null;
                this.SendEvent(new HeldItemChangedEvent
                {
                    HeldIndex = model.HeldIndexValue,
                    Definition = heldEntry != null ? heldEntry.Definition : null,
                    Quantity = heldEntry != null ? heldEntry.Quantity : 0
                });
            }

            return true;
        }

        private static void NormalizeBackpackIndices(BackpackModel model)
        {
            if (model.SelectedIndexValue >= model.Items.Count)
            {
                model.SelectedIndexValue = model.Items.Count - 1;
            }

            if (model.HeldIndexValue >= model.Items.Count)
            {
                model.HeldIndexValue = -1;
            }
        }

        private void EnforceHeldLampAreaLimit()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            if (model == null)
            {
                return;
            }

            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            var areaId = string.IsNullOrEmpty(currentAreaId) ? fallbackAreaId : currentAreaId;
            var limit = Mathf.Max(1, maxActivePerArea);
            if (model.GetActiveCountForArea(areaId) < limit)
            {
                return;
            }

            var oldestLampId = model.FindOldestActiveLampId(areaId);
            if (oldestLampId >= 0)
            {
                this.SendCommand(new SetLampGameplayStateCommand(oldestLampId, false));
            }
        }

        private int FindLampIndexInBackpack()
        {
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return -1;
            }

            for (var i = 0; i < model.Items.Count; i++)
            {
                var entry = model.Items[i];
                if (entry.Instance is KeroseneLampInstance)
                {
                    return i;
                }

                var definition = entry.Definition;
                if (definition != null && definition.DropPrefab != null
                    && definition.DropPrefab.GetComponent<KeroseneLampInstance>() != null)
                {
                    return i;
                }
            }

            return -1;
        }

        private KeroseneLampInstance FindHeldLampInstance()
        {
            foreach (var entry in _lampInstances.Values)
            {
                if (entry != null && entry.State == KeroseneLampState.Held)
                {
                    return entry;
                }
            }

            return null;
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

            var controller = FindObjectOfType<ThatGameJam.Features.PlayerCharacter2D.Controllers.PlatformerCharacterController>();
            if (controller == null)
            {
                var candidates = Resources.FindObjectsOfTypeAll<ThatGameJam.Features.PlayerCharacter2D.Controllers.PlatformerCharacterController>();
                for (var i = 0; i < candidates.Length; i++)
                {
                    var candidate = candidates[i];
                    if (candidate != null && candidate.gameObject.scene.IsValid())
                    {
                        controller = candidate;
                        break;
                    }
                }
            }

            if (controller != null)
            {
                _resolvedHoldPoint = controller.transform;
            }

            return _resolvedHoldPoint;
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

                instance.SetManager(this);
                instance.SetLampId(lampId);
                instance.SetHeldOffsets(heldLampLocalOffset, heldLampLocalEulerAngles);
                instance.SetState(KeroseneLampState.Dropped, null, instance.transform.position);

                var lightController = instance.GetComponent<LampRegionLightController>();
                if (lightController != null)
                {
                    lightController.SetAreaId(areaId);
                }

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
                heldLampId = -1
            };

            for (var i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                var isPreplaced = false;
                var lampState = KeroseneLampState.Dropped;
                var visualEnabled = info.VisualEnabled;

                if (model.TryGetLamp(info.LampId, out var record) && record.Instance != null)
                {
                    isPreplaced = record.Instance.GetComponent<KeroseneLampPreplaced>() != null;
                }

                if (_lampInstances.TryGetValue(info.LampId, out var instance) && instance != null)
                {
                    lampState = instance.State;
                    var lightController = instance.GetComponent<LampRegionLightController>();
                    if (lightController != null)
                    {
                        visualEnabled = lightController.IsVisualEnabled;
                    }
                }

                if (lampState == KeroseneLampState.Held)
                {
                    state.heldLampId = info.LampId;
                }

                state.lamps.Add(new KeroseneLampSaveEntry
                {
                    lampId = info.LampId,
                    worldPos = info.WorldPos,
                    areaId = info.AreaId ?? string.Empty,
                    spawnOrderInArea = info.SpawnOrderInArea,
                    visualEnabled = visualEnabled,
                    gameplayEnabled = info.GameplayEnabled,
                    ignoreAreaLimit = info.IgnoreAreaLimit,
                    countInLampCount = info.CountInLampCount,
                    presetId = info.PresetId ?? string.Empty,
                    isHeld = lampState == KeroseneLampState.Held,
                    isPreplaced = isPreplaced,
                    state = lampState,
                    hasState = true
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

                instance.SetManager(this);
                instance.SetLampId(entry.lampId);
                instance.SetHeldOffsets(heldLampLocalOffset, heldLampLocalEulerAngles);

                var lightController = instance.GetComponent<LampRegionLightController>();
                if (lightController != null)
                {
                    lightController.SetAreaId(entry.areaId ?? string.Empty);
                }

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

                var restoreState = entry.hasState ? entry.state : KeroseneLampState.Dropped;
                if (!entry.hasState)
                {
                    if (entry.isHeld)
                    {
                        restoreState = KeroseneLampState.Held;
                    }
                    else if (!entry.gameplayEnabled)
                    {
                        restoreState = KeroseneLampState.Disabled;
                    }
                }

                switch (restoreState)
                {
                    case KeroseneLampState.Held:
                        instance.SetState(KeroseneLampState.Held, holdPoint, entry.worldPos);
                        break;
                    case KeroseneLampState.InBackpack:
                        instance.SetState(KeroseneLampState.InBackpack, null, entry.worldPos);
                        break;
                    case KeroseneLampState.Disabled:
                        instance.SetState(KeroseneLampState.Disabled, null, entry.worldPos);
                        break;
                    default:
                        instance.SetState(KeroseneLampState.Dropped, null, entry.worldPos);
                        break;
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
