using QFramework;
using ThatGameJam.Features.DoorGate.Events;
using ThatGameJam.Features.DoorGate.Models;
using UnityEngine;

namespace ThatGameJam.Features.DoorGate.Commands
{
    public class ResetDoorsCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            var doors = model.GetAllDoors();

            for (var i = 0; i < doors.Count; i++)
            {
                var state = doors[i];
                var wasOpen = state.IsOpen;
                var shouldOpen = state.StartOpen || state.RequiredFlowerCount <= 0;

                state.IsOpen = shouldOpen;
                state.ActiveFlowerCount = state.IsOpen
                    ? Mathf.Max(state.ActiveFlowerCount, state.RequiredFlowerCount)
                    : 0;

                model.UpdateDoor(state);

                if (state.IsOpen != wasOpen)
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
