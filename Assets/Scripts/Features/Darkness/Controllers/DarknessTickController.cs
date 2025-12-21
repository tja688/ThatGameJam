using QFramework;
using ThatGameJam.Features.Darkness.Systems;
using UnityEngine;

namespace ThatGameJam.Features.Darkness.Controllers
{
    public class DarknessTickController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Update()
        {
            this.GetSystem<IDarknessSystem>().Tick(Time.deltaTime);
        }
    }
}
