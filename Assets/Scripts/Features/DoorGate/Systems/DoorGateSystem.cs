using QFramework;
using ThatGameJam.Features.BellFlower.Events;
using ThatGameJam.Features.DoorGate.Commands;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.DoorGate.Systems
{
    public class DoorGateSystem : AbstractSystem, IDoorGateSystem
    {
        private IUnRegister _flowerUnregister;
        private IUnRegister _resetUnregister;

        protected override void OnInit()
        {
            _flowerUnregister = this.RegisterEvent<FlowerActivatedEvent>(OnFlowerActivated);
            _resetUnregister = this.RegisterEvent<RunResetEvent>(_ => this.SendCommand(new ResetDoorsCommand()));
        }

        protected override void OnDeinit()
        {
            _flowerUnregister?.UnRegister();
            _resetUnregister?.UnRegister();
            _flowerUnregister = null;
            _resetUnregister = null;
        }

        private void OnFlowerActivated(FlowerActivatedEvent e)
        {
            if (string.IsNullOrEmpty(e.DoorId))
            {
                return;
            }

            var delta = e.IsActive ? 1 : -1;
            if (delta == 0)
            {
                return;
            }

            this.SendCommand(new UpdateDoorProgressCommand(e.DoorId, delta));
        }
    }
}
