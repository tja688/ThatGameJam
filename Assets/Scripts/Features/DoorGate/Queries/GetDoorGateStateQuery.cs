using QFramework;
using ThatGameJam.Features.DoorGate.Models;

namespace ThatGameJam.Features.DoorGate.Queries
{
    public class GetDoorGateStateQuery : AbstractQuery<DoorGateSnapshot>
    {
        private readonly string _doorId;

        public GetDoorGateStateQuery(string doorId)
        {
            _doorId = doorId;
        }

        protected override DoorGateSnapshot OnDo()
        {
            if (string.IsNullOrEmpty(_doorId))
            {
                return new DoorGateSnapshot { HasDoor = false };
            }

            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            if (!model.TryGetDoor(_doorId, out var state))
            {
                return new DoorGateSnapshot { HasDoor = false };
            }

            return new DoorGateSnapshot
            {
                HasDoor = true,
                State = state
            };
        }
    }
}
