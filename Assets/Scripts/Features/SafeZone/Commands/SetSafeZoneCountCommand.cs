using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.SafeZone.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.SafeZone.Commands
{
    public class SetSafeZoneCountCommand : AbstractCommand
    {
        private readonly int _count;

        public SetSafeZoneCountCommand(int count)
        {
            _count = count;
        }

        protected override void OnExecute()
        {
            var model = (SafeZoneModel)this.GetModel<ISafeZoneModel>();
            var clamped = Mathf.Max(0, _count);
            if (model.CountValue == clamped)
            {
                return;
            }

            model.SetCount(clamped);
            var isSafe = clamped > 0;
            if (model.IsSafeValue != isSafe)
            {
                model.SetIsSafe(isSafe);
            }

            this.SendEvent(new SafeZoneStateChangedEvent
            {
                SafeZoneCount = clamped,
                IsSafe = isSafe
            });

            if (isSafe)
            {
                AudioService.Play("SFX-ENV-0003");
            }
            else
            {
                AudioService.Stop("SFX-ENV-0003");
            }
        }
    }
}
