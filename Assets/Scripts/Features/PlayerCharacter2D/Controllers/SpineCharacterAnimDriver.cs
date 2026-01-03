using System;
using System.Collections;
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
        private static class Animations
        {
            public const string Idle = "front/animations/basic_buildin/standby";
            public const string WalkLeft = "front/animations/basic_buildin/walk";
            public const string WalkRight = "front/animations/basic_buildin/walk_best";
            public const string WalkTurn = "front/animations/basic_buildin/walk_2";
            public const string HandSwing = "front/animations/basic_buildin/handsswing";
            public const string Puzzled = "front/animations/basic_buildin/puzzled";
            public const string Blink = "front/animations/eyes/blink";
            public const string ClimbVertical = "back/climb_verticle";
            public const string ClimbHorizontal = "back/climb_horizon";
            public const string StaticBackHang = "static/static_back_hang";
            public const string StaticBackStand = "static/static_back_stand";
            public const string StaticFront = "static/static_front";
            public const string StaticFrontEyesShut = "static/static_front_eyes_shut";
            public const string FaceEyesShut = "front/skin/state_mask/with_eyes_shut";
            public const string FaceMouthUpset = "front/skin/state_mask/with_mouth_upset";
            public const string FaceShockedEyes = "front/skin/state_mask/with_shocked_eyes";
        }

        private enum ExpressionMode
        {
            Persistent,
            OneShot
        }

        [Serializable]
        private struct ExpressionBinding
        {
            public KeyCode Key;
            public KeyCode AltKey;
            public string Animation;
            public ExpressionMode Mode;

            public ExpressionBinding(KeyCode key, KeyCode altKey, string animation, ExpressionMode mode)
            {
                Key = key;
                AltKey = altKey;
                Animation = animation;
                Mode = mode;
            }
        }

        private static readonly ExpressionBinding[] ExpressionBindings =
        {
            new ExpressionBinding(KeyCode.Alpha4, KeyCode.Keypad4, Animations.Blink, ExpressionMode.OneShot),
            new ExpressionBinding(KeyCode.Alpha5, KeyCode.Keypad5, Animations.FaceEyesShut, ExpressionMode.Persistent),
            new ExpressionBinding(KeyCode.Alpha6, KeyCode.Keypad6, Animations.FaceMouthUpset, ExpressionMode.Persistent),
            new ExpressionBinding(KeyCode.Alpha7, KeyCode.Keypad7, Animations.FaceShockedEyes, ExpressionMode.Persistent),
            new ExpressionBinding(KeyCode.Alpha8, KeyCode.Keypad8, Animations.Puzzled, ExpressionMode.OneShot),
            new ExpressionBinding(KeyCode.Alpha9, KeyCode.Keypad9, null, ExpressionMode.Persistent)
        };

        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private int baseTrack = 0;
        [SerializeField] private int overlayTrack = 1;
        [SerializeField] private int expressionTrack = 2;

        [Header("References")]
        [SerializeField] private PlatformerCharacterController controller;
        [SerializeField] private Rigidbody2D rb;

        [Header("Thresholds")]
        [SerializeField] private float speedThreshold = 0.1f;
        [SerializeField] private float turnMinSpeed = 0.1f;
        [SerializeField] private float climbMoveThreshold = 0.05f;
        [SerializeField] private float climbIdleTimeScale = 0.2f;

        [Header("Mixing")]
        [SerializeField] private float baseMixDuration = 0.08f;
        [SerializeField] private float overlayFadeIn = 0.1f;
        [SerializeField] private float overlayFadeOut = 0.1f;
        [SerializeField, Range(0f, 1f)] private float overlayAlpha = 1f;
        [SerializeField] private float expressionFadeIn = 0.05f;
        [SerializeField] private float expressionFadeOut = 0.05f;
        [SerializeField, Range(0f, 1f)] private float expressionAlpha = 1f;

        [Header("Climb Landing")]
        [SerializeField] private float landingFrameDuration = 0.06f;

        [Header("Facing")]
        [SerializeField] private bool enableFacing = true;
        [SerializeField] private bool preferDirectionalWalk = true;
        [SerializeField] private bool faceByMoveInput = true;
        [SerializeField] private float facingDeadZone = 0.05f;

        [Header("Debug Browser")]
        [SerializeField] private bool enableDebugBrowser;
        [SerializeField] private KeyCode previousKey = KeyCode.LeftBracket;
        [SerializeField] private KeyCode nextKey = KeyCode.RightBracket;

        private IPlayerCharacter2DModel _model;
        private TrackEntry _turnEntry;
        private Coroutine _landingCoroutine;
        private bool _wasClimbing;
        private bool _hasDirectionalWalk;
        private float _defaultScaleX = 1f;
        private SkeletonData _skeletonData;
        private string[] _animationNames = Array.Empty<string>();
        private int _debugIndex;
        private int _lastMoveSign;
        private string _currentBase;
        private string _currentOverlay;
        private string _currentExpression;

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
            _hasDirectionalWalk = HasAnimation(Animations.WalkLeft) && HasAnimation(Animations.WalkRight);
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

            ReadState(out bool isGrounded, out bool isClimbing, out bool climbHorizontal, out Vector2 velocity, out Vector2 moveInput);
            UpdateFacing(moveInput, velocity, isClimbing);
            UpdateBaseAnimation(isGrounded, isClimbing, climbHorizontal, velocity, moveInput);
            HandleExpressionInput(isClimbing);
        }

        private void ReadState(out bool isGrounded, out bool isClimbing, out bool climbHorizontal, out Vector2 velocity, out Vector2 moveInput)
        {
            isGrounded = false;
            isClimbing = false;
            climbHorizontal = false;
            velocity = Vector2.zero;
            moveInput = Vector2.zero;

            if (_model != null)
            {
                isGrounded = _model.Grounded.Value;
                isClimbing = _model.IsClimbing.Value;
                climbHorizontal = _model.ClimbIsHorizontal;
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
                if (isClimbing && velocity != Vector2.zero)
                {
                    climbHorizontal = Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y);
                }
            }
        }

        private void UpdateFacing(Vector2 moveInput, Vector2 velocity, bool isClimbing)
        {
            if (!enableFacing || skeletonAnimation == null || skeletonAnimation.Skeleton == null)
            {
                return;
            }

            if (_hasDirectionalWalk && preferDirectionalWalk && !isClimbing)
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

        private void UpdateBaseAnimation(bool isGrounded, bool isClimbing, bool climbHorizontal, Vector2 velocity, Vector2 moveInput)
        {
            if (_landingCoroutine != null)
            {
                if (isClimbing || !isGrounded)
                {
                    StopClimbLandingSequence();
                }
                else
                {
                    SetWalkOverlay(false);
                    return;
                }
            }

            if (_turnEntry != null && !_turnEntry.IsComplete)
            {
                if (isClimbing || !isGrounded)
                {
                    _turnEntry = null;
                }
                else
                {
                    SetWalkOverlay(false);
                    return;
                }
            }

            if (_turnEntry != null && _turnEntry.IsComplete)
            {
                _turnEntry = null;
            }

            bool exitedClimb = _wasClimbing && !isClimbing && isGrounded;
            _wasClimbing = isClimbing;

            if (isClimbing)
            {
                UpdateClimbAnimation(climbHorizontal, velocity);
                return;
            }

            if (exitedClimb)
            {
                StartClimbLandingSequence();
                return;
            }

            if (!isGrounded)
            {
                SetWalkOverlay(false);
                return;
            }

            bool moving = Mathf.Abs(velocity.x) > speedThreshold || Mathf.Abs(moveInput.x) > speedThreshold;
            if (!moving)
            {
                SetBaseAnimation(Animations.Idle, true);
                SetWalkOverlay(false);
                _lastMoveSign = 0;
                return;
            }

            int moveSign = GetMoveSign(moveInput.x, velocity.x);
            string walkAnim = GetWalkAnimation(moveSign);
            if (_lastMoveSign != 0 && moveSign != 0 && moveSign != _lastMoveSign && Mathf.Abs(velocity.x) > turnMinSpeed)
            {
                if (HasAnimation(Animations.WalkTurn))
                {
                    var entry = skeletonAnimation.AnimationState.SetAnimation(baseTrack, Animations.WalkTurn, false);
                    entry.MixDuration = baseMixDuration;
                    skeletonAnimation.AnimationState.AddAnimation(baseTrack, walkAnim, true, 0f);
                    _turnEntry = entry;
                    _currentBase = Animations.WalkTurn;
                    SetWalkOverlay(false);
                    _lastMoveSign = moveSign;
                    return;
                }
            }

            _lastMoveSign = moveSign;
            SetBaseAnimation(walkAnim, true);
            SetWalkOverlay(true);
        }

        private void UpdateClimbAnimation(bool climbHorizontal, Vector2 velocity)
        {
            SetWalkOverlay(false);
            ClearExpression();

            float axisSpeed = climbHorizontal ? Mathf.Abs(velocity.x) : Mathf.Abs(velocity.y);
            bool moving = axisSpeed > climbMoveThreshold;
            string anim = moving ? (climbHorizontal ? Animations.ClimbHorizontal : Animations.ClimbVertical) : GetClimbIdleAnimation(climbHorizontal);
            TrackEntry entry = SetBaseAnimation(anim, true);

            if (!moving && entry != null && (anim == Animations.ClimbHorizontal || anim == Animations.ClimbVertical))
            {
                entry.TimeScale = climbIdleTimeScale;
            }
        }

        private string GetClimbIdleAnimation(bool climbHorizontal)
        {
            if (HasAnimation(Animations.StaticBackHang))
            {
                return Animations.StaticBackHang;
            }

            if (HasAnimation(Animations.StaticBackStand))
            {
                return Animations.StaticBackStand;
            }

            return climbHorizontal ? Animations.ClimbHorizontal : Animations.ClimbVertical;
        }

        private int GetMoveSign(float inputX, float velocityX)
        {
            if (Mathf.Abs(inputX) > facingDeadZone)
            {
                return inputX > 0 ? 1 : -1;
            }

            if (Mathf.Abs(velocityX) > facingDeadZone)
            {
                return velocityX > 0 ? 1 : -1;
            }

            return _lastMoveSign;
        }

        private string GetWalkAnimation(int moveSign)
        {
            if (preferDirectionalWalk && _hasDirectionalWalk)
            {
                return moveSign < 0 ? Animations.WalkLeft : Animations.WalkRight;
            }

            if (HasAnimation(Animations.WalkRight))
            {
                return Animations.WalkRight;
            }

            return Animations.WalkLeft;
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

        private void SetWalkOverlay(bool enable)
        {
            if (!enable || !HasAnimation(Animations.HandSwing))
            {
                ClearOverlay();
                return;
            }

            if (_currentOverlay == Animations.HandSwing)
            {
                return;
            }

            var entry = skeletonAnimation.AnimationState.SetAnimation(overlayTrack, Animations.HandSwing, true);
            entry.MixDuration = overlayFadeIn;
            entry.Alpha = overlayAlpha;
            _currentOverlay = Animations.HandSwing;
        }

        private void ClearOverlay()
        {
            if (_currentOverlay == null)
            {
                return;
            }

            skeletonAnimation.AnimationState.SetEmptyAnimation(overlayTrack, overlayFadeOut);
            _currentOverlay = null;
        }

        private void HandleExpressionInput(bool isClimbing)
        {
            if (isClimbing)
            {
                return;
            }

            foreach (var binding in ExpressionBindings)
            {
                if (!Input.GetKeyDown(binding.Key) && !Input.GetKeyDown(binding.AltKey))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(binding.Animation))
                {
                    ClearExpression();
                    return;
                }

                if (!HasAnimation(binding.Animation))
                {
                    return;
                }

                if (binding.Mode == ExpressionMode.OneShot)
                {
                    PlayExpressionOnce(binding.Animation);
                    return;
                }

                if (_currentExpression == binding.Animation)
                {
                    ClearExpression();
                    return;
                }

                SetExpression(binding.Animation);
                return;
            }
        }

        private void SetExpression(string name)
        {
            var entry = skeletonAnimation.AnimationState.SetAnimation(expressionTrack, name, true);
            entry.MixDuration = expressionFadeIn;
            entry.Alpha = expressionAlpha;
            _currentExpression = name;
        }

        private void PlayExpressionOnce(string name)
        {
            var entry = skeletonAnimation.AnimationState.SetAnimation(expressionTrack, name, false);
            entry.MixDuration = expressionFadeIn;
            entry.Alpha = expressionAlpha;

            if (!string.IsNullOrEmpty(_currentExpression))
            {
                skeletonAnimation.AnimationState.AddAnimation(expressionTrack, _currentExpression, true, 0f);
            }
            else
            {
                skeletonAnimation.AnimationState.AddEmptyAnimation(expressionTrack, expressionFadeOut, 0f);
            }
        }

        private void ClearExpression()
        {
            skeletonAnimation.AnimationState.SetEmptyAnimation(expressionTrack, expressionFadeOut);
            _currentExpression = null;
        }

        private void StartClimbLandingSequence()
        {
            if (_landingCoroutine != null)
            {
                StopCoroutine(_landingCoroutine);
            }

            _landingCoroutine = StartCoroutine(PlayClimbLandingSequence());
        }

        private void StopClimbLandingSequence()
        {
            if (_landingCoroutine == null)
            {
                return;
            }

            StopCoroutine(_landingCoroutine);
            _landingCoroutine = null;
        }

        private IEnumerator PlayClimbLandingSequence()
        {
            SetWalkOverlay(false);
            ClearExpression();

            var sequence = new List<string>
            {
                Animations.StaticBackHang,
                Animations.StaticBackStand,
                Animations.StaticFront,
                Animations.StaticFrontEyesShut
            };

            foreach (var name in sequence)
            {
                if (HasAnimation(name))
                {
                    var entry = skeletonAnimation.AnimationState.SetAnimation(baseTrack, name, false);
                    entry.MixDuration = 0f;
                }

                yield return new WaitForSeconds(landingFrameDuration);
            }

            _landingCoroutine = null;
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
