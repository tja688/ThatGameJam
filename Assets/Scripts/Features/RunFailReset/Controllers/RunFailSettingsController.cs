using QFramework;
using ThatGameJam.Features.RunFailReset.Commands;
using UnityEngine;

namespace ThatGameJam.Features.RunFailReset.Controllers
{
    public class RunFailSettingsController : MonoBehaviour, IController
    {
        [SerializeField] private int maxDeathsPerLevel = 3;
        [SerializeField] private bool applyOnEnable = true;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            if (!applyOnEnable)
            {
                return;
            }

            this.SendCommand(new SetMaxDeathsCommand(maxDeathsPerLevel));
        }
    }
}
