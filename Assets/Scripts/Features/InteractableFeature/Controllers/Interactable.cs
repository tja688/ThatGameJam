using PixelCrushers.DialogueSystem.Wrappers;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Commands;
using ThatGameJam.Features.BackpackFeature.Models;
using UnityEngine;

namespace ThatGameJam.Features.InteractableFeature.Controllers
{
    public enum InteractableType
    {
        Dialogue = 0,
        Pickup = 1
    }

    [RequireComponent(typeof(Collider2D))]
    public class Interactable : MonoBehaviour, IController
    {
        [Header("Interactable")]
        [SerializeField] private InteractableType type = InteractableType.Pickup;
        [SerializeField] private int priority = 0;
        [SerializeField] private string displayName;

        [Header("Dialogue")]
        [SerializeField] private DialogueSystemTrigger dialogueTrigger;

        [Header("Pickup")]
        [SerializeField] private ItemDefinition pickupItem;
        [SerializeField] private bool disableOnPickup = true;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;
        public InteractableType Type => type;
        public int Priority => priority;
        public string DisplayName => displayName;
        public ItemDefinition PickupItem => pickupItem;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("Interactable expects Collider2D.isTrigger = true.");
            }
        }

        public bool TryInteract(Transform actor)
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            switch (type)
            {
                case InteractableType.Dialogue:
                    return TriggerDialogue(actor);
                case InteractableType.Pickup:
                    return TriggerPickup();
                default:
                    return false;
            }
        }

        private bool TriggerDialogue(Transform actor)
        {
            if (dialogueTrigger == null)
            {
                LogKit.W($"Interactable '{name}' missing DialogueSystemTrigger.");
                return false;
            }

            if (actor != null)
            {
                dialogueTrigger.OnUse(actor);
            }
            else
            {
                dialogueTrigger.OnUse();
            }

            return true;
        }

        private bool TriggerPickup()
        {
            if (pickupItem == null)
            {
                LogKit.W($"Interactable '{name}' missing pickup ItemDefinition.");
                return false;
            }

            var instance = GetComponentInParent<IBackpackItemInstance>();
            if (instance != null && instance.Definition != null && instance.Definition != pickupItem)
            {
                LogKit.W($"Interactable '{name}' pickup item does not match instance definition.");
            }

            var addedIndex = this.SendCommand(new AddItemCommand(pickupItem, instance, 1));
            if (addedIndex < 0)
            {
                return false;
            }

            if (instance == null && disableOnPickup)
            {
                gameObject.SetActive(false);
            }

            return true;
        }
    }
}
