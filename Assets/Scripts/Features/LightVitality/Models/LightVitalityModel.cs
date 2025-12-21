using QFramework;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Models
{
    public class LightVitalityModel : AbstractModel, ILightVitalityModel
    {
        private const float DefaultMaxLight = 100f;
        private const float DefaultInitialLight = 100f;

        private readonly BindableProperty<float> _currentLight = new BindableProperty<float>();
        private readonly BindableProperty<float> _maxLight = new BindableProperty<float>();

        public IReadonlyBindableProperty<float> CurrentLight => _currentLight;
        public IReadonlyBindableProperty<float> MaxLight => _maxLight;

        internal float CurrentValue => _currentLight.Value;
        internal float MaxValue => _maxLight.Value;

        internal void SetCurrent(float value) => _currentLight.Value = value;
        internal void SetMax(float value) => _maxLight.Value = value;

        protected override void OnInit()
        {
            _maxLight.Value = Mathf.Max(0f, DefaultMaxLight);
            _currentLight.Value = Mathf.Clamp(DefaultInitialLight, 0f, _maxLight.Value);
        }
    }
}
