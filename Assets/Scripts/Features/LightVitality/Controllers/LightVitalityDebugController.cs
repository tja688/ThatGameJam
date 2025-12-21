using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Queries;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Controllers
{
    public class LightVitalityDebugController : MonoBehaviour, IController
    {
        [SerializeField] private float addAmount = 10f;
        [SerializeField] private float consumeAmount = 10f;
        [SerializeField] private KeyCode addKey = KeyCode.Equals;
        [SerializeField] private KeyCode consumeKey = KeyCode.Minus;
        [SerializeField] private KeyCode setToMaxKey = KeyCode.Alpha0;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<LightChangedEvent>(OnLightChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(addKey))
            {
                this.SendCommand(new AddLightCommand(addAmount));
            }

            if (Input.GetKeyDown(consumeKey))
            {
                this.SendCommand(new ConsumeLightCommand(consumeAmount, ELightConsumeReason.Debug));
            }

            if (Input.GetKeyDown(setToMaxKey))
            {
                var max = this.SendQuery(new GetMaxLightQuery());
                this.SendCommand(new SetLightCommand(max));
            }
        }

        private void OnLightChanged(LightChangedEvent e)
        {
            LogKit.I($"Light {e.Current:0.##}/{e.Max:0.##}");
        }
    }
}
