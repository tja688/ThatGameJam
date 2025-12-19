using System.Collections;
using QFramework;
using UnityEngine;

namespace ThatGameJam
{
    public class EnergyAutoRecoverController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        [SerializeField] private float RecoverInterval = 1.0f;
        [SerializeField] private int RecoverAmount = 1;

        private void Start()
        {
            StartCoroutine(AutoRecoverCoroutine());
        }

        private IEnumerator AutoRecoverCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(RecoverInterval);
                this.SendCommand(new RecoverEnergyCommand(RecoverAmount));
            }
        }
    }
}
