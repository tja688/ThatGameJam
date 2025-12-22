using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Queries;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Controllers
{
    public class LightVitalityResetController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<RunResetEvent>(OnRunReset)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnRunReset(RunResetEvent e)
        {
            var max = this.SendQuery(new GetMaxLightQuery());
            this.SendCommand(new SetLightCommand(max));
        }
    }
}
