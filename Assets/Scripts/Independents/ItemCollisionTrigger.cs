using ThatGameJam.Features.InteractableFeature.Controllers;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Detects collision with a "Soil" tagged object, activates a target, 
    /// optional deactivates an interactable, and destroys itself.
    /// Typically used for seed items hitting the ground.
    /// </summary>
    public class ItemCollisionTrigger : MonoBehaviour
    {
        [Header("Target Configuration")]
        [Tooltip("The GameObject to activate when collision occurs.")]
        public GameObject TargetToActivate;

        [Tooltip("Optional: An Interactable component to deactivate when collision occurs.")]
        public Interactable InteractableToDeactivate;

        [Header("Detection Settings")]
        [Tooltip("The tag to look for (Default: Soil).")]
        public string TargetTag = "Soil";
        [Tooltip("If true, use Trigger events; if false, use Collision events.")]
        public bool UseTrigger = true;

        private bool _triggered = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (UseTrigger) CheckAndTrigger(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!UseTrigger) CheckAndTrigger(collision.gameObject);
        }

        private void CheckAndTrigger(GameObject other)
        {
            if (_triggered) return;

            if (other.CompareTag(TargetTag))
            {
                _triggered = true;

                // 1. Activate target (e.g., the vine)
                if (TargetToActivate != null)
                {
                    TargetToActivate.SetActive(true);
                }

                // 2. Deactivate specified interactable (e.g., the seed pickup point)
                if (InteractableToDeactivate != null)
                {
                    InteractableToDeactivate.enabled = false;
                }

                // Debug.Log($"[ItemCollisionTrigger] Hit {TargetTag}, activating {TargetToActivate?.name} and deactivating interactable.");

                Destroy(gameObject);
            }
        }
    }
}
