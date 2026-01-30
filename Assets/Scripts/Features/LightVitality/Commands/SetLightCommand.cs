using QFramework;
using ThatGameJam.Features.LightVitality.Models;

namespace ThatGameJam.Features.LightVitality.Commands
{
    public class SetLightCommand : AbstractCommand
    {
        private readonly float _value;
        private readonly object _requester;

        public SetLightCommand(float value, object requester)
        {
            _value = value;
            _requester = requester;
        }

        protected override void OnExecute()
        {
            var model = (LightVitalityModel)this.GetModel<ILightVitalityModel>();
            LightVitalityCommandUtils.ApplyCurrentLight(model, _value, this, _requester);
        }
    }
}
