using System;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Commands;
using ThatGameJam.Features.PlayerCharacter2D.Configs;
using ThatGameJam.Features.PlayerCharacter2D.Events;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using ThatGameJam.Features.PlayerCharacter2D.Queries;
using ThatGameJam.Features.Shared;
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

        [Header("Climb")]
        [SerializeField] private Collider2D _climbSensor;
        [SerializeField] private string _climbableTag = "Climbable";
        [SerializeField] private LayerMask _climbableLayerMask = ~0;

        private IPlatformerFrameInputSource _resolvedInputSource;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private readonly List<Collider2D> _climbOverlapResults = new List<Collider2D>(8);
        private ContactFilter2D _climbOverlapFilter;
        private float _defaultGravityScale;
        private float _time;
        private bool _inputLocked;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        // Reads through Model or Query
        public Vector2 MoveInput => this.GetModel<IPlayerCharacter2DModel>().FrameInput.Move;
        public bool IsGrounded => this.GetModel<IPlayerCharacter2DModel>().Grounded.Value;
        public bool IsClimbing => this.GetModel<IPlayerCharacter2DModel>().IsClimbing.Value;

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

        // Action events for backward compatibility or Unity-side listeners
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public event Action<bool> ClimbStateChanged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _defaultGravityScale = _rb.gravityScale;
            _climbOverlapFilter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true,
                layerMask = _climbableLayerMask
            };

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

        private void OnEnable()
        {
            // Register event listeners tied to Enable/Disable lifecycle
            this.RegisterEvent<PlayerGroundedChangedEvent>(e =>
            {
                GroundedChanged?.Invoke(e.Grounded, e.ImpactSpeed);
            }).UnRegisterWhenDisabled(gameObject);

            this.RegisterEvent<PlayerJumpedEvent>(e =>
            {
                Jumped?.Invoke();
            }).UnRegisterWhenDisabled(gameObject);

            this.RegisterEvent<PlayerClimbStateChangedEvent>(e =>
            {
                ClimbStateChanged?.Invoke(e.IsClimbing);
            }).UnRegisterWhenDisabled(gameObject);

            this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<PlayerRespawnedEvent>(OnPlayerRespawned)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void Update()
        {
            if (_stats == null) return;

            _time += Time.deltaTime;

            var frameInput = _useExternalInput
                ? _externalInput
                : (_resolvedInputSource != null ? _resolvedInputSource.ReadInput() : default);

            if (_inputLocked)
            {
                frameInput = default;
            }

            if (_stats.SnapInput)
            {
                frameInput.Move.x = Mathf.Abs(frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(frameInput.Move.x);
                frameInput.Move.y = Mathf.Abs(frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(frameInput.Move.y);
            }

            // Write input to Model via Command
            this.SendCommand(new SetFrameInputCommand(frameInput, _time));
        }

        private void FixedUpdate()
        {
            if (_stats == null) return;

            // Physics queries (allowed in Controller)
            var previousQueriesStartInColliders = Physics2D.queriesStartInColliders;
            bool wallDetected = TryGetClimbableWall(out var wallSideSign, out var wallPoint, out var wallCollider, out var wallHorizontal);
            Physics2D.queriesStartInColliders = false;

            try
            {
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

                // Advance Model state
                this.SendCommand(new TickFixedStepCommand(
                    groundHit,
                    ceilingHit,
                    wallDetected,
                    wallSideSign,
                    wallDetected && wallHorizontal,
                    wallCollider != null ? wallCollider.name : string.Empty,
                    wallCollider != null ? wallCollider.tag : string.Empty,
                    wallCollider != null ? wallCollider.gameObject.layer : -1,
                    Time.fixedDeltaTime,
                    _stats));

                var isClimbing = this.GetModel<IPlayerCharacter2DModel>().IsClimbing.Value;
                _rb.gravityScale = isClimbing ? 0f : _defaultGravityScale;

                if (isClimbing && wallDetected)
                {
                    var climbSide = wallSideSign;
                    var modelSide = this.GetModel<IPlayerCharacter2DModel>().ClimbWallSide;
                    if (modelSide != 0f)
                    {
                        climbSide = modelSide;
                    }

                    var model = this.GetModel<IPlayerCharacter2DModel>();
                    if (!model.ClimbJumpProtected)
                    {
                        ApplyClimbStick(wallPoint, climbSide, wallHorizontal);
                    }
                }
            }
            finally
            {
                Physics2D.queriesStartInColliders = previousQueriesStartInColliders;
            }

            // Read result and apply
            _rb.linearVelocity = this.SendQuery(new GetDesiredVelocityQuery());
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            _inputLocked = true;
            ForceExitClimb();
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            _inputLocked = false;
            ForceExitClimb();
        }

        private void ForceExitClimb()
        {
            this.SendCommand(new ResetClimbStateCommand());
            _rb.gravityScale = _defaultGravityScale;
        }

        private bool TryGetClimbableWall(out float wallSideSign, out Vector2 wallPoint, out Collider2D wallCollider, out bool wallHorizontal)
        {
            wallSideSign = 0f;
            wallPoint = default;
            wallCollider = null;
            wallHorizontal = false;

            if (_climbSensor == null || !_climbSensor.enabled || string.IsNullOrEmpty(_climbableTag))
            {
                return false;
            }

            _climbOverlapResults.Clear();
            _climbOverlapFilter.useLayerMask = true;
            _climbOverlapFilter.layerMask = _climbableLayerMask;
            _climbSensor.Overlap(_climbOverlapFilter, _climbOverlapResults);
            if (_climbOverlapResults.Count == 0)
            {
                return false;
            }

            var center = (Vector2)_climbSensor.bounds.center;
            var bestDistance = float.MaxValue;
            Collider2D bestCollider = null;
            ColliderDistance2D bestSeparation = default;
            var bestHasSeparation = false;

            for (var i = 0; i < _climbOverlapResults.Count; i++)
            {
                var hit = _climbOverlapResults[i];
                if (hit == null || !hit.enabled)
                {
                    continue;
                }

                if (hit.attachedRigidbody == _rb)
                {
                    continue;
                }

                if (!hit.CompareTag(_climbableTag))
                {
                    continue;
                }

                var separation = Physics2D.Distance(_climbSensor, hit);
                var distance = Mathf.Abs(separation.distance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCollider = hit;
                    bestSeparation = separation;
                    bestHasSeparation = true;
                }
            }

            if (bestCollider == null)
            {
                return false;
            }

            wallHorizontal = IsHorizontalClimbSurface(bestCollider);

            if (bestHasSeparation)
            {
                wallPoint = bestSeparation.pointB;
                wallSideSign = Mathf.Sign(wallHorizontal ? bestSeparation.normal.y : bestSeparation.normal.x);
                if (wallSideSign == 0f)
                {
                    wallSideSign = Mathf.Sign(wallHorizontal
                        ? bestSeparation.pointB.y - bestSeparation.pointA.y
                        : bestSeparation.pointB.x - bestSeparation.pointA.x);
                }
            }
            else
            {
                wallPoint = bestCollider.ClosestPoint(center);
                wallSideSign = Mathf.Sign(wallHorizontal ? wallPoint.y - center.y : wallPoint.x - center.x);
            }

            if (wallSideSign == 0f)
            {
                wallSideSign = Mathf.Sign(wallHorizontal
                    ? bestCollider.bounds.center.y - center.y
                    : bestCollider.bounds.center.x - center.x);
            }

            if (wallSideSign == 0f)
            {
                wallSideSign = 1f;
            }

            wallCollider = bestCollider;
            return true;
        }

        private void ApplyClimbStick(Vector2 wallPoint, float wallSideSign, bool wallHorizontal)
        {
            if (_climbSensor == null || _stats.ClimbStickDistance <= 0f || wallSideSign == 0f)
            {
                return;
            }

            if (!_stats.ClimbLockSecondaryAxis)
            {
                var move = this.GetModel<IPlayerCharacter2DModel>().FrameInput.Move;
                var secondaryInput = wallHorizontal ? move.y : move.x;
                if (Mathf.Abs(secondaryInput) > 0.01f)
                {
                    return;
                }
            }

            var center = (Vector2)_climbSensor.bounds.center;
            var offsetToTransform = (Vector2)transform.position - center;

            var pos = _rb.position;
            if (wallHorizontal)
            {
                var targetCenterY = wallPoint.y - wallSideSign * _stats.ClimbStickDistance;
                var targetPositionY = targetCenterY + offsetToTransform.y;
                pos.y = targetPositionY;
            }
            else
            {
                var targetCenterX = wallPoint.x - wallSideSign * _stats.ClimbStickDistance;
                var targetPositionX = targetCenterX + offsetToTransform.x;
                pos.x = targetPositionX;
            }
            _rb.position = pos;
        }

        private static bool IsHorizontalClimbSurface(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            var size = collider.bounds.size;
            return size.x >= size.y;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
            {
                // Debug.LogWarning("Please assign a PlatformerCharacterStats asset to the controller.", this);
            }
        }
#endif
    }
}
