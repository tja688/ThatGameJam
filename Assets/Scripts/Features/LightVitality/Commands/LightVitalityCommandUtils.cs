using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.LightVitality.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Commands
{
    internal static class LightVitalityCommandUtils
    {
        public static void ApplyCurrentLight(LightVitalityModel model, float newValue, ICanSendEvent sender)
        {
            var max = model.MaxValue;
            var clamped = Mathf.Clamp(newValue, 0f, max);
            var previous = model.CurrentValue;

            if (Mathf.Approximately(previous, clamped))
            {
                return;
            }

            model.SetCurrent(clamped);
            sender.SendEvent(new LightChangedEvent
            {
                Current = clamped,
                Max = max
            });

            if (previous > 0f && clamped <= 0f)
            {
                sender.SendEvent(new LightDepletedEvent());
                AudioService.Play("SFX-META-0002");
            }
        }
    }
}
