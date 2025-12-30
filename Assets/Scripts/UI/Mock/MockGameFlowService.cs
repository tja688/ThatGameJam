using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;

namespace ThatGameJam.UI.Mock
{
    public class MockGameFlowService : IGameFlowService
    {
        public void StartNewGame()
        {
            Debug.Log("[Mock] StartNewGame called.");
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("[Mock] ReturnToMainMenu called.");
        }
    }
}
