using QFramework;
using ThatGameJam.Features.LightVitality.Models;

namespace ThatGameJam.Features.LightVitality.Commands
{
    public class AddLightCommand : AbstractCommand
    {
        private readonly float _amount;

        public AddLightCommand(float amount)
        {
            _amount = amount;
        }

        protected override void OnExecute()
        {
            var model = (LightVitalityModel)this.GetModel<ILightVitalityModel>();
            var next = model.CurrentValue + _amount;
            LightVitalityCommandUtils.ApplyCurrentLight(model, next, this);
        }
    }
}
