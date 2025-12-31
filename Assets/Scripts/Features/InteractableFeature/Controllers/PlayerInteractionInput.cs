using QFramework;
using ThatGameJam.Features.InteractableFeature.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThatGameJam.Features.InteractableFeature.Controllers
{
    public class PlayerInteractionInput : MonoBehaviour, IController, ICanSendEvent
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference interact;
        [SerializeField] private InputActionReference scroll;

        [Header("Scroll Settings")]
        [SerializeField] private float scrollThreshold = 0.1f;
        [SerializeField] private float scrollCooldown = 0.08f;

        [Header("Lifecycle")]
        [SerializeField] private bool enableActionsOnEnable = true;

        private float _nextScrollTime;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            if (!enableActionsOnEnable)
            {
                return;
            }

            interact?.action?.Enable();
            scroll?.action?.Enable();
        }

        private void OnDisable()
        {
            if (!enableActionsOnEnable)
            {
                return;
            }

            interact?.action?.Disable();
            scroll?.action?.Disable();
        }

        private void Update()
        {
            var interactAction = interact?.action;
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                this.SendEvent(new InteractPressedEvent());
            }

            var scrollAction = scroll?.action;
            if (scrollAction == null)
            {
                return;
            }

            var scrollValue = scrollAction.ReadValue<Vector2>();
            var delta = scrollValue.y;
            if (Mathf.Approximately(delta, 0f))
            {
                delta = scrollAction.ReadValue<float>();
            }

            if (Mathf.Abs(delta) < scrollThreshold)
            {
                return;
            }

            if (Time.time < _nextScrollTime)
            {
                return;
            }

            _nextScrollTime = Time.time + scrollCooldown;

            if (delta > 0f)
            {
                this.SendEvent(new ScrollUpEvent { RawDelta = delta });
            }
            else
            {
                this.SendEvent(new ScrollDownEvent { RawDelta = delta });
            }
        }
    }
}
