using QFramework;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    public abstract class MechanismControllerBase : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;
    }
}
