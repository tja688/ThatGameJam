using QFramework;

namespace ThatGameJam.Features.Darkness.Models
{
    public class DarknessModel : AbstractModel, IDarknessModel
    {
        private readonly BindableProperty<bool> _isInDarkness = new BindableProperty<bool>(false);

        public IReadonlyBindableProperty<bool> IsInDarkness => _isInDarkness;

        internal bool CurrentValue => _isInDarkness.Value;

        internal void SetIsInDarkness(bool value) => _isInDarkness.Value = value;

        protected override void OnInit()
        {
            _isInDarkness.Value = false;
        }
    }
}
