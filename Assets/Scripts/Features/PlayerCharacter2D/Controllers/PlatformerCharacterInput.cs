using ThatGameJam.Features.PlayerCharacter2D.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThatGameJam.Features.PlayerCharacter2D.Controllers
{
    public class PlatformerCharacterInput : MonoBehaviour, IPlatformerFrameInputSource
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _jump;
        [SerializeField] private InputActionReference _grab;

        [Header("Lifecycle")]
        [SerializeField] private bool _enableActionsOnEnable = true;

        private void OnEnable()
        {
            if (!_enableActionsOnEnable) return;
            _move?.action?.Enable();
            _jump?.action?.Enable();
            _grab?.action?.Enable();
        }

        private void OnDisable()
        {
            if (!_enableActionsOnEnable) return;
            _move?.action?.Disable();
            _jump?.action?.Disable();
            _grab?.action?.Disable();
        }

        public PlatformerFrameInput ReadInput()
        {
            var move = _move != null ? _move.action.ReadValue<Vector2>() : Vector2.zero;

            var jumpAction = _jump?.action;
            var jumpDown = jumpAction != null && jumpAction.WasPressedThisFrame();
            var jumpHeld = jumpAction != null && jumpAction.IsPressed();

            var grabAction = _grab?.action;
            var grabHeld = grabAction != null && grabAction.IsPressed();

            return new PlatformerFrameInput
            {
                Move = move,
                JumpDown = jumpDown,
                JumpHeld = jumpHeld,
                GrabHeld = grabHeld,
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_move == null || _jump == null)
            {
                // Editor-only: help catch missing action references early.
                return;
            }
        }
#endif
    }
}
