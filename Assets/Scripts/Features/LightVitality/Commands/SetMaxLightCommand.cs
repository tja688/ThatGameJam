using QFramework;
using ThatGameJam.Features.LightVitality.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Commands
{
    public class SetMaxLightCommand : AbstractCommand
    {
        private readonly float _max;
        private readonly bool _clampCurrent;

        public SetMaxLightCommand(float max, bool clampCurrent)
        {
            _max = max;
            _clampCurrent = clampCurrent;
        }

        protected override void OnExecute()
        {
            var model = (LightVitalityModel)this.GetModel<ILightVitalityModel>();
            var sanitizedMax = Mathf.Max(0f, _max);
            var previousMax = model.MaxValue;
            var previousCurrent = model.CurrentValue;

            var maxChanged = !Mathf.Approximately(previousMax, sanitizedMax);
            if (maxChanged)
            {
                model.SetMax(sanitizedMax);
            }

            var newCurrent = previousCurrent;
            var currentChanged = false;

            if (_clampCurrent && previousCurrent > sanitizedMax)
            {
                newCurrent = sanitizedMax;
                model.SetCurrent(newCurrent);
                currentChanged = true;
            }

            if (maxChanged || currentChanged)
            {
                this.SendEvent(new LightChangedEvent
                {
                    Current = newCurrent,
                    Max = sanitizedMax
                });

                if (previousCurrent > 0f && newCurrent <= 0f)
                {
                    this.SendEvent(new LightDepletedEvent());
                }
            }
        }
    }
}
