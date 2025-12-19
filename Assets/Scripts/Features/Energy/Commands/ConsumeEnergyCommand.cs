using QFramework;
using UnityEngine;

namespace ThatGameJam
{
    public class ConsumeEnergyCommand : AbstractCommand
    {
        private readonly int _amount;

        public ConsumeEnergyCommand(int amount)
        {
            _amount = amount;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<IEnergyModel>();
            if (model.CurrentEnergy.Value >= _amount)
            {
                model.CurrentEnergy.Value -= _amount;
            }
        }
    }
}
