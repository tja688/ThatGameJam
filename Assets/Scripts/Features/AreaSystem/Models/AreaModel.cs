using QFramework;

namespace ThatGameJam.Features.AreaSystem.Models
{
    public class AreaModel : AbstractModel, IAreaModel
    {
        private readonly BindableProperty<string> _currentAreaId = new BindableProperty<string>(string.Empty);

        public IReadonlyBindableProperty<string> CurrentAreaId => _currentAreaId;

        internal string CurrentAreaValue => _currentAreaId.Value;

        internal void SetCurrentAreaId(string areaId)
        {
            _currentAreaId.Value = areaId ?? string.Empty;
        }

        protected override void OnInit()
        {
            _currentAreaId.Value = string.Empty;
        }
    }
}
