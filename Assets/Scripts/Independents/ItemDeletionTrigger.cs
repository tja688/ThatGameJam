using QFramework;
using ThatGameJam.Features.BackpackFeature.Commands;
using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.InteractableFeature.Controllers;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Trigger script that removes a specific item from the player's backpack
    /// and deactivates an interactable script when the Player enters the trigger.
    /// </summary>
    public class ItemDeletionTrigger : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        [Header("Settings")]
        [Tooltip("The ID or Display Name of the item to remove from the backpack.")]
        public string ItemToRemove = "Letter";

        [Tooltip("The quantity to remove (usually 1).")]
        public int Quantity = 1;

        [Tooltip("The interactable script to deactivate after the item is removed.")]
        public Interactable InteractableToDisable;

        [Tooltip("The tag assigned to the player object.")]
        public string PlayerTag = "Player";

        private bool _triggered = false;

        public bool IsTriggered => _triggered;

        public void ApplyTriggeredState(bool triggered)
        {
            _triggered = triggered;
            if (_triggered && InteractableToDisable != null)
            {
                InteractableToDisable.enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_triggered) return;

            if (collision.CompareTag(PlayerTag))
            {
                ExecuteAction();
            }
        }

        private void ExecuteAction()
        {
            _triggered = true;

            // 1. Find the actual ID for the item (in case a Display Name was used)
            string actualId = GetActualItemId(ItemToRemove);

            if (!string.IsNullOrEmpty(actualId))
            {
                // 2. Remove the item from backpack
                this.SendCommand(new RemoveItemCommand(actualId, Quantity));
                // Debug.Log($"[ItemDeletionTrigger] Removed {Quantity}x {actualId} from backpack.");
            }

            // 3. Deactivate the interactable
            if (InteractableToDisable != null)
            {
                InteractableToDisable.enabled = false;
                // Debug.Log($"[ItemDeletionTrigger] Deactivated interactable: {InteractableToDisable.name}");
            }

            // Optional: Destroy the trigger object if it's no longer needed
            // Destroy(gameObject);
        }

        private string GetActualItemId(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return null;

            var model = this.GetModel<IBackpackModel>() as BackpackModel;
            if (model == null) return null;

            foreach (var item in model.Items)
            {
                if (item.Definition == null) continue;
                if (string.Equals(item.Definition.Id, idOrName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Definition.DisplayName, idOrName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return item.Definition.Id;
                }
            }
            return null;
        }
    }
}
