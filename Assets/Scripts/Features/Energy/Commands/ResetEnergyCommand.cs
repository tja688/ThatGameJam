using QFramework;

namespace ThatGameJam
{
    public class ResetEnergyCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = this.GetModel<IEnergyModel>();
            model.CurrentEnergy.Value = model.MaxEnergy;
        }
    }
}
