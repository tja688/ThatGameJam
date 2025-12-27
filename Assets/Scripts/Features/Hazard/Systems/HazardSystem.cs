using QFramework;
using ThatGameJam.Features.DeathRespawn.Systems;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Hazard.Systems
{
    public class HazardSystem : AbstractSystem, IHazardSystem
    {
        protected override void OnInit()
        {
        }

        public void ApplyInstantKill(EDeathReason reason, Vector3 worldPos)
        {
            this.GetSystem<IDeathRespawnSystem>().MarkDead(reason, worldPos);
        }

        public void ApplyLightCostRatio(float ratio, ELightConsumeReason reason)
        {
            var amount = GetLightAmountFromRatio(ratio);
            if (amount <= 0f)
            {
                return;
            }

            this.SendCommand(new ConsumeLightCommand(amount, reason));
        }

        public void ApplyLightDrainRatio(float ratioPerSecond, float deltaTime, ELightConsumeReason reason)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var amount = GetLightAmountFromRatio(ratioPerSecond) * deltaTime;
            if (amount <= 0f)
            {
                return;
            }

            this.SendCommand(new ConsumeLightCommand(amount, reason));
        }

        private float GetLightAmountFromRatio(float ratio)
        {
            if (ratio <= 0f)
            {
                return 0f;
            }

            var model = this.GetModel<ILightVitalityModel>();
            var maxLight = model.MaxLight.Value;
            return maxLight * Mathf.Clamp01(ratio);
        }
    }
}
