using QFramework;

namespace ThatGameJam
{
    public interface IEnergySystem : ISystem
    {
        bool IsEnough(int amount);
    }

    public class EnergySystem : AbstractSystem, IEnergySystem
    {
        protected override void OnInit()
        {
        }

        public bool IsEnough(int amount)
        {
            return this.GetModel<IEnergyModel>().CurrentEnergy.Value >= amount;
        }
    }
}
