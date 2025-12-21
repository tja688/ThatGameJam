using QFramework;

namespace ThatGameJam.Features.SafeZone.Models
{
    public class SafeZoneModel : AbstractModel, ISafeZoneModel
    {
        private readonly BindableProperty<int> _safeZoneCount = new BindableProperty<int>(0);
        private readonly BindableProperty<bool> _isSafe = new BindableProperty<bool>(false);

        public IReadonlyBindableProperty<int> SafeZoneCount => _safeZoneCount;
        public IReadonlyBindableProperty<bool> IsSafe => _isSafe;

        internal int CountValue => _safeZoneCount.Value;
        internal bool IsSafeValue => _isSafe.Value;

        internal void SetCount(int count) => _safeZoneCount.Value = count;
        internal void SetIsSafe(bool isSafe) => _isSafe.Value = isSafe;

        protected override void OnInit()
        {
            _safeZoneCount.Value = 0;
            _isSafe.Value = false;
        }
    }
}
