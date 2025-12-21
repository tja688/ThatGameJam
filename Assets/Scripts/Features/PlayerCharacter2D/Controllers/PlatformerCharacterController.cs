using System;
using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Commands;
using ThatGameJam.Features.PlayerCharacter2D.Configs;
using ThatGameJam.Features.PlayerCharacter2D.Events;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using ThatGameJam.Features.PlayerCharacter2D.Queries;
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
        private bool _cachedQueriesStartInColliders;
        private float _time;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        // Reads through Model or Query
        public Vector2 MoveInput => this.GetModel<IPlayerCharacter2DModel>().FrameInput.Move;
        public bool IsGrounded => this.GetModel<IPlayerCharacter2DModel>().Grounded.Value;

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

            // Register event listeners for the Actions
            this.RegisterEvent<PlayerGroundedChangedEvent>(e =>
            {
                GroundedChanged?.Invoke(e.Grounded, e.ImpactSpeed);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<PlayerJumpedEvent>(e =>
            {
                Jumped?.Invoke();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (_stats == null) return;

            _time += Time.deltaTime;

            var frameInput = _useExternalInput
                ? _externalInput
                : (_resolvedInputSource != null ? _resolvedInputSource.ReadInput() : default);

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

            Physics2D.queriesStartInColliders = _cachedQueriesStartInColliders;

            // Advance Model state
            this.SendCommand(new TickFixedStepCommand(groundHit, ceilingHit, Time.fixedDeltaTime, _stats));

            // Read result and apply
            _rb.linearVelocity = this.SendQuery(new GetDesiredVelocityQuery());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
                Debug.LogWarning("Please assign a PlatformerCharacterStats asset to the controller.", this);
        }
#endif
    }
}
