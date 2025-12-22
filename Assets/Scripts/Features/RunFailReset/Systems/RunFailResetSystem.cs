using QFramework;
using ThatGameJam.Features.RunFailReset.Commands;
using ThatGameJam.Features.RunFailReset.Models;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.RunFailReset.Systems
{
    public class RunFailResetSystem : AbstractSystem, IRunFailResetSystem
    {
        private IUnRegister _lampCountUnregister;

        protected override void OnInit()
        {
            _lampCountUnregister = this.RegisterEvent<LampCountChangedEvent>(OnLampCountChanged);
        }

        protected override void OnDeinit()
        {
            _lampCountUnregister?.UnRegister();
            _lampCountUnregister = null;
        }

        public void RequestReset()
        {
            this.SendCommand(new ResetRunCommand());
            this.SendEvent(new RunResetEvent());
        }

        private void OnLampCountChanged(LampCountChangedEvent e)
        {
            if (e.Count <= e.Max)
            {
                return;
            }

            var model = this.GetModel<IRunFailResetModel>();
            if (model.IsFailed.Value)
            {
                return;
            }

            this.SendCommand(new MarkRunFailedCommand());
        }
    }
}
