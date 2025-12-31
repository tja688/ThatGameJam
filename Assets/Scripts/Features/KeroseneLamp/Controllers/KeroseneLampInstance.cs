using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.KeroseneLamp.Commands;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Independents.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace ThatGameJam.Features.KeroseneLamp.Controllers
{
    public class KeroseneLampInstance : MonoBehaviour, IBackpackItemInstance, IController
    {
        [Header("Item")]
        [SerializeField] private ItemDefinition itemDefinition;

        [Header("Visuals")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Behaviour[] visualBehaviours;
        [SerializeField] private Renderer[] visualRenderers;

        [Header("Gameplay Visuals")]
        [SerializeField] private GameObject gameplayEnabledRoot;
        [SerializeField] private GameObject gameplayDisabledRoot;

        [Header("Held Offsets")]
        [SerializeField] private Vector3 heldLocalOffset;
        [SerializeField] private Vector3 heldLocalEulerAngles;

        [Header("State Events")]
        [SerializeField] private UnityEvent onEnterInBackpack;
        [SerializeField] private UnityEvent onEnterHeld;
        [SerializeField] private UnityEvent onEnterDropped;
        [SerializeField] private UnityEvent onEnterDisabled;

        private int _lampId = -1;
        private bool _visualEnabled = true;
        private bool _gameplayEnabled = true;
        private bool _backpackHidden;

        private bool _cachedPhysics;
        private bool _cachedVisuals;
        private Rigidbody2D _rigidbody2D;
        private bool _defaultSimulated;
        private Collider2D[] _colliders;
        private bool[] _colliderDefaults;
        private bool[] _behaviourDefaults;
        private bool[] _rendererDefaults;
        private bool _visualRootDefaultActive;
        private Transform _defaultParent;
        private KeroseneLampManager _manager;

        private KeroseneLampState _state = KeroseneLampState.Dropped;
        private bool _stateInitialized;

        public ItemDefinition Definition => itemDefinition;
        public int InstanceId => _lampId;
        public KeroseneLampState State => _state;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            _defaultParent = transform.parent;
            CachePhysicsState();
            CacheVisualState();
        }

        private void OnEnable()
        {
            ApplyVisualState();
            SyncGameplayState();
        }

        public void SetLampId(int lampId)
        {
            _lampId = lampId;
            UpdateGameplayState();
        }

        public void SetHeldOffsets(Vector3 localOffset, Vector3 localEulerAngles)
        {
            heldLocalOffset = localOffset;
            heldLocalEulerAngles = localEulerAngles;
        }

        public void SetManager(KeroseneLampManager manager)
        {
            _manager = manager;
        }

        public void SetVisualEnabled(bool enabled)
        {
            if (_visualEnabled == enabled)
            {
                return;
            }

            _visualEnabled = enabled;
            ApplyVisualState();
        }

        public void SetGameplayEnabled(bool enabled)
        {
            if (_gameplayEnabled == enabled)
            {
                return;
            }

            _gameplayEnabled = enabled;
            SyncGameplayState();
        }

        public void SetState(KeroseneLampState state, Transform holdPoint = null, Vector3? worldPosition = null)
        {
            if (_state == state && _stateInitialized)
            {
                return;
            }

            _state = state;
            _stateInitialized = true;
            CachePhysicsState();
            CacheVisualState();

            switch (state)
            {
                case KeroseneLampState.InBackpack:
                    _backpackHidden = true;
                    ApplyVisualState();
                    SetPhysicsActive(false, false);
                    SetCollidersActive(false);
                    onEnterInBackpack?.Invoke();
                    break;
                case KeroseneLampState.Held:
                    _backpackHidden = false;
                    ApplyVisualState();
                    SetPhysicsActive(false, false);
                    SetCollidersActive(false);
                    AttachToHoldPoint(holdPoint);
                    onEnterHeld?.Invoke();
                    break;
                case KeroseneLampState.Dropped:
                    _backpackHidden = false;
                    ApplyVisualState();
                    DetachFromHoldPoint(worldPosition);
                    SetPhysicsActive(true, true);
                    SetCollidersActive(true);
                    onEnterDropped?.Invoke();
                    break;
                case KeroseneLampState.Disabled:
                    _backpackHidden = false;
                    ApplyVisualState();
                    SetPhysicsActive(false, false);
                    SetCollidersActive(false);
                    onEnterDisabled?.Invoke();
                    break;
            }

            if (_lampId >= 0 && (state == KeroseneLampState.InBackpack || state == KeroseneLampState.Held))
            {
                this.SendCommand(new SetLampInventoryFlagsCommand(_lampId, true));
            }

            UpdateGameplayState();
        }

        public void OnAddedToBackpack()
        {
            SetState(KeroseneLampState.InBackpack);
        }

        public void OnSetHeld(Transform holdPoint)
        {
            SetState(KeroseneLampState.Held, holdPoint);
        }

        public void OnDropped(Vector3 worldPosition)
        {
            SetState(KeroseneLampState.Dropped, null, worldPosition);
            _manager?.NotifyLampDropped(this, worldPosition);
        }

        public void OnRemovedFromBackpack()
        {
        }

        private void AttachToHoldPoint(Transform holdPoint)
        {
            if (holdPoint == null)
            {
                LogKit.W("KeroseneLampInstance missing hold point for Held state.");
                return;
            }

            var lampTransform = transform;
            lampTransform.SetParent(holdPoint, false);
            lampTransform.localPosition = heldLocalOffset;
            lampTransform.localEulerAngles = heldLocalEulerAngles;
        }

        private void DetachFromHoldPoint(Vector3? worldPosition)
        {
            if (_defaultParent != null)
            {
                transform.SetParent(_defaultParent, true);
            }
            else
            {
                transform.SetParent(null, true);
            }

            if (worldPosition.HasValue)
            {
                transform.position = worldPosition.Value;
            }
        }

        private void SyncGameplayState()
        {
            if (gameplayEnabledRoot != null)
            {
                gameplayEnabledRoot.SetActive(_gameplayEnabled);
            }

            if (gameplayDisabledRoot != null)
            {
                gameplayDisabledRoot.SetActive(!_gameplayEnabled);
            }

            if (!_gameplayEnabled && !_backpackHidden)
            {
                AudioService.Play("SFX-INT-0009", new AudioContext
                {
                    Position = transform.position,
                    HasPosition = true
                });
            }
        }

        private void CachePhysicsState()
        {
            if (_cachedPhysics)
            {
                return;
            }

            _cachedPhysics = true;
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _defaultSimulated = _rigidbody2D != null && _rigidbody2D.simulated;
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _colliderDefaults = new bool[_colliders.Length];
            for (var i = 0; i < _colliders.Length; i++)
            {
                _colliderDefaults[i] = _colliders[i] != null && _colliders[i].enabled;
            }
        }

        private void CacheVisualState()
        {
            if (_cachedVisuals)
            {
                return;
            }

            _cachedVisuals = true;
            _visualRootDefaultActive = visualRoot != null && visualRoot.activeSelf;

            if (visualBehaviours != null)
            {
                _behaviourDefaults = new bool[visualBehaviours.Length];
                for (var i = 0; i < visualBehaviours.Length; i++)
                {
                    _behaviourDefaults[i] = visualBehaviours[i] != null && visualBehaviours[i].enabled;
                }
            }

            if (visualRenderers != null)
            {
                _rendererDefaults = new bool[visualRenderers.Length];
                for (var i = 0; i < visualRenderers.Length; i++)
                {
                    _rendererDefaults[i] = visualRenderers[i] != null && visualRenderers[i].enabled;
                }
            }
        }

        private void ApplyVisualState()
        {
            var shouldShow = _visualEnabled && !_backpackHidden;

            if (visualRoot != null)
            {
                visualRoot.SetActive(shouldShow && _visualRootDefaultActive);
            }

            if (visualBehaviours != null)
            {
                for (var i = 0; i < visualBehaviours.Length; i++)
                {
                    if (visualBehaviours[i] != null)
                    {
                        var defaultEnabled = _behaviourDefaults != null && i < _behaviourDefaults.Length
                            ? _behaviourDefaults[i]
                            : true;
                        visualBehaviours[i].enabled = shouldShow && defaultEnabled;
                    }
                }
            }

            if (visualRenderers != null)
            {
                for (var i = 0; i < visualRenderers.Length; i++)
                {
                    if (visualRenderers[i] != null)
                    {
                        var defaultEnabled = _rendererDefaults != null && i < _rendererDefaults.Length
                            ? _rendererDefaults[i]
                            : true;
                        visualRenderers[i].enabled = shouldShow && defaultEnabled;
                    }
                }
            }
        }

        private void SetPhysicsActive(bool enableSimulation, bool resetVelocity)
        {
            if (_rigidbody2D == null)
            {
                return;
            }

            _rigidbody2D.simulated = enableSimulation && _defaultSimulated;

            if (resetVelocity)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }
        }

        private void SetCollidersActive(bool enabled)
        {
            if (_colliders == null)
            {
                return;
            }

            for (var i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] == null)
                {
                    continue;
                }

                var defaultEnabled = _colliderDefaults != null && i < _colliderDefaults.Length
                    ? _colliderDefaults[i]
                    : true;
                _colliders[i].enabled = enabled && defaultEnabled;
            }
        }

        private void UpdateGameplayState()
        {
            if (_lampId < 0)
            {
                return;
            }

            var shouldEnable = _state == KeroseneLampState.Held || _state == KeroseneLampState.Dropped;
            this.SendCommand(new SetLampGameplayStateCommand(_lampId, shouldEnable));
        }
    }
}
