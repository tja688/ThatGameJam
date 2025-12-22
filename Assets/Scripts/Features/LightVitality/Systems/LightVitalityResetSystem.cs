using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Models;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.LightVitality.Systems
{
    public class LightVitalityResetSystem : AbstractSystem, ILightVitalityResetSystem,ICanSendCommand
    {
        private IUnRegister _runResetUnregister;
        private IUnRegister _respawnUnregister;

        protected override void OnInit()
        {
            _runResetUnregister = this.RegisterEvent<RunResetEvent>(_ => ResetLightToMax());
            _respawnUnregister = this.RegisterEvent<PlayerRespawnedEvent>(_ => ResetLightToMax());
        }

        protected override void OnDeinit()
        {
            _runResetUnregister?.UnRegister();
            _respawnUnregister?.UnRegister();
            _runResetUnregister = null;
            _respawnUnregister = null;
        }

        private void ResetLightToMax()
        {
            var model = this.GetModel<ILightVitalityModel>();
            this.SendCommand(new SetLightCommand(model.MaxLight.Value));
        }
    }
}
