using QFramework;

namespace ThatGameJam.Features.KeroseneLamp.Models
{
    public class KeroseneLampModel : AbstractModel, IKeroseneLampModel
    {
        private readonly BindableProperty<int> _lampCount = new BindableProperty<int>(0);

        public IReadonlyBindableProperty<int> LampCount => _lampCount;

        internal int CountValue => _lampCount.Value;
        internal int NextLampId { get; private set; }

        internal void SetCount(int value) => _lampCount.Value = value;
        internal void UpdateNextLampId(int nextId) => NextLampId = nextId > NextLampId ? nextId : NextLampId;
        internal void ResetNextLampId() => NextLampId = 0;

        protected override void OnInit()
        {
            _lampCount.Value = 0;
            NextLampId = 0;
        }
    }
}
