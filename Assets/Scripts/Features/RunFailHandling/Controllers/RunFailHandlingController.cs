using QFramework;
using ThatGameJam.Features.RunFailReset.Systems;
using UnityEngine;

namespace ThatGameJam.Features.RunFailHandling.Controllers
{
    public class RunFailHandlingController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        public void RequestHardResetFromTest()
        {
            this.GetSystem<IRunFailResetSystem>().RequestResetFromTest();
        }
    }
}
