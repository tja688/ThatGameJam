using System;
using QFramework;
using ThatGameJam.Features.Hazard.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.FallingRockFromTrashCan.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class FallingRockProjectile : MonoBehaviour, IController
    {
        [SerializeField] private float lifeSeconds = 6f;
        [SerializeField] private EDeathReason deathReason = EDeathReason.Script;
        [SerializeField] private string playerTag = "Player";

        private Rigidbody2D _rigidbody2D;
        private float _lifeTimer;
        private Action<FallingRockProjectile> _despawnHandler;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _lifeTimer = 0f;
        }

        private void Update()
        {
            if (lifeSeconds <= 0f)
            {
                return;
            }

            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= lifeSeconds)
            {
                Despawn();
            }
        }

        public void SetDespawnHandler(Action<FallingRockProjectile> handler)
        {
            _despawnHandler = handler;
        }

        public void ResetState(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.angularVelocity = 0f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.collider);
            Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other);
            Despawn();
        }

        private void HandleHit(Collider2D collider2D)
        {
            if (collider2D == null)
            {
                return;
            }

            if (IsPlayer(collider2D))
            {
                this.GetSystem<IHazardSystem>().ApplyInstantKill(deathReason, collider2D.transform.position);
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }

        private void Despawn()
        {
            if (_despawnHandler != null)
            {
                _despawnHandler(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
