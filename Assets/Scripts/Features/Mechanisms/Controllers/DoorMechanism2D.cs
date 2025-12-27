using QFramework;
using ThatGameJam.Features.DoorGate.Events;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    public class DoorMechanism2D : MechanismControllerBase
    {
        [SerializeField] private string doorId;
        [SerializeField] private GameObject doorVisual;
        [SerializeField] private Collider2D doorCollider;
        [SerializeField] private bool disableColliderOnOpen = true;
        [SerializeField] private bool hideVisualOnOpen = true;
        [SerializeField] private bool startOpen;

        private bool _isOpen;

        protected override void OnEnable()
        {
            base.OnEnable();
            this.RegisterEvent<DoorStateChangedEvent>(OnDoorStateChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void Awake()
        {
            if (doorCollider == null)
            {
                doorCollider = GetComponent<Collider2D>();
            }

            _isOpen = startOpen;
            ApplyState();
        }

        protected override void OnHardReset()
        {
            _isOpen = startOpen;
            ApplyState();
        }

        private void OnDoorStateChanged(DoorStateChangedEvent e)
        {
            if (!string.IsNullOrEmpty(doorId) && e.DoorId != doorId)
            {
                return;
            }

            _isOpen = e.IsOpen;
            ApplyState();
        }

        private void ApplyState()
        {
            if (hideVisualOnOpen && doorVisual != null)
            {
                doorVisual.SetActive(!_isOpen);
            }

            if (disableColliderOnOpen && doorCollider != null)
            {
                doorCollider.enabled = !_isOpen;
            }
        }
    }
}
