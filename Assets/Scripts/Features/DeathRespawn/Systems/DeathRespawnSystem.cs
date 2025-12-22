using QFramework;
using ThatGameJam.Features.DeathRespawn.Commands;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.DeathRespawn.Systems
{
    public class DeathRespawnSystem : AbstractSystem, IDeathRespawnSystem
    {
        protected override void OnInit()
        {
        }

        public void MarkDead(EDeathReason reason, Vector3 worldPos)
        {
            this.SendCommand(new MarkPlayerDeadCommand(reason, worldPos));
        }

        public void MarkRespawned(Vector3 worldPos)
        {
            this.SendCommand(new MarkPlayerRespawnedCommand(worldPos));
        }
    }
}
