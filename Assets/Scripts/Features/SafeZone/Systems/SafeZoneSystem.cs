using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.SafeZone.Models;

namespace ThatGameJam.Features.SafeZone.Systems
{
    public class SafeZoneSystem : AbstractSystem, ISafeZoneSystem
    {
        private const float DefaultRegenPerSec = 6f;
        private bool _loopPlaying;


        protected override void OnInit()
        {
        }


        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var model = this.GetModel<ISafeZoneModel>();
            if (!model.IsSafe.Value)
            {
                if (_loopPlaying)
                {
                    _loopPlaying = false;
                    AudioService.Stop("SFX-ENV-0004");
                }
                return;
            }

            if (!_loopPlaying)
            {
                _loopPlaying = true;
                AudioService.Play("SFX-ENV-0004");
            }

            var amount = DefaultRegenPerSec * deltaTime;
            if (amount <= 0f)
            {
                return;
            }

            this.SendCommand(new AddLightCommand(amount));
        }
    }
}
