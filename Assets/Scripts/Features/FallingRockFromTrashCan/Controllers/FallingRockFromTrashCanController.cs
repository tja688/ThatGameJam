using System.Collections;
using QFramework;
using ThatGameJam.Features.FallingRockFromTrashCan.Utilities;
using UnityEngine;

namespace ThatGameJam.Features.FallingRockFromTrashCan.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class FallingRockFromTrashCanController : MonoBehaviour, IController
    {
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform poolParent;
        [SerializeField] private int preloadCount = 6;
        [SerializeField] private bool triggerOnEnter = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private float spawnInterval = 0f;
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private bool loopSpawn;

        private SimpleGameObjectPool _pool;
        private Coroutine _spawnRoutine;
        private bool _triggered;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("FallingRockFromTrashCanController expects Collider2D.isTrigger = true.");
            }
        }

        private void OnEnable()
        {
            if (_pool == null)
            {
                _pool = new SimpleGameObjectPool(rockPrefab, preloadCount, poolParent);
            }
        }

        private void OnDisable()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            _triggered = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!triggerOnEnter || _triggered || !IsPlayer(other))
            {
                return;
            }

            _triggered = true;
            TriggerSpawn();
        }

        public void TriggerSpawn()
        {
            if (_spawnRoutine != null)
            {
                return;
            }

            _spawnRoutine = StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            if (loopSpawn)
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
            else
            {
                var count = Mathf.Max(1, spawnCount);
                for (var i = 0; i < count; i++)
                {
                    SpawnOnce();
                    if (spawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(spawnInterval);
                    }
                }
            }

            _spawnRoutine = null;
        }

        private void SpawnOnce()
        {
            if (_pool == null)
            {
                _pool = new SimpleGameObjectPool(rockPrefab, preloadCount, poolParent);
            }

            var instance = _pool.Get();
            if (instance == null)
            {
                LogKit.W("FallingRockFromTrashCanController missing rockPrefab.");
                return;
            }

            var spawnTransform = PickSpawnPoint();
            var position = spawnTransform != null ? spawnTransform.position : transform.position;
            var rotation = spawnTransform != null ? spawnTransform.rotation : Quaternion.identity;

            var projectile = instance.GetComponent<FallingRockProjectile>();
            if (projectile != null)
            {
                projectile.SetDespawnHandler(OnRockDespawned);
                projectile.ResetState(position, rotation);
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, rotation);
            }
        }

        private Transform PickSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            var index = Random.Range(0, spawnPoints.Length);
            return spawnPoints[index];
        }

        private void OnRockDespawned(FallingRockProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            _pool.Return(projectile.gameObject);
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }
    }
}
