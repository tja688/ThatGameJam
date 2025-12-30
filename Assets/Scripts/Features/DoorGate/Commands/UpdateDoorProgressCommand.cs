using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.DoorGate.Events;
using ThatGameJam.Features.DoorGate.Models;
using UnityEngine;

namespace ThatGameJam.Features.DoorGate.Commands
{
    public class UpdateDoorProgressCommand : AbstractCommand
    {
        private readonly string _doorId;
        private readonly int _delta;

        public UpdateDoorProgressCommand(string doorId, int delta)
        {
            _doorId = doorId;
            _delta = delta;
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(_doorId) || _delta == 0)
            {
                return;
            }

            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            if (!model.TryGetDoor(_doorId, out var state))
            {
                return;
            }

            state.ActiveFlowerCount = Mathf.Max(0, state.ActiveFlowerCount + _delta);

            var wasOpen = state.IsOpen;
            if (!state.IsOpen && state.ActiveFlowerCount >= state.RequiredFlowerCount && state.RequiredFlowerCount > 0)
            {
                state.IsOpen = true;
                state.ActiveFlowerCount = Mathf.Max(state.ActiveFlowerCount, state.RequiredFlowerCount);
            }
            else if (state.IsOpen && state.AllowCloseOnDeactivate && state.RequiredFlowerCount > 0
                && state.ActiveFlowerCount < state.RequiredFlowerCount)
            {
                state.IsOpen = false;
            }

            model.UpdateDoor(state);

            if (state.IsOpen != wasOpen)
            {
                this.SendEvent(new DoorStateChangedEvent
                {
                    DoorId = _doorId,
                    IsOpen = state.IsOpen
                });

                AudioService.Play("SFX-INT-0002");

                if (state.IsOpen)
                {
                    this.SendEvent(new DoorOpenEvent
                    {
                        DoorId = _doorId
                    });
                }
            }
        }
    }
}
