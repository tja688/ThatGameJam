using QFramework;
using ThatGameJam.Features.DoorGate.Commands;
using UnityEngine;

namespace ThatGameJam.Features.DoorGate.Controllers
{
    public class DoorGateConfig2D : MonoBehaviour, IController
    {
        [SerializeField] private string doorId;
        [SerializeField] private int requiredFlowerCount = 2;
        [SerializeField] private bool allowCloseOnDeactivate;
        [SerializeField] private bool startOpen;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.SendCommand(new RegisterDoorConfigCommand(doorId, requiredFlowerCount, allowCloseOnDeactivate, startOpen));
        }

        private void OnDisable()
        {
            this.SendCommand(new UnregisterDoorCommand(doorId));
        }
    }
}
