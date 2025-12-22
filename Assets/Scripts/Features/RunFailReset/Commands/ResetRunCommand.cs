using QFramework;
using ThatGameJam.Features.RunFailReset.Models;

namespace ThatGameJam.Features.RunFailReset.Commands
{
    public class ResetRunCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = (RunFailResetModel)this.GetModel<IRunFailResetModel>();
            if (!model.IsFailedValue)
            {
                return;
            }

            model.SetFailed(false);
        }
    }
}
