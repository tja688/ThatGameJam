using QFramework;
using UnityEngine;

namespace ThatGameJam
{
    public class GameRootApp : Architecture<GameRootApp>
    {
        protected override void Init()
        {
            // Register Utilities
            this.RegisterUtility<IEnergyStorage>(new EnergyStorage());

            // Register Models
            this.RegisterModel<IEnergyModel>(new EnergyModel());

            // Register Systems
            this.RegisterSystem<IEnergySystem>(new EnergySystem());
        }
    }
}
