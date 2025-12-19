using QFramework;

namespace ThatGameJam
{
    public interface IEnergyModel : IModel
    {
        BindableProperty<int> CurrentEnergy { get; }
        int MaxEnergy { get; }
    }

    public class EnergyModel : AbstractModel, IEnergyModel
    {
        public BindableProperty<int> CurrentEnergy { get; } = new BindableProperty<int>(0);
        public int MaxEnergy { get; } = 100;

        protected override void OnInit()
        {
            var storage = this.GetUtility<IEnergyStorage>();
            CurrentEnergy.Value = storage.LoadEnergy(MaxEnergy);

            // Auto-save when changed
            CurrentEnergy.Register(value =>
            {
                storage.SaveEnergy(value);
            });
        }
    }
}
