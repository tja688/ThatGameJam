using QFramework;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Hazard.Systems
{
    public interface IHazardSystem : ISystem, ICanSendCommand
    {
        void ApplyInstantKill(EDeathReason reason, Vector3 worldPos);
        void ApplyLightCostRatio(float ratio, ELightConsumeReason reason);
        void ApplyLightDrainRatio(float ratioPerSecond, float deltaTime, ELightConsumeReason reason);
    }
}
