using QFramework;
using ThatGameJam.Features.DeathRespawn.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.DeathRespawn.Commands
{
    public class MarkPlayerDeadCommand : AbstractCommand
    {
        private readonly EDeathReason _reason;
        private readonly Vector3 _worldPos;

        public MarkPlayerDeadCommand(EDeathReason reason, Vector3 worldPos)
        {
            _reason = reason;
            _worldPos = worldPos;
        }

        protected override void OnExecute()
        {
            var model = (DeathRespawnModel)this.GetModel<IDeathRespawnModel>();
            if (!model.IsAliveValue)
            {
                return;
            }

            model.SetAlive(false);
            model.IncrementDeathCount();

            this.SendEvent(new PlayerDiedEvent
            {
                Reason = _reason,
                WorldPos = _worldPos
            });

            this.SendEvent(new DeathCountChangedEvent
            {
                Count = model.DeathCountValue
            });
        }
    }
}
