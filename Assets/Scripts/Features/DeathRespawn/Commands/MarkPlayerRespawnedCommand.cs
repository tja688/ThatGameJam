using QFramework;
using ThatGameJam.Features.DeathRespawn.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.DeathRespawn.Commands
{
    public class MarkPlayerRespawnedCommand : AbstractCommand
    {
        private readonly Vector3 _worldPos;

        public MarkPlayerRespawnedCommand(Vector3 worldPos)
        {
            _worldPos = worldPos;
        }

        protected override void OnExecute()
        {
            var model = (DeathRespawnModel)this.GetModel<IDeathRespawnModel>();
            if (model.IsAliveValue)
            {
                return;
            }

            model.SetAlive(true);

            this.SendEvent(new PlayerRespawnedEvent
            {
                WorldPos = _worldPos
            });
        }
    }
}
