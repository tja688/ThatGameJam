using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.Darkness.Models;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.Darkness.Systems
{
    public class DarknessSystem : AbstractSystem, IDarknessSystem
    {
        private const float DefaultDrainPerSec = 5f;

        private bool _isSafeZone;
        private IUnRegister _safeZoneUnregister;
        private bool _loopPlaying;

        protected override void OnInit()
        {
            _safeZoneUnregister = this.RegisterEvent<SafeZoneStateChangedEvent>(OnSafeZoneStateChanged);
        }

        protected override void OnDeinit()
        {
            _safeZoneUnregister?.UnRegister();
            _safeZoneUnregister = null;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var model = this.GetModel<IDarknessModel>();
            var shouldPlay = !_isSafeZone && model.IsInDarkness.Value;
            if (!shouldPlay)
            {
                if (_loopPlaying)
                {
                    _loopPlaying = false;
                    AudioService.Stop("SFX-ENV-0002");
                }
                return;
            }

            if (!_loopPlaying)
            {
                _loopPlaying = true;
                AudioService.Play("SFX-ENV-0002");
            }

            var amount = DefaultDrainPerSec * deltaTime;
            if (amount <= 0f)
            {
                return;
            }

            this.SendCommand(new ConsumeLightCommand(amount, ELightConsumeReason.Darkness));
        }

        private void OnSafeZoneStateChanged(SafeZoneStateChangedEvent e)
        {
            _isSafeZone = e.IsSafe;
            if (_isSafeZone && _loopPlaying)
            {
                _loopPlaying = false;
                AudioService.Stop("SFX-ENV-0002");
            }
        }
    }
}
