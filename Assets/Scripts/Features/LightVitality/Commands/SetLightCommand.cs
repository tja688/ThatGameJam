using QFramework;
using ThatGameJam.Features.LightVitality.Models;

namespace ThatGameJam.Features.LightVitality.Commands
{
    public class SetLightCommand : AbstractCommand
    {
        private readonly float _value;

        public SetLightCommand(float value)
        {
            _value = value;
        }

        protected override void OnExecute()
        {
            var model = (LightVitalityModel)this.GetModel<ILightVitalityModel>();
            LightVitalityCommandUtils.ApplyCurrentLight(model, _value, this);
        }
    }
}
