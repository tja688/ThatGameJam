using QFramework;
using UnityEngine;

namespace ThatGameJam
{
    public interface IEnergyStorage : IUtility
    {
        void SaveEnergy(int currentEnergy);
        int LoadEnergy(int defaultEnergy);
    }

    public class EnergyStorage : IEnergyStorage
    {
        private const string Key = "ENERGY_CURRENT";

        public void SaveEnergy(int currentEnergy)
        {
            PlayerPrefs.SetInt(Key, currentEnergy);
            PlayerPrefs.Save();
        }

        public int LoadEnergy(int defaultEnergy)
        {
            return PlayerPrefs.GetInt(Key, defaultEnergy);
        }
    }
}
