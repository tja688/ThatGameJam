using QFramework;
using ThatGameJam.Features.DoorGate.Events;
using ThatGameJam.Features.DoorGate.Models;
using UnityEngine;

namespace ThatGameJam.Features.DoorGate.Commands
{
    public class SetDoorStateCommand : AbstractCommand
    {
        private readonly string _doorId;
        private readonly bool _isOpen;

        public SetDoorStateCommand(string doorId, bool isOpen)
        {
            _doorId = doorId;
            _isOpen = isOpen;
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(_doorId))
            {
                return;
            }

            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            if (!model.TryGetDoor(_doorId, out var state))
            {
                return;
            }

            var wasOpen = state.IsOpen;
            state.IsOpen = _isOpen;

            if (state.IsOpen)
            {
                state.ActiveFlowerCount = Mathf.Max(state.ActiveFlowerCount, state.RequiredFlowerCount);
            }
            else
            {
                state.ActiveFlowerCount = 0;
            }

            model.UpdateDoor(state);

            if (state.IsOpen != wasOpen)
            {
                this.SendEvent(new DoorStateChangedEvent
                {
                    DoorId = _doorId,
                    IsOpen = state.IsOpen
                });

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
