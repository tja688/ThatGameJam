using QFramework;
using ThatGameJam.Features.Hazard.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Hazard.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class DamageVolume2D : MonoBehaviour, IController
    {
        [SerializeField] private float costRatio = 0.25f;
        [SerializeField] private float cooldownSeconds = 1f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private ELightConsumeReason reason = ELightConsumeReason.Script;

        private float _nextApplyTime;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("DamageVolume2D expects Collider2D.isTrigger = true.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApply(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApply(other);
        }

        private void TryApply(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (Time.time < _nextApplyTime)
            {
                return;
            }

            _nextApplyTime = Time.time + Mathf.Max(0f, cooldownSeconds);
            this.GetSystem<IHazardSystem>().ApplyLightCostRatio(costRatio, reason);
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
