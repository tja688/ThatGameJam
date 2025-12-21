using QFramework;
using ThatGameJam.Features.LightVitality.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Commands
{
    public class ConsumeLightCommand : AbstractCommand
    {
        private readonly float _amount;
        private readonly ELightConsumeReason _reason;

        public ConsumeLightCommand(float amount, ELightConsumeReason reason)
        {
            _amount = amount;
            _reason = reason;
        }

        protected override void OnExecute()
        {
            var model = (LightVitalityModel)this.GetModel<ILightVitalityModel>();
            var safeAmount = Mathf.Max(0f, _amount);
            var next = model.CurrentValue - safeAmount;


            LightVitalityCommandUtils.ApplyCurrentLight(model, next, this);


            this.SendEvent(new LightConsumedEvent
            {
                Amount = safeAmount,
                Reason = _reason
            });
        }
    }
}
