using Animancer;
using UnityEngine;

namespace ThatGameJam.Test
{
    /// <summary>
    /// Spine + Animancer Smoke Test Script.
    /// This script demonstrates how to use Animancer to control a Spine character (via SkeletonMecanim).
    /// Focuses on: Basic playback, State switching (Idle/Move), Layering (Expressions), and Cross-fading.
    /// </summary>
    [SelectionBase]
    public sealed class SpineAnimancerSmokeTest : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private AnimancerComponent _animancer;

        [Header("Base Animations (Layer 0)")]
        [SerializeField] private ClipTransition _idle;
        [SerializeField] private ClipTransition _walk;
        [SerializeField] private ClipTransition _salute;

        [Header("Overlay Animations (Layer 1)")]
        [SerializeField] private ClipTransition _blink;

        [Header("Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _fadeDuration = 0.25f;
        [SerializeField] private KeyCode _saluteKey = KeyCode.G;
        [SerializeField] private KeyCode _blinkKey = KeyCode.B;

        private void OnValidate()
        {
            if (_animancer == null) _animancer = GetComponent<AnimancerComponent>();
        }

        private void Start()
        {
            // Initial state
            if (_idle != null) _animancer.Play(_idle);
            
            // Set up Layer 1 for overlays (e.g. blinking, facial expressions)
            _animancer.Layers[1].SetDebugName("Overlay / Expressions");
        }

        private void Update()
        {
            HandleMovement();
            HandleActions();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

            if (isMoving)
            {
                // Play Walk with automatic cross-fade
                if (_walk != null) _animancer.Play(_walk, _fadeDuration);
                
                // Simple 2D flip logic
                if (horizontal != 0)
                {
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (horizontal > 0 ? 1 : -1);
                    transform.localScale = scale;
                }
                
                // Translate for testing movement
                transform.Translate(new Vector3(horizontal, vertical, 0).normalized * _moveSpeed * Time.deltaTime);
            }
            else
            {
                // Back to Idle if not moving and not performing a one-shot action
                // Using IsPlaying(HasKey) is efficient.
                if (_idle != null && !_animancer.IsPlaying(_salute))
                {
                    _animancer.Play(_idle, _fadeDuration);
                }
            }
        }

        private void HandleActions()
        {
            // Test One-Shot: Salute
            if (Input.GetKeyDown(_saluteKey) && _salute != null)
            {
                var state = _animancer.Play(_salute);
                
                // Using Animancer 8.0 event handling
                // state.Events(this) ensures we are the owner and avoid conflicts
                state.Events(this).OnEnd = () => 
                {
                    if (!IsMoving()) _animancer.Play(_idle, _fadeDuration);
                };
            }

            // Test Layering: Blink (played on Layer 1)
            if (Input.GetKeyDown(_blinkKey) && _blink != null)
            {
                // Play on Layer 1 to test simultaneous animations
                _animancer.Layers[1].Play(_blink);
            }
        }

        private bool IsMoving()
        {
            return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        }

        [ContextMenu("Debug/Log Current States")]
        public void LogStates()
        {
            Debug.Log($"Layer 0 current state: {_animancer.Layers[0].CurrentState}");
            Debug.Log($"Layer 1 current state: {_animancer.Layers[1].CurrentState}");
        }
    }
}
