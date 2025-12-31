using ThatGameJam.Features.InteractableFeature.Controllers;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Place this script on the STATIC "Soil" object in the scene.
    /// It waits for a "Seed" tagged object to hit it, then triggers the growth logic.
    /// This avoids reference loss when the Seed is instantiated from the backpack.
    /// </summary>
    public class SoilCollisionListener : MonoBehaviour
    {
        [Header("References (Pre-bind in Scene)")]
        [Tooltip("The vine or plant object that should grow here.")]
        public GameObject TargetToActivate;

        [Tooltip("Optional: An Interactable component (e.g. the original seed source) to deactivate.")]
        public Interactable InteractableToDeactivate;

        [Header("Detection Settings")]
        [Tooltip("The tag of the dropped item (Default: Seed).")]
        public string SeedTag = "Seed";
        [Tooltip("If true, use Trigger events; if false, use Collision events.")]
        public bool UseTrigger = true;

        private bool _triggered = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (UseTrigger) HandleCollision(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!UseTrigger) HandleCollision(collision.gameObject);
        }

        private void HandleCollision(GameObject other)
        {
            if (_triggered) return;

            // Check if the falling object is the Seed
            if (other.CompareTag(SeedTag))
            {
                _triggered = true;

                // 1. Activate the plant (it likely has AutoGrowthController on it)
                if (TargetToActivate != null)
                {
                    TargetToActivate.SetActive(true);
                }

                // 2. Deactivate specified interactable
                if (InteractableToDeactivate != null)
                {
                    InteractableToDeactivate.enabled = false;
                }

                // 3. Destroy the dropped seed instance
                Destroy(other);

                // Debug.Log($"[SoilCollisionListener] Seed hit soil. Growing plant and destroying seed instance.");
            }
        }
    }
}
