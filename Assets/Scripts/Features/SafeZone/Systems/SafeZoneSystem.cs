using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.SafeZone.Models;

namespace ThatGameJam.Features.SafeZone.Systems
{
    public class SafeZoneSystem : AbstractSystem, ISafeZoneSystem
    {
        private const float DefaultRegenPerSec = 6f;


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
                return;
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
