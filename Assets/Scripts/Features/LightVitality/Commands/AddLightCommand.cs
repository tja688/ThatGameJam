using QFramework;
using ThatGameJam.Features.LightVitality.Models;

namespace ThatGameJam.Features.LightVitality.Commands
{
    public class AddLightCommand : AbstractCommand
    {
        private readonly float _amount;
        private readonly object _requester;

        public AddLightCommand(float amount, object requester)
        {
            _amount = amount;
            _requester = requester;
        }

        protected override void OnExecute()
        {
            var model = (LightVitalityModel)this.GetModel<ILightVitalityModel>();
            var next = model.CurrentValue + _amount;
            LightVitalityCommandUtils.ApplyCurrentLight(model, next, this, _requester);
        }
    }
}
