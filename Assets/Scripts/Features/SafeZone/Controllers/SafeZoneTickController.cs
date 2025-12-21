using QFramework;
using ThatGameJam.Features.SafeZone.Systems;
using UnityEngine;

namespace ThatGameJam.Features.SafeZone.Controllers
{
    public class SafeZoneTickController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Update()
        {
            this.GetSystem<ISafeZoneSystem>().Tick(Time.deltaTime);
        }
    }
}
