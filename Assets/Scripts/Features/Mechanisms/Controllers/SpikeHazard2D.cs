using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.DeathRespawn.Controllers;
using ThatGameJam.Features.DeathRespawn.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class SpikeHazard2D : MechanismControllerBase
    {
        [SerializeField] private EDeathReason deathReason = EDeathReason.Script;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("SpikeHazard2D expects Collider2D.isTrigger = true.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<DeathController>() == null)
            {
                return;
            }

            var system = this.GetSystem<IDeathRespawnSystem>();
            system.MarkDead(deathReason, other.transform.position);
            AudioService.Play("SFX-ENV-0009", new AudioContext
            {
                Position = other.transform.position,
                HasPosition = true
            });
        }
    }
}
