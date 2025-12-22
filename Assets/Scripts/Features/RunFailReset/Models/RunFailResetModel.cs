using QFramework;

namespace ThatGameJam.Features.RunFailReset.Models
{
    public class RunFailResetModel : AbstractModel, IRunFailResetModel
    {
        private readonly BindableProperty<bool> _isFailed = new BindableProperty<bool>(false);

        public IReadonlyBindableProperty<bool> IsFailed => _isFailed;

        internal bool IsFailedValue => _isFailed.Value;
        internal void SetFailed(bool value) => _isFailed.Value = value;

        protected override void OnInit()
        {
            _isFailed.Value = false;
        }
    }
}
