using UnityEngine;

namespace ThatGameJam.Features.SafeZone.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class SafeZone2D : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var sensor = other.GetComponentInParent<PlayerSafeZoneSensor>();
            if (sensor == null)
            {
                return;
            }

            sensor.NotifyZoneEnter(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var sensor = other.GetComponentInParent<PlayerSafeZoneSensor>();
            if (sensor == null)
            {
                return;
            }

            sensor.NotifyZoneExit(this);
        }
    }
}
