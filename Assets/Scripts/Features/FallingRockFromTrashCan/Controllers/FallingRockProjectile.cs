using System.Collections;
using QFramework;
using ThatGameJam.Features.DeathRespawn.Commands;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.FallingRockFromTrashCan.Controllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class FallingRockProjectile : MonoBehaviour, IController
    {
        [Header("Spawn Tuning")]
        [SerializeField] private Vector2 angularSpeedRange = new Vector2(-180f, 180f);
        [SerializeField] private Vector2 scaleRange = new Vector2(0.85f, 1.15f);
        [SerializeField] private Vector2 massRange = new Vector2(0.8f, 1.2f);

        [Header("Hit")]
        [SerializeField] private LayerMask floorLayerMask;
        [SerializeField] private float destroyDelaySeconds = 1f;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private ParticleSystem hitVfx;
        [SerializeField] private AudioSource hitSfx;

        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Renderer[] _renderers;
        private Vector3 _baseScale;
        private bool _hit;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();
            _baseScale = transform.localScale;

            if (_rigidbody2D == null)
            {
                LogKit.E("FallingRockProjectile requires Rigidbody2D.");
            }

            if (_collider2D == null)
            {
                LogKit.E("FallingRockProjectile requires Collider2D.");
            }

            if (floorLayerMask.value == 0)
            {
                var floorLayer = LayerMask.NameToLayer("Floor");
                if (floorLayer >= 0)
                {
                    floorLayerMask = 1 << floorLayer;
                }
                else
                {
                    LogKit.W("FallingRockProjectile floorLayerMask is empty; hits are ignored until set.");
                }
            }

            if (visualRoot == null)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void OnEnable()
        {
            _hit = false;
            if (_collider2D != null)
            {
                _collider2D.enabled = true;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.simulated = true;
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            RestoreVisuals();
            ResetEffects();
            ApplySpawnTuning();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null)
            {
                return;
            }

            TryHandleHit(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHandleHit(other);
        }

        private void TryHandleHit(Collider2D other)
        {
            if (_hit || other == null)
            {
                return;
            }

            // --- Player Hit Detection ---
            if (other.CompareTag("Player"))
            {
                _hit = true;
                this.SendCommand(new MarkPlayerDeadCommand(EDeathReason.Script, transform.position));


                ProcessHitEffects();
                return;
            }

            // --- Floor Hit Detection ---
            if (!IsFloor(other))
            {
                return;
            }

            _hit = true;
            ProcessHitEffects();
        }

        private void ProcessHitEffects()
        {
            if (_collider2D != null)
            {
                _collider2D.enabled = false;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.simulated = false;
            }

            HideVisuals();
            PlayEffects();
            StartCoroutine(DestroyAfterDelay());
        }

        private bool IsFloor(Collider2D other)
        {
            if (floorLayerMask.value == 0)
            {
                return false;
            }

            return (floorLayerMask.value & (1 << other.gameObject.layer)) != 0;
        }

        private void ApplySpawnTuning()
        {
            var scale = Mathf.Max(0.01f, PickRange(scaleRange));
            transform.localScale = new Vector3(_baseScale.x * scale, _baseScale.y * scale, _baseScale.z);

            if (_rigidbody2D == null)
            {
                return;
            }

            _rigidbody2D.mass = Mathf.Max(0.01f, PickRange(massRange));
            _rigidbody2D.angularVelocity = PickRange(angularSpeedRange);
        }

        private void HideVisuals()
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
                return;
            }

            if (_renderers == null)
            {
                return;
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = false;
                }
            }
        }

        private void RestoreVisuals()
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
                return;
            }

            if (_renderers == null)
            {
                return;
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = true;
                }
            }
        }

        private void PlayEffects()
        {
            if (hitVfx != null)
            {
                hitVfx.gameObject.SetActive(true);
                hitVfx.Play(true);
            }

            if (hitSfx != null)
            {
                hitSfx.Play();
            }
        }

        private void ResetEffects()
        {
            if (hitVfx != null)
            {
                hitVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private IEnumerator DestroyAfterDelay()
        {
            if (destroyDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(destroyDelaySeconds);
            }

            Destroy(gameObject);
        }

        private static float PickRange(Vector2 range)
        {
            var min = Mathf.Min(range.x, range.y);
            var max = Mathf.Max(range.x, range.y);
            return Random.Range(min, max);
        }
    }
}
