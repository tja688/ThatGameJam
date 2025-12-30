using ThatGameJam.Features.Shared;
using ThatGameJam.Independents.Audio;
using UnityEngine;

namespace ThatGameJam.Features.DeathRespawn.Controllers
{
    public class KillVolume2D : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var deathController = other.GetComponentInParent<DeathController>();
            if (deathController == null)
            {
                return;
            }

            deathController.KillAt(EDeathReason.Fall, other.transform.position);
            AudioService.Play("SFX-ENV-0011", new AudioContext
            {
                Position = other.transform.position,
                HasPosition = true
            });
        }
    }
}
