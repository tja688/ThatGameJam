using QFramework;
using ThatGameJam.Features.RunFailReset.Models;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.RunFailReset.Commands
{
    public class MarkRunFailedCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = (RunFailResetModel)this.GetModel<IRunFailResetModel>();
            if (model.IsFailedValue)
            {
                return;
            }

            model.SetFailed(true);
            this.SendEvent(new RunFailedEvent());
        }
    }
}
