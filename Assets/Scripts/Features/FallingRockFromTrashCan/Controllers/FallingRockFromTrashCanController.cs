using System.Collections;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.FallingRockFromTrashCan.Events;
using ThatGameJam.Features.Shared;
using ThatGameJam.Independents.Audio;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace ThatGameJam.Features.FallingRockFromTrashCan.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class FallingRockFromTrashCanController : MonoBehaviour, IController, ICanSendEvent
    {
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private float stopDelaySeconds = 5f;
        [SerializeField] private string playerTag = "Player";

        private readonly Collider2D[] _overlapResults = new Collider2D[8];
        private ContactFilter2D _contactFilter;
        private Collider2D _triggerCollider;
        private readonly List<GameObject> _activeProjectiles = new List<GameObject>();
        private Coroutine _spawnRoutine;
        private Coroutine _stopRoutine;
        private int _spawnIndex;
        private bool _eventActive;
        private bool _missingPrefabLogged;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            if (_triggerCollider != null && !_triggerCollider.isTrigger)
            {
                LogKit.W("FallingRockFromTrashCanController expects Collider2D.isTrigger = true.");
            }

            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = Physics2D.AllLayers,
                useTriggers = true
            };
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            StartFallingEvent();
        }

        private void OnEnable()
        {
            _eventActive = false;
            _spawnIndex = 0;
            _missingPrefabLogged = false;

            this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<PlayerRespawnedEvent>(OnPlayerRespawned)
                .UnRegisterWhenDisabled(gameObject);

            if (IsPlayerInside())
            {
                StartFallingEvent();
            }
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            // Stop own event immediately
            StopFallingEventImmediate();

            // Force global stop for this sound ID just in case
            AudioService.Stop("SFX-INT-0014");
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            if (IsPlayerInside())
            {
                StartFallingEvent();
            }
        }

        private void OnDisable()
        {
            StopFallingEventImmediate();
        }

        private void Update()
        {
            if (!_eventActive)
            {
                return;
            }

            if (!IsPlayerInside())
            {
                EndFallingEvent();
            }
        }

        private void StartFallingEvent()
        {
            if (_eventActive)
            {
                return;
            }

            _eventActive = true;
            _spawnIndex = 0;
            this.SendEvent(new FallingRockFromTrashCanStartedEvent
            {
                AreaTransform = transform
            });
            AudioService.Play("SFX-INT-0014", new AudioContext
            {
                Owner = transform
            });

            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }

            if (_spawnRoutine == null)
            {
                _spawnRoutine = StartCoroutine(SpawnRoutine());
            }
        }

        private void EndFallingEvent()
        {
            if (!_eventActive)
            {
                return;
            }

            _eventActive = false;
            this.SendEvent(new FallingRockFromTrashCanEndedEvent
            {
                AreaTransform = transform
            });
            AudioService.Stop("SFX-INT-0014", new AudioContext
            {
                Owner = transform
            });

            if (_stopRoutine == null)
            {
                _stopRoutine = StartCoroutine(StopAfterDelay());
            }
        }

        private IEnumerator StopAfterDelay()
        {
            if (stopDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(stopDelaySeconds);
            }

            _stopRoutine = null;

            if (_eventActive)
            {
                yield break;
            }

            StopSpawningImmediate();
        }

        private void StopFallingEventImmediate()
        {
            _eventActive = false;
            AudioService.Stop("SFX-INT-0014", new AudioContext
            {
                Owner = transform
            });
            StopSpawningImmediate();
            CleanupActiveProjectiles();
        }

        private void CleanupActiveProjectiles()
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                if (_activeProjectiles[i] != null)
                {
                    Destroy(_activeProjectiles[i]);
                }
            }
            _activeProjectiles.Clear();
        }

        private void StopSpawningImmediate()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                SpawnOnce();
                if (spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private void SpawnOnce()
        {
            if (rockPrefab == null)
            {
                if (!_missingPrefabLogged)
                {
                    LogKit.W("FallingRockFromTrashCanController missing rockPrefab.");
                    _missingPrefabLogged = true;
                }

                return;
            }

            var spawnTransform = GetNextSpawnPoint();
            var position = spawnTransform != null ? spawnTransform.position : transform.position;
            var rotation = spawnTransform != null ? spawnTransform.rotation : Quaternion.identity;

            var instance = Instantiate(rockPrefab, position, rotation);
            if (instance != null)
            {
                _activeProjectiles.Add(instance);
                // Simple cleanup if it gets destroyed naturally
                var rocks = _activeProjectiles;
                instance.OnDestroyAsObservable()
                    .Subscribe(_ => rocks.Remove(instance))
                    .AddTo(instance);
            }
        }

        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            if (_spawnIndex < 0 || _spawnIndex >= spawnPoints.Length)
            {
                _spawnIndex = 0;
            }

            var spawnTransform = spawnPoints[_spawnIndex];
            _spawnIndex = (_spawnIndex + 1) % spawnPoints.Length;
            return spawnTransform;
        }

        private bool IsPlayerInside()
        {
            if (_triggerCollider == null)
            {
                return false;
            }

            var count = _triggerCollider.Overlap(_contactFilter, _overlapResults);
            if (count <= 0)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var collider = _overlapResults[i];
                if (collider != null && IsPlayer(collider))
                {
                    return true;
                }
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
