using QFramework;

namespace ThatGameJam.Features.DeathRespawn.Models
{
    public class DeathRespawnModel : AbstractModel, IDeathRespawnModel
    {
        private readonly BindableProperty<bool> _isAlive = new BindableProperty<bool>(true);
        private readonly BindableProperty<int> _deathCount = new BindableProperty<int>(0);

        public IReadonlyBindableProperty<bool> IsAlive => _isAlive;
        public IReadonlyBindableProperty<int> DeathCount => _deathCount;

        internal bool IsAliveValue => _isAlive.Value;
        internal int DeathCountValue => _deathCount.Value;

        internal void SetAlive(bool value) => _isAlive.Value = value;
        internal void IncrementDeathCount() => _deathCount.Value++;

        protected override void OnInit()
        {
            _isAlive.Value = true;
            _deathCount.Value = 0;
        }
    }
}
