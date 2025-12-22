using QFramework;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.DeathRespawn.Systems
{
    public interface IDeathRespawnSystem : ISystem, ICanSendCommand
    {
        void MarkDead(EDeathReason reason, Vector3 worldPos);
        void MarkRespawned(Vector3 worldPos);
    }
}
