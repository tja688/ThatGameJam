using System.Collections;
using QFramework;
using ThatGameJam.Features.DeathRespawn.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.DeathRespawn.Controllers
{
    public class RespawnController : MonoBehaviour, IController
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float respawnDelay = 0f;
        [SerializeField] private bool respawnOnDeath = true;
        [SerializeField] private bool respawnOnRunReset = true;
        [SerializeField] private bool resetVelocity = true;

        private Rigidbody2D _rigidbody2D;
        private Coroutine _respawnRoutine;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (resetVelocity)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }
        }

        private void OnEnable()
        {
            if (respawnOnDeath)
            {
                this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
                    .UnRegisterWhenDisabled(gameObject);
            }

            if (respawnOnRunReset)
            {
                this.RegisterEvent<RunResetEvent>(OnRunReset)
                    .UnRegisterWhenDisabled(gameObject);
            }
        }

        private void OnDisable()
        {
            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
                _respawnRoutine = null;
            }
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            RequestRespawn();
        }

        private void OnRunReset(RunResetEvent e)
        {
            RequestRespawn();
        }

        public void RequestRespawn()
        {
            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
                _respawnRoutine = null;
            }

            if (respawnDelay <= 0f)
            {
                ExecuteRespawn();
            }
            else
            {
                _respawnRoutine = StartCoroutine(RespawnAfterDelay());
            }
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            _respawnRoutine = null;
            ExecuteRespawn();
        }

        private void ExecuteRespawn()
        {
            var target = respawnPoint != null ? respawnPoint.position : transform.position;
            transform.position = target;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }

            this.GetSystem<IDeathRespawnSystem>().MarkRespawned(target);
        }
    }
}
