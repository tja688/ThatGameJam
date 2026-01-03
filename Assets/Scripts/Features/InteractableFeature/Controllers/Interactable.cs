using System.Collections.Generic;
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

        [Header("Interaction Icon")]
        [SerializeField] private GameObject interactionIconPrefab;
        [SerializeField] private Vector3 interactionIconOffset = Vector3.up;
        [SerializeField] private float iconSenseRange = 3f;
        [SerializeField] private LayerMask floorLayerMask;

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

        private const float PlayerLookupInterval = 1f;
        private const float PlayerRangeCheckInterval = 0.25f;
        private Transform _player;
        private GameObject _iconInstance;
        private float _nextPlayerLookupTime;
        private float _nextRangeCheckTime;
        private bool _playerInRange;
        private readonly HashSet<Collider2D> _floorContacts = new HashSet<Collider2D>();

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("Interactable expects Collider2D.isTrigger = true.");
            }

            if (floorLayerMask.value == 0)
            {
                var floorLayer = LayerMask.NameToLayer("Floor");
                if (floorLayer >= 0)
                {
                    floorLayerMask = 1 << floorLayer;
                }
            }
        }

        private void Update()
        {
            UpdateInteractionIcon();
            UpdateInteractionIconTransform();
        }

        private void OnDisable()
        {
            _floorContacts.Clear();
            ClearInteractionIcon();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAddFloorContact(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TryRemoveFloorContact(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null)
            {
                return;
            }

            TryAddFloorContact(collision.collider);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision == null)
            {
                return;
            }

            TryRemoveFloorContact(collision.collider);
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

        private void UpdateInteractionIcon()
        {
            if (interactionIconPrefab == null || iconSenseRange <= 0f)
            {
                ClearInteractionIcon();
                return;
            }

            if (_player == null && Time.time >= _nextPlayerLookupTime)
            {
                _nextPlayerLookupTime = Time.time + PlayerLookupInterval;
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                _player = playerObject != null ? playerObject.transform : null;
            }

            if (_player == null)
            {
                _playerInRange = false;
                ClearInteractionIcon();
                return;
            }

            if (Time.time >= _nextRangeCheckTime)
            {
                _nextRangeCheckTime = Time.time + PlayerRangeCheckInterval;
                var delta = (Vector2)(_player.position - transform.position);
                _playerInRange = delta.sqrMagnitude <= iconSenseRange * iconSenseRange;
            }

            if (!_playerInRange)
            {
                ClearInteractionIcon();
                return;
            }

            if (type == InteractableType.Pickup && !HasFloorContact())
            {
                ClearInteractionIcon();
                return;
            }

            EnsureInteractionIcon();
        }

        private void EnsureInteractionIcon()
        {
            if (_iconInstance != null)
            {
                return;
            }

            var rotation = interactionIconPrefab.transform.rotation;
            _iconInstance = Instantiate(interactionIconPrefab, transform.position + interactionIconOffset, rotation);
            UpdateInteractionIconTransform();
        }

        private void ClearInteractionIcon()
        {
            if (_iconInstance == null)
            {
                return;
            }

            Destroy(_iconInstance);
            _iconInstance = null;
        }

        private void UpdateInteractionIconTransform()
        {
            if (_iconInstance == null)
            {
                return;
            }

            _iconInstance.transform.position = transform.position + interactionIconOffset;
        }

        private void TryAddFloorContact(Collider2D other)
        {
            if (!IsFloor(other))
            {
                return;
            }

            _floorContacts.Add(other);
        }

        private void TryRemoveFloorContact(Collider2D other)
        {
            if (!IsFloor(other))
            {
                return;
            }

            _floorContacts.Remove(other);
        }

        private bool HasFloorContact()
        {
            return _floorContacts.Count > 0;
        }

        private bool IsFloor(Collider2D other)
        {
            if (other == null || floorLayerMask.value == 0)
            {
                return false;
            }

            return (floorLayerMask.value & (1 << other.gameObject.layer)) != 0;
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
