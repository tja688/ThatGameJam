using QFramework;
using ThatGameJam.Features.DoorGate.Models;

namespace ThatGameJam.Features.DoorGate.Commands
{
    public class UnregisterDoorCommand : AbstractCommand
    {
        private readonly string _doorId;

        public UnregisterDoorCommand(string doorId)
        {
            _doorId = doorId;
        }

        protected override void OnExecute()
        {
            var model = (DoorGateModel)this.GetModel<IDoorGateModel>();
            model.UnregisterDoor(_doorId);
        }
    }
}
