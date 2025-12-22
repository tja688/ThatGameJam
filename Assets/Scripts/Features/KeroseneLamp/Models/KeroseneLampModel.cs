using QFramework;

namespace ThatGameJam.Features.KeroseneLamp.Models
{
    public class KeroseneLampModel : AbstractModel, IKeroseneLampModel
    {
        private const int DefaultLampMax = 3;

        private readonly BindableProperty<int> _lampCount = new BindableProperty<int>(0);
        private readonly BindableProperty<int> _lampMax = new BindableProperty<int>(DefaultLampMax);

        public IReadonlyBindableProperty<int> LampCount => _lampCount;
        public IReadonlyBindableProperty<int> LampMax => _lampMax;

        internal int CountValue => _lampCount.Value;
        internal int MaxValue => _lampMax.Value;
        internal int NextLampId { get; private set; }

        internal void SetCount(int value) => _lampCount.Value = value;
        internal void SetMax(int value) => _lampMax.Value = value;
        internal void UpdateNextLampId(int nextId) => NextLampId = nextId > NextLampId ? nextId : NextLampId;
        internal void ResetNextLampId() => NextLampId = 0;

        protected override void OnInit()
        {
            _lampCount.Value = 0;
            _lampMax.Value = DefaultLampMax;
            NextLampId = 0;
        }
    }
}
