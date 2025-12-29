using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Controllers
{
    public class KeroseneLampInstance : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Behaviour[] visualBehaviours;
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private GameObject gameplayEnabledRoot;
        [SerializeField] private GameObject gameplayDisabledRoot;
        [SerializeField] private AudioSource gameplayDisabledSfx;

        private bool _visualEnabled = true;
        private bool _gameplayEnabled = true;
        private bool _held;
        private bool _cachedPhysics;
        private bool _defaultSimulated;
        private Rigidbody2D _rigidbody2D;
        private Collider2D[] _colliders;
        private bool[] _colliderDefaults;

        private void Awake()
        {
            CachePhysicsState();
        }

        private void OnEnable()
        {
            SyncGameplayState();
        }

        public void SetVisualEnabled(bool enabled)
        {
            if (_visualEnabled == enabled)
            {
                return;
            }

            _visualEnabled = enabled;

            if (visualRoot != null)
            {
                visualRoot.SetActive(enabled);
            }

            if (visualBehaviours != null)
            {
                for (var i = 0; i < visualBehaviours.Length; i++)
                {
                    if (visualBehaviours[i] != null)
                    {
                        visualBehaviours[i].enabled = enabled;
                    }
                }
            }

            if (visualRenderers != null)
            {
                for (var i = 0; i < visualRenderers.Length; i++)
                {
                    if (visualRenderers[i] != null)
                    {
                        visualRenderers[i].enabled = enabled;
                    }
                }
            }
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

        public void SetHeld(bool held)
        {
            if (_held == held)
            {
                return;
            }

            _held = held;
            CachePhysicsState();

            if (_rigidbody2D != null)
            {
                if (held)
                {
                    _rigidbody2D.simulated = false;
                    _rigidbody2D.linearVelocity = Vector2.zero;
                    _rigidbody2D.angularVelocity = 0f;
                }
                else
                {
                    _rigidbody2D.simulated = _defaultSimulated;
                }
            }

            if (_colliders != null)
            {
                for (var i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null)
                    {
                        _colliders[i].enabled = held ? false : _colliderDefaults[i];
                    }
                }
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

            if (!_gameplayEnabled && gameplayDisabledSfx != null)
            {
                gameplayDisabledSfx.Play();
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
    }
}
