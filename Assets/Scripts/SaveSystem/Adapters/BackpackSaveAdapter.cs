using QFramework;
using ThatGameJam.Features.BackpackFeature.Commands;
using ThatGameJam.Features.BackpackFeature.Controllers;
using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.BackpackFeature.Queries;
using ThatGameJam.Features.KeroseneLamp.Controllers;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    public class BackpackSaveAdapter : SaveParticipant<BackpackSaveState>, IController
    {
        [SerializeField] private string saveKey = "feature.backpack.items";
        [SerializeField] private BackpackItemDatabase itemDatabase;
        [SerializeField] private KeroseneLampManager lampManager;

        private BackpackSaveState _pendingRestore;
        private bool _restoreQueued;

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "feature.backpack.items";
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_restoreQueued && SaveManager.Instance != null)
            {
                SaveManager.Instance.RestoreFinished -= OnRestoreFinished;
            }
            _restoreQueued = false;
            _pendingRestore = null;
        }

        protected override BackpackSaveState Capture()
        {
            var state = new BackpackSaveState
            {
                selectedIndex = this.SendQuery(new GetSelectedIndexQuery()),
                heldIndex = this.SendQuery(new GetHeldIndexQuery())
            };

            var items = this.SendQuery(new GetBackpackItemsQuery());
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var entry = items[i];
                    if (entry.Definition == null)
                    {
                        continue;
                    }

                    state.items.Add(new BackpackSaveEntry
                    {
                        itemId = entry.Definition.Id,
                        quantity = entry.Quantity,
                        instanceId = entry.Instance != null ? entry.Instance.InstanceId : -1
                    });
                }
            }

            return state;
        }

        protected override void Restore(BackpackSaveState data)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsRestoring)
            {
                _pendingRestore = data;
                if (!_restoreQueued)
                {
                    _restoreQueued = true;
                    SaveManager.Instance.RestoreFinished += OnRestoreFinished;
                }
                return;
            }

            RestoreInternal(data);
        }

        private void OnRestoreFinished()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RestoreFinished -= OnRestoreFinished;
            }
            _restoreQueued = false;
            var data = _pendingRestore;
            _pendingRestore = null;
            RestoreInternal(data);
        }

        private void RestoreInternal(BackpackSaveState data)
        {
            ResolveReferences();
            this.SendCommand(new ResetBackpackCommand());

            if (data == null || itemDatabase == null)
            {
                return;
            }

            if (data.items == null)
            {
                data.items = new System.Collections.Generic.List<BackpackSaveEntry>();
            }

            for (var i = 0; i < data.items.Count; i++)
            {
                var entry = data.items[i];
                if (entry == null || string.IsNullOrEmpty(entry.itemId))
                {
                    continue;
                }

                if (!itemDatabase.TryGetDefinition(entry.itemId, out var definition))
                {
                    LogKit.W($"BackpackSaveAdapter missing ItemDefinition for '{entry.itemId}'.");
                    continue;
                }

                IBackpackItemInstance instance = null;
                if (entry.instanceId >= 0 && lampManager != null)
                {
                    if (lampManager.TryGetLampInstance(entry.instanceId, out var lampInstance))
                    {
                        instance = lampInstance;
                    }
                    else
                    {
                        LogKit.W($"BackpackSaveAdapter missing lamp instance for id {entry.instanceId}.");
                    }
                }
                else if (entry.instanceId >= 0 && lampManager == null)
                {
                    LogKit.W($"BackpackSaveAdapter missing KeroseneLampManager for id {entry.instanceId}.");
                }

                this.SendCommand(new AddItemCommand(definition, instance, entry.quantity));
            }

            this.SendCommand(new SetSelectedIndexCommand(data.selectedIndex));
            this.SendCommand(new SetHeldItemCommand(data.heldIndex, null, false, true));
        }

        private void ResolveReferences()
        {
            if (itemDatabase == null)
            {
                itemDatabase = FindObjectOfType<BackpackItemDatabase>();
            }

            if (lampManager == null)
            {
                lampManager = FindObjectOfType<KeroseneLampManager>();
            }
        }
    }
}
