using QFramework;
using ThatGameJam.Features.Hazard.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Hazard.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class HazardVolume2D : MonoBehaviour, IController
    {
        public enum HazardMode
        {
            InstantKill,
            DrainFast
        }

        [SerializeField] private HazardMode mode = HazardMode.InstantKill;
        [SerializeField] private float drainRatioPerSecond = 0.25f;
        [SerializeField] private ELightConsumeReason drainReason = ELightConsumeReason.Script;
        [SerializeField] private EDeathReason deathReason = EDeathReason.Script;
        [SerializeField] private string playerTag = "Player";

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("HazardVolume2D expects Collider2D.isTrigger = true.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (mode == HazardMode.InstantKill)
            {
                this.GetSystem<IHazardSystem>().ApplyInstantKill(deathReason, other.transform.position);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsPlayer(other) || mode != HazardMode.DrainFast)
            {
                return;
            }

            this.GetSystem<IHazardSystem>().ApplyLightDrainRatio(drainRatioPerSecond, Time.deltaTime, drainReason);
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }
    }
}
