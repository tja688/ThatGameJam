using QFramework;
using ThatGameJam.Features.DeathRespawn.Models;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.DeathRespawn.Commands
{
    public class ResetDeathCountCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = (DeathRespawnModel)this.GetModel<IDeathRespawnModel>();
            if (model.DeathCountValue == 0)
            {
                return;
            }

            model.ResetDeathCount();

            this.SendEvent(new DeathCountChangedEvent
            {
                Count = model.DeathCountValue
            });
        }
    }
}
