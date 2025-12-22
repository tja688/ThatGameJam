using QFramework;
using ThatGameJam.Features.DeathRespawn.Models;
using ThatGameJam.Features.DeathRespawn.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.DeathRespawn.Controllers
{
    public class DeathController : MonoBehaviour, IController
    {
        [SerializeField] private bool listenToLightDepleted = true;
        [SerializeField] private bool useFallCheck = true;
        [SerializeField] private float fallYThreshold = -10f;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            if (listenToLightDepleted)
            {
                this.RegisterEvent<LightDepletedEvent>(OnLightDepleted)
                    .UnRegisterWhenDisabled(gameObject);
            }
        }

        private void Update()
        {
            if (!useFallCheck)
            {
                return;
            }

            var model = this.GetModel<IDeathRespawnModel>();
            if (!model.IsAlive.Value)
            {
                return;
            }

            if (transform.position.y < fallYThreshold)
            {
                Kill(EDeathReason.Fall);
            }
        }

        public void Kill(EDeathReason reason)
        {
            KillAt(reason, transform.position);
        }

        public void KillAt(EDeathReason reason, Vector3 worldPos)
        {
            var system = this.GetSystem<IDeathRespawnSystem>();
            system.MarkDead(reason, worldPos);
        }

        private void OnLightDepleted(LightDepletedEvent e)
        {
            Kill(EDeathReason.LightDepleted);
        }
    }
}
