using System;
using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Configs;
using ThatGameJam.Features.PlayerCharacter2D.Events;
using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Controllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlatformerCharacterController : MonoBehaviour, IController
    {
        [SerializeField] private PlatformerCharacterStats _stats;

        [Header("Input")]
        [SerializeField] private bool _useExternalInput;
        [SerializeField] private PlatformerFrameInput _externalInput;
        [SerializeField] private MonoBehaviour _inputSource;

        private IPlatformerFrameInputSource _resolvedInputSource;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private PlatformerFrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueriesStartInColliders;

        private float _time;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        public Vector2 MoveInput => _frameInput.Move;
        public bool IsGrounded => _grounded;

        public bool UseExternalInput
        {
            get => _useExternalInput;
            set => _useExternalInput = value;
        }

        public PlatformerFrameInput ExternalInput
        {
            get => _externalInput;
            set => _externalInput = value;
        }

        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _cachedQueriesStartInColliders = Physics2D.queriesStartInColliders;

            _resolvedInputSource = _inputSource as IPlatformerFrameInputSource;
            if (_resolvedInputSource == null)
            {
                foreach (var monoBehaviour in GetComponents<MonoBehaviour>())
                {
                    if (monoBehaviour is IPlatformerFrameInputSource source)
                    {
                        _resolvedInputSource = source;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (_stats == null) return;

            _time += Time.deltaTime;

            _frameInput = _useExternalInput
                ? _externalInput
                : (_resolvedInputSource != null ? _resolvedInputSource.ReadInput() : default);

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            if (_stats == null) return;

            CheckCollisions();

            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.down,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer);

            bool ceilingHit = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.up,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer);

            if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;

                var impact = Mathf.Abs(_frameVelocity.y);
                GroundedChanged?.Invoke(true, impact);
                GetArchitecture().SendEvent(new PlayerGroundedChangedEvent { Grounded = true, ImpactSpeed = impact });
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;

                GroundedChanged?.Invoke(false, 0);
                GetArchitecture().SendEvent(new PlayerGroundedChangedEvent { Grounded = false, ImpactSpeed = 0 });
            }

            Physics2D.queriesStartInColliders = _cachedQueriesStartInColliders;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.y = _stats.JumpPower;

            Jumped?.Invoke();
            GetArchitecture().SendEvent<PlayerJumpedEvent>();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    _frameInput.Move.x * _stats.MaxSpeed,
                    _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
                return;
            }

            var inAirGravity = _stats.FallAcceleration;
            if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;

            _frameVelocity.y = Mathf.MoveTowards(
                _frameVelocity.y,
                -_stats.MaxFallSpeed,
                inAirGravity * Time.fixedDeltaTime);
        }

        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
                Debug.LogWarning("Please assign a PlatformerCharacterStats asset to the controller.", this);
        }
#endif
    }
}
