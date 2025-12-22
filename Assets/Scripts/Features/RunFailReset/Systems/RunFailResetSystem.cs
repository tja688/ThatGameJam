using QFramework;
using ThatGameJam.Features.RunFailReset.Commands;
using ThatGameJam.Features.RunFailReset.Models;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.RunFailReset.Systems
{
    public class RunFailResetSystem : AbstractSystem, IRunFailResetSystem
    {
        private IUnRegister _deathCountUnregister;

        protected override void OnInit()
        {
            _deathCountUnregister = this.RegisterEvent<DeathCountChangedEvent>(OnDeathCountChanged);
        }

        protected override void OnDeinit()
        {
            _deathCountUnregister?.UnRegister();
            _deathCountUnregister = null;
        }

        public void RequestResetFromFail()
        {
            var model = this.GetModel<IRunFailResetModel>();
            if (!model.IsFailed.Value)
            {
                return;
            }

            ExecuteReset();
        }

        public void RequestResetFromTest()
        {
            ExecuteReset();
        }

        private void ExecuteReset()
        {
            this.SendCommand(new ResetRunCommand());
            this.SendEvent(new RunResetEvent());
        }

        private void OnDeathCountChanged(DeathCountChangedEvent e)
        {
            var model = this.GetModel<IRunFailResetModel>();
            if (e.Count < model.MaxDeaths.Value)
            {
                return;
            }

            if (model.IsFailed.Value)
            {
                return;
            }

            this.SendCommand(new MarkRunFailedCommand());
        }
    }
}
