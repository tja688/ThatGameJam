using System;
using System.Collections.Generic;
using QFramework;
using Spine;
using Spine.Unity;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Controllers
{
    [DisallowMultipleComponent]
    public class SpineCharacterAnimDriver : MonoBehaviour, IController
    {
        [Serializable]
        public class BaseAnimationMap
        {
            public string Idle;
            public string Run;
            public string JumpUp;
            public string Fall;
            public string Land;
            public string ClimbMove;
            public string ClimbIdle;
        }

        [Serializable]
        public class OverlayAnimationMap
        {
            public string Breath;
            public string Blink;
            public string UpperBody;
            public string Hit;
        }

        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private int baseTrack = 0;
        [SerializeField] private int overlayTrack = 1;

        [Header("References")]
        [SerializeField] private PlatformerCharacterController controller;
        [SerializeField] private Rigidbody2D rb;

        [Header("Thresholds")]
        [SerializeField] private float speedThreshold = 0.1f;
        [SerializeField] private float jumpThreshold = 0.1f;
        [SerializeField] private float fallThreshold = -0.1f;
        [SerializeField] private float climbMoveThreshold = 0.05f;
        [SerializeField] private float climbIdleTimeScale = 0.2f;

        [Header("Mixing")]
        [SerializeField] private float baseMixDuration = 0.08f;
        [SerializeField] private float overlayFadeIn = 0.1f;
        [SerializeField] private float overlayFadeOut = 0.1f;
        [SerializeField, Range(0f, 1f)] private float overlayAlpha = 1f;

        [Header("Animation Names")]
        [SerializeField] private BaseAnimationMap baseAnimations = new BaseAnimationMap();
        [SerializeField] private OverlayAnimationMap overlayAnimations = new OverlayAnimationMap();

        [Header("Facing")]
        [SerializeField] private bool enableFacing = true;
        [SerializeField] private bool faceByMoveInput = true;
        [SerializeField] private float facingDeadZone = 0.05f;

        [Header("Debug Browser")]
        [SerializeField] private bool enableDebugBrowser;
        [SerializeField] private KeyCode previousKey = KeyCode.LeftBracket;
        [SerializeField] private KeyCode nextKey = KeyCode.RightBracket;

        private IPlayerCharacter2DModel _model;
        private TrackEntry _landEntry;
        private string _currentBase;
        private string _currentOverlay;
        private string _overlayLoopName;
        private bool _wasGrounded;
        private float _defaultScaleX = 1f;
        private SkeletonData _skeletonData;
        private string[] _animationNames = Array.Empty<string>();
        private int _debugIndex;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
            }

            if (controller == null)
            {
                controller = GetComponentInParent<PlatformerCharacterController>();
            }

            if (rb == null)
            {
                rb = GetComponentInParent<Rigidbody2D>();
            }

            if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
            {
                _defaultScaleX = Mathf.Abs(skeletonAnimation.Skeleton.ScaleX);
            }
        }

        private void Start()
        {
            if (skeletonAnimation == null)
            {
                Debug.LogWarning("[SpineCharacterAnimDriver] Missing SkeletonAnimation reference.", this);
                enabled = false;
                return;
            }

            _model = this.GetModel<IPlayerCharacter2DModel>();
            CacheSkeletonData();
            SetupOverlayLoop();
        }

        private void Update()
        {
            if (skeletonAnimation == null)
            {
                return;
            }

            if (enableDebugBrowser && HandleDebugBrowser())
            {
                return;
            }

            ReadState(out bool isGrounded, out bool isClimbing, out Vector2 velocity, out Vector2 moveInput);
            UpdateFacing(moveInput, velocity);
            UpdateBaseAnimation(isGrounded, isClimbing, velocity);
        }

        private void ReadState(out bool isGrounded, out bool isClimbing, out Vector2 velocity, out Vector2 moveInput)
        {
            // Reads from IPlayerCharacter2DModel (updated by PlatformerCharacterController/TickFixedStepCommand),
            // falls back to PlatformerCharacterController or Rigidbody2D when model is unavailable.
            isGrounded = false;
            isClimbing = false;
            velocity = Vector2.zero;
            moveInput = Vector2.zero;

            if (_model != null)
            {
                isGrounded = _model.Grounded.Value;
                isClimbing = _model.IsClimbing.Value;
                velocity = _model.Velocity.Value;
                moveInput = _model.FrameInput.Move;
                return;
            }

            if (controller != null)
            {
                isGrounded = controller.IsGrounded;
                isClimbing = controller.IsClimbing;
                moveInput = controller.MoveInput;
            }

            if (rb != null)
            {
                velocity = rb.linearVelocity;
            }
        }

        private void UpdateFacing(Vector2 moveInput, Vector2 velocity)
        {
            if (!enableFacing || skeletonAnimation == null || skeletonAnimation.Skeleton == null)
            {
                return;
            }

            float facingX = faceByMoveInput ? moveInput.x : velocity.x;
            if (Mathf.Abs(facingX) < facingDeadZone)
            {
                return;
            }

            float sign = Mathf.Sign(facingX);
            skeletonAnimation.Skeleton.ScaleX = _defaultScaleX * sign;
        }

        private void UpdateBaseAnimation(bool isGrounded, bool isClimbing, Vector2 velocity)
        {
            if (_landEntry != null && !_landEntry.IsComplete)
            {
                if (isClimbing || !isGrounded)
                {
                    _landEntry = null;
                }
                else
                {
                    return;
                }
            }

            if (_landEntry != null && _landEntry.IsComplete)
            {
                _landEntry = null;
            }

            bool justLanded = !_wasGrounded && isGrounded;
            _wasGrounded = isGrounded;

            if (isClimbing)
            {
                float climbSpeed = velocity.magnitude;
                bool moving = climbSpeed > climbMoveThreshold;
                string anim = moving ? baseAnimations.ClimbMove : baseAnimations.ClimbIdle;
                if (string.IsNullOrEmpty(anim))
                {
                    anim = baseAnimations.ClimbMove;
                }

                TrackEntry entry = SetBaseAnimation(anim, true);
                if (!moving && entry != null && string.IsNullOrEmpty(baseAnimations.ClimbIdle))
                {
                    entry.TimeScale = climbIdleTimeScale;
                }
                return;
            }

            if (!isGrounded)
            {
                string anim = null;
                if (velocity.y > jumpThreshold)
                {
                    anim = baseAnimations.JumpUp;
                }
                else if (velocity.y < fallThreshold)
                {
                    anim = baseAnimations.Fall;
                }

                if (string.IsNullOrEmpty(anim))
                {
                    anim = !string.IsNullOrEmpty(baseAnimations.Fall) ? baseAnimations.Fall : baseAnimations.JumpUp;
                }

                SetBaseAnimation(anim, true);
                return;
            }

            string groundedAnim = Mathf.Abs(velocity.x) > speedThreshold ? baseAnimations.Run : baseAnimations.Idle;
            if (justLanded && !string.IsNullOrEmpty(baseAnimations.Land) && HasAnimation(baseAnimations.Land))
            {
                var landEntry = skeletonAnimation.AnimationState.SetAnimation(baseTrack, baseAnimations.Land, false);
                landEntry.MixDuration = baseMixDuration;
                skeletonAnimation.AnimationState.AddAnimation(baseTrack, groundedAnim, true, 0f);
                _landEntry = landEntry;
                _currentBase = baseAnimations.Land;
                return;
            }

            SetBaseAnimation(groundedAnim, true);
        }

        private TrackEntry SetBaseAnimation(string name, bool loop)
        {
            if (string.IsNullOrEmpty(name) || !HasAnimation(name))
            {
                return null;
            }

            var state = skeletonAnimation.AnimationState;
            var current = state.GetCurrent(baseTrack);
            if (current != null && current.Animation != null && current.Animation.Name == name && !current.IsComplete)
            {
                return current;
            }

            var entry = state.SetAnimation(baseTrack, name, loop);
            entry.MixDuration = baseMixDuration;
            _currentBase = name;
            return entry;
        }

        private void SetupOverlayLoop()
        {
            _overlayLoopName = !string.IsNullOrEmpty(overlayAnimations.Breath)
                ? overlayAnimations.Breath
                : overlayAnimations.Blink;

            if (!string.IsNullOrEmpty(_overlayLoopName))
            {
                SetOverlayLoop(_overlayLoopName);
            }
        }

        private void SetOverlayLoop(string name)
        {
            if (string.IsNullOrEmpty(name) || !HasAnimation(name))
            {
                return;
            }

            if (_currentOverlay == name)
            {
                return;
            }

            var entry = skeletonAnimation.AnimationState.SetAnimation(overlayTrack, name, true);
            entry.MixDuration = overlayFadeIn;
            entry.Alpha = overlayAlpha;
            _currentOverlay = name;
        }

        private void ClearOverlay()
        {
            if (skeletonAnimation == null)
            {
                return;
            }

            skeletonAnimation.AnimationState.SetEmptyAnimation(overlayTrack, overlayFadeOut);
            _currentOverlay = null;
        }

        public void PlayHitOverlay()
        {
            PlayOverlayOnce(overlayAnimations.Hit);
        }

        public void PlayUpperBodyOverlay(bool enable)
        {
            if (enable)
            {
                PlayOverlayOnce(overlayAnimations.UpperBody);
                return;
            }

            if (!string.IsNullOrEmpty(_overlayLoopName))
            {
                SetOverlayLoop(_overlayLoopName);
            }
            else
            {
                ClearOverlay();
            }
        }

        public void PlayOverlayOnce(string animationName)
        {
            if (string.IsNullOrEmpty(animationName) || !HasAnimation(animationName))
            {
                return;
            }

            var entry = skeletonAnimation.AnimationState.SetAnimation(overlayTrack, animationName, false);
            entry.MixDuration = overlayFadeIn;
            entry.Alpha = overlayAlpha;

            if (!string.IsNullOrEmpty(_overlayLoopName))
            {
                skeletonAnimation.AnimationState.AddAnimation(overlayTrack, _overlayLoopName, true, 0f);
            }
            else
            {
                skeletonAnimation.AnimationState.AddEmptyAnimation(overlayTrack, overlayFadeOut, 0f);
            }

            _currentOverlay = animationName;
        }

        private void CacheSkeletonData()
        {
            if (skeletonAnimation == null)
            {
                return;
            }

            _skeletonData = skeletonAnimation.Skeleton != null
                ? skeletonAnimation.Skeleton.Data
                : skeletonAnimation.SkeletonDataAsset?.GetSkeletonData(true);

            if (_skeletonData == null)
            {
                return;
            }

            var list = new List<string>(_skeletonData.Animations.Count);
            foreach (var anim in _skeletonData.Animations)
            {
                list.Add(anim.Name);
            }
            _animationNames = list.ToArray();
        }

        private bool HasAnimation(string name)
        {
            if (string.IsNullOrEmpty(name) || _skeletonData == null)
            {
                return false;
            }

            return _skeletonData.FindAnimation(name) != null;
        }

        private bool HandleDebugBrowser()
        {
            if (_animationNames.Length == 0)
            {
                return false;
            }

            bool changed = false;
            if (Input.GetKeyDown(previousKey))
            {
                _debugIndex = (_debugIndex - 1 + _animationNames.Length) % _animationNames.Length;
                changed = true;
            }
            else if (Input.GetKeyDown(nextKey))
            {
                _debugIndex = (_debugIndex + 1) % _animationNames.Length;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            string name = _animationNames[_debugIndex];
            SetBaseAnimation(name, true);
            return true;
        }
    }
}
