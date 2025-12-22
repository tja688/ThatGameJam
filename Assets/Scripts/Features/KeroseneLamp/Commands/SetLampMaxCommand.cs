using QFramework;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class SetLampMaxCommand : AbstractCommand
    {
        private readonly int _max;

        public SetLampMaxCommand(int max)
        {
            _max = max;
        }

        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            var clamped = Mathf.Max(0, _max);
            if (model.MaxValue == clamped)
            {
                return;
            }

            model.SetMax(clamped);

            this.SendEvent(new LampCountChangedEvent
            {
                Count = model.CountValue,
                Max = clamped
            });
        }
    }
}
