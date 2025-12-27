using QFramework;
using ThatGameJam.Features.RunFailReset.Systems;
using UnityEngine;

namespace ThatGameJam.Features.RunFailReset.Controllers
{
    public class RunResetController : MonoBehaviour, IController
    {
        [SerializeField] private KeyCode resetKey = KeyCode.R;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Input.GetKeyDown(resetKey))
            {
                return;
            }

            this.GetSystem<IRunFailResetSystem>().RequestResetFromTest();
#endif
        }

        public void RequestReset()
        {
            this.GetSystem<IRunFailResetSystem>().RequestResetFromTest();
        }
    }
}
