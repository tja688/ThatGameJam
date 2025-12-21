using QFramework;
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
            if (deltaTime <= 0f || _isSafeZone)
            {
                return;
            }

            var model = this.GetModel<IDarknessModel>();
            if (!model.IsInDarkness.Value)
            {
                return;
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
        }
    }
}
