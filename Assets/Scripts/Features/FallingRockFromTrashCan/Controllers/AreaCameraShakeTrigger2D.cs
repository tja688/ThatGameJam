using QFramework;
using ThatGameJam.Features.DeathRespawn.Models;
using ThatGameJam.Features.Shared;
using Unity.Cinemachine;
using UnityEngine;

namespace ThatGameJam.Features.FallingRockFromTrashCan.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class AreaCameraShakeTrigger2D : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private CinemachineCamera targetCamera;
        [SerializeField] private CinemachineBasicMultiChannelPerlin noiseComponent;
        [SerializeField] private NoiseSettings noiseProfile;
        [SerializeField] private float activeAmplitudeGain = 1f;
        [SerializeField] private float activeFrequencyGain = 1f;
        [SerializeField] private bool restoreOriginalOnExit = true;

        private readonly Collider2D[] _overlapResults = new Collider2D[8];
        private ContactFilter2D _contactFilter;
        private bool _isShaking;
        private bool _originalCached;
        private float _originalAmplitude;
        private float _originalFrequency;
        private NoiseSettings _originalProfile;
        private bool _missingNoiseLogged;

        private void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider != null && !triggerCollider.isTrigger)
            {
                LogKit.W("AreaCameraShakeTrigger2D expects Collider2D.isTrigger = true.");
            }

            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = Physics2D.AllLayers,
                useTriggers = true
            };
        }

        private void OnEnable()
        {
            _isShaking = false;
            _missingNoiseLogged = false;
            CacheNoiseDefaults();
            RefreshShakeState();

            this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            StopShake();
        }

        private void OnDisable()
        {
            StopShake();
        }

        private void Update()
        {
            RefreshShakeState();
        }



        private void RefreshShakeState()
        {
            if (triggerCollider == null)
            {
                return;
            }

            var playerInside = IsPlayerInside();
            if (playerInside && !_isShaking)
            {
                StartShake();
            }
            else if (!playerInside && _isShaking)
            {
                StopShake();
            }
        }

        private void StartShake()
        {
            if (_isShaking)
            {
                return;
            }

            CinemachineShakeAudit.Tag(this, "StartShake");

            var noise = GetNoiseComponent();
            if (noise == null)
            {
                if (!_missingNoiseLogged)
                {
                    LogKit.W("AreaCameraShakeTrigger2D missing CinemachineBasicMultiChannelPerlin.");
                    _missingNoiseLogged = true;
                }

                return;
            }

            CacheNoiseDefaults();

            if (noiseProfile != null)
            {
                noise.NoiseProfile = noiseProfile;
            }

            noise.AmplitudeGain = activeAmplitudeGain;
            noise.FrequencyGain = activeFrequencyGain;
            noise.ReSeed();
            _isShaking = true;
        }

        private void StopShake()
        {
            if (!_isShaking)
            {
                return;
            }

            CinemachineShakeAudit.Tag(this, "StopShake");

            var noise = GetNoiseComponent();
            if (noise == null)
            {
                _isShaking = false;
                return;
            }

            if (restoreOriginalOnExit && _originalCached)
            {
                noise.AmplitudeGain = _originalAmplitude;
                noise.FrequencyGain = _originalFrequency;
                noise.NoiseProfile = _originalProfile;
            }
            else
            {
                noise.AmplitudeGain = 0f;
            }

            _isShaking = false;
        }

        private void CacheNoiseDefaults()
        {
            var noise = GetNoiseComponent();
            if (noise == null)
            {
                return;
            }

            if (_originalCached)
            {
                return;
            }

            _originalAmplitude = noise.AmplitudeGain;
            _originalFrequency = noise.FrequencyGain;
            _originalProfile = noise.NoiseProfile;
            _originalCached = true;
        }

        private CinemachineBasicMultiChannelPerlin GetNoiseComponent()
        {
            if (noiseComponent != null)
            {
                return noiseComponent;
            }

            if (targetCamera == null)
            {
                return null;
            }

            noiseComponent = targetCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            return noiseComponent;
        }

        private bool IsPlayerInside()
        {
            if (triggerCollider == null)
            {
                return false;
            }

            var count = triggerCollider.Overlap(_contactFilter, _overlapResults);
            if (count <= 0)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var collider = _overlapResults[i];
                if (collider == null || !IsPlayer(collider))
                {
                    continue;
                }

                // Check if player is alive - if we can't get the model, assume not alive (safer default)
                var deathModel = this.GetModel<IDeathRespawnModel>();
                if (deathModel == null || !deathModel.IsAlive.Value)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other != null && other.CompareTag(playerTag);
        }
    }
}
