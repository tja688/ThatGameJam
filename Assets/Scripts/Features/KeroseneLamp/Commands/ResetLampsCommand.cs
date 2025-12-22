using QFramework;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class ResetLampsCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            var countChanged = model.CountValue != 0;

            model.SetCount(0);
            model.ResetNextLampId();

            if (countChanged)
            {
                this.SendEvent(new LampCountChangedEvent
                {
                    Count = 0
                });
            }
        }
    }
}
