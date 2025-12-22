using QFramework;

namespace ThatGameJam.Features.RunFailReset.Models
{
    public class RunFailResetModel : AbstractModel, IRunFailResetModel
    {
        private const int DefaultMaxDeaths = 3;

        private readonly BindableProperty<bool> _isFailed = new BindableProperty<bool>(false);
        private readonly BindableProperty<int> _maxDeaths = new BindableProperty<int>(DefaultMaxDeaths);

        public IReadonlyBindableProperty<bool> IsFailed => _isFailed;
        public IReadonlyBindableProperty<int> MaxDeaths => _maxDeaths;

        internal bool IsFailedValue => _isFailed.Value;
        internal int MaxDeathsValue => _maxDeaths.Value;
        internal void SetFailed(bool value) => _isFailed.Value = value;
        internal void SetMaxDeaths(int value) => _maxDeaths.Value = value;

        protected override void OnInit()
        {
            _isFailed.Value = false;
            _maxDeaths.Value = DefaultMaxDeaths;
        }
    }
}
