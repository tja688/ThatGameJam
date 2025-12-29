using System;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.DoorGate.Events;
using ThatGameJam.Features.DoorGate.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class DoorGateSaveData
    {
        public List<DoorGateEntry> doors = new List<DoorGateEntry>();
    }

    [Serializable]
    public class DoorGateEntry
    {
        public string doorId;
        public int requiredFlowerCount;
        public int activeFlowerCount;
        public bool isOpen;
        public bool allowCloseOnDeactivate;
        public bool startOpen;
    }

    public class DoorGateSaveAdapter : SaveParticipant<DoorGateSaveData>, IController, ICanSendEvent
    {
        [SerializeField] private string saveKey = "door.gates";

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "door.gates";
        }

        protected override DoorGateSaveData Capture()
        {
            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            var list = model.GetAllDoors();
            var data = new DoorGateSaveData();

            for (var i = 0; i < list.Count; i++)
            {
                var state = list[i];
                data.doors.Add(new DoorGateEntry
                {
                    doorId = state.DoorId,
                    requiredFlowerCount = state.RequiredFlowerCount,
                    activeFlowerCount = state.ActiveFlowerCount,
                    isOpen = state.IsOpen,
                    allowCloseOnDeactivate = state.AllowCloseOnDeactivate,
                    startOpen = state.StartOpen
                });
            }

            return data;
        }

        protected override void Restore(DoorGateSaveData data)
        {
            if (data == null || data.doors == null)
            {
                return;
            }

            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            for (var i = 0; i < data.doors.Count; i++)
            {
                var entry = data.doors[i];
                if (entry == null || string.IsNullOrEmpty(entry.doorId))
                {
                    continue;
                }

                var hasExisting = model.TryGetDoor(entry.doorId, out var state);
                var wasOpen = hasExisting && state.IsOpen;

                state.DoorId = entry.doorId;
                state.RequiredFlowerCount = entry.requiredFlowerCount;
                state.ActiveFlowerCount = Mathf.Max(0, entry.activeFlowerCount);
                state.IsOpen = entry.isOpen;
                state.AllowCloseOnDeactivate = entry.allowCloseOnDeactivate;
                state.StartOpen = entry.startOpen;

                model.UpdateDoor(state);

                if (!hasExisting || state.IsOpen != wasOpen)
                {
                    this.SendEvent(new DoorStateChangedEvent
                    {
                        DoorId = state.DoorId,
                        IsOpen = state.IsOpen
                    });

                    if (state.IsOpen)
                    {
                        this.SendEvent(new DoorOpenEvent
                        {
                            DoorId = state.DoorId
                        });
                    }
                }
            }
        }
    }
}
