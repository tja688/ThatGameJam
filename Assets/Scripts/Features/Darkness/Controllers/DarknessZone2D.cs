using UnityEngine;

namespace ThatGameJam.Features.Darkness.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class DarknessZone2D : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var sensor = other.GetComponentInParent<PlayerDarknessSensor>();
            if (sensor == null)
            {
                return;
            }

            sensor.NotifyZoneEnter(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var sensor = other.GetComponentInParent<PlayerDarknessSensor>();
            if (sensor == null)
            {
                return;
            }

            sensor.NotifyZoneExit(this);
        }
    }
}
