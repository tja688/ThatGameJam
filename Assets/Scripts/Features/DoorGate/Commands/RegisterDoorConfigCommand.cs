using QFramework;
using ThatGameJam.Features.DoorGate.Events;
using ThatGameJam.Features.DoorGate.Models;
using UnityEngine;

namespace ThatGameJam.Features.DoorGate.Commands
{
    public class RegisterDoorConfigCommand : AbstractCommand
    {
        private readonly string _doorId;
        private readonly int _requiredCount;
        private readonly bool _allowCloseOnDeactivate;
        private readonly bool _startOpen;

        public RegisterDoorConfigCommand(string doorId, int requiredCount, bool allowCloseOnDeactivate, bool startOpen)
        {
            _doorId = doorId;
            _requiredCount = Mathf.Max(0, requiredCount);
            _allowCloseOnDeactivate = allowCloseOnDeactivate;
            _startOpen = startOpen;
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(_doorId))
            {
                return;
            }

            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            var hasDoor = model.TryGetDoor(_doorId, out var state);
            if (!hasDoor)
            {
                state = new DoorGateState
                {
                    DoorId = _doorId,
                    ActiveFlowerCount = 0
                };
            }

            state.RequiredFlowerCount = _requiredCount;
            state.AllowCloseOnDeactivate = _allowCloseOnDeactivate;
            state.StartOpen = _startOpen;

            var shouldOpen = state.StartOpen || state.RequiredFlowerCount <= 0;
            var wasOpen = state.IsOpen;
            state.IsOpen = shouldOpen;

            if (state.IsOpen)
            {
                state.ActiveFlowerCount = Mathf.Max(state.ActiveFlowerCount, state.RequiredFlowerCount);
            }

            model.RegisterDoor(state);

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
