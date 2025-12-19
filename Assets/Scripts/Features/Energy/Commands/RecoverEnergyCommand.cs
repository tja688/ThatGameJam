using QFramework;
using UnityEngine;

namespace ThatGameJam
{
    public class RecoverEnergyCommand : AbstractCommand
    {
        private readonly int _amount;

        public RecoverEnergyCommand(int amount)
        {
            _amount = amount;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<IEnergyModel>();
            if (model.CurrentEnergy.Value < model.MaxEnergy)
            {
                model.CurrentEnergy.Value = Mathf.Min(model.CurrentEnergy.Value + _amount, model.MaxEnergy);
            }
        }
    }
}
