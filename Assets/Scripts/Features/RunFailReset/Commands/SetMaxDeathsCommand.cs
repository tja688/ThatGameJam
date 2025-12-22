using QFramework;
using ThatGameJam.Features.RunFailReset.Models;
using UnityEngine;

namespace ThatGameJam.Features.RunFailReset.Commands
{
    public class SetMaxDeathsCommand : AbstractCommand
    {
        private readonly int _maxDeaths;

        public SetMaxDeathsCommand(int maxDeaths)
        {
            _maxDeaths = maxDeaths;
        }

        protected override void OnExecute()
        {
            var model = (RunFailResetModel)this.GetModel<IRunFailResetModel>();
            var clamped = Mathf.Max(1, _maxDeaths);
            if (model.MaxDeathsValue == clamped)
            {
                return;
            }

            model.SetMaxDeaths(clamped);
        }
    }
}
