using DG.Tweening;
using QFramework;
using ThatGameJam.Features.KeroseneLamp.Queries;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class VineMechanism2D : MechanismControllerBase
    {
        [Header("Light Detection")]
        [SerializeField] private float lightAffectRadius = 3f;
        [SerializeField] private float lightRequiredSeconds = 0f;

        [Header("Growth Animation")]
        [Tooltip("The target height/length when fully activated.")]
        [SerializeField] private float targetGrowthLength = 5f;
        [SerializeField] private float growthDuration = 1.0f;
        [SerializeField] private Vector2 growthDirection = Vector2.up;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D growthCollider;
        [SerializeField] private bool enableColliderDuringGrowth = true;

        [Header("Status")]
        [SerializeField] private float growthValue; // 0 (inactive) to 1 (activated)

        private float _lightTimer;
        private bool _isActivated;
        private Tween _growthTween;

        private Vector2 _initialSpriteSize;
        private float _initialGrowthLength;
        private Vector2 _colliderBaseAnchor;
        private float _colliderThickness;
        private GrowthAxis _axis;
        private float _axisSign = 1f;

        private enum GrowthAxis
        {
            X,
            Y
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (growthCollider == null)
            {
                growthCollider = GetComponent<BoxCollider2D>();
            }

            CacheAxis();

            if (spriteRenderer != null)
            {
                _initialSpriteSize = spriteRenderer.size;
                _initialGrowthLength = (_axis == GrowthAxis.X) ? _initialSpriteSize.x : _initialSpriteSize.y;
            }

            if (growthCollider != null)
            {
                CacheColliderBase();
            }

            ResetGrowthImmediate();
        }

        private void Update()
        {
            var hasLight = IsLighted();
            UpdateLightTimer(hasLight, Time.deltaTime);

            var shouldActivate = (lightRequiredSeconds <= 0f)
                ? hasLight
                : (_lightTimer >= lightRequiredSeconds);

            if (shouldActivate != _isActivated)
            {
                _isActivated = shouldActivate;
                PlayGrowthAnimation(_isActivated ? 1f : 0f);
            }
        }

        private void PlayGrowthAnimation(float target)
        {
            _growthTween?.Kill();
            _growthTween = DOTween.To(() => growthValue, x =>
            {
                growthValue = x;
                ApplyGrowth(growthValue);
            }, target, growthDuration)
            .SetEase(Ease.OutQuint);
        }

        private void ApplyGrowth(float normalized)
        {
            var currentLength = Mathf.Lerp(_initialGrowthLength, targetGrowthLength, normalized);
            SetGrowthInternal(currentLength);

            if (growthCollider != null)
            {
                if (enableColliderDuringGrowth)
                {
                    growthCollider.enabled = normalized > 0.01f;
                }
                else
                {
                    growthCollider.enabled = normalized >= 0.99f;
                }
            }
        }

        private void SetGrowthInternal(float length)
        {
            if (spriteRenderer != null)
            {
                var size = spriteRenderer.size;
                if (_axis == GrowthAxis.X) size.x = length;
                else size.y = length;
                spriteRenderer.size = size;
            }

            if (growthCollider != null)
            {
                Vector2 size;
                Vector2 offset;

                if (_axis == GrowthAxis.X)
                {
                    size = new Vector2(length, _colliderThickness);
                    offset = new Vector2(_colliderBaseAnchor.x + _axisSign * length * 0.5f, _colliderBaseAnchor.y);
                }
                else
                {
                    size = new Vector2(_colliderThickness, length);
                    offset = new Vector2(_colliderBaseAnchor.x, _colliderBaseAnchor.y + _axisSign * length * 0.5f);
                }

                growthCollider.size = size;
                growthCollider.offset = offset;
            }
        }

        protected override void OnHardReset()
        {
            ResetGrowthImmediate();
        }

        private void ResetGrowthImmediate()
        {
            _lightTimer = 0f;
            _isActivated = false;
            growthValue = 0f;
            _growthTween?.Kill();
            ApplyGrowth(0f);
        }

        private bool IsLighted()
        {
            if (lightAffectRadius <= 0f) return false;

            var lamps = this.SendQuery(new GetGameplayEnabledLampsQuery());
            var pos = (Vector2)transform.position;
            var radius = lightAffectRadius;

            foreach (var lamp in lamps)
            {
                if (Vector2.Distance(pos, lamp.WorldPos) <= radius) return true;
            }
            return false;
        }

        private void UpdateLightTimer(bool hasLight, float deltaTime)
        {
            if (lightRequiredSeconds <= 0f)
            {
                _lightTimer = hasLight ? lightRequiredSeconds : 0f;
                return;
            }

            if (hasLight) _lightTimer = Mathf.Min(lightRequiredSeconds, _lightTimer + deltaTime);
            else _lightTimer = Mathf.Max(0f, _lightTimer - deltaTime);
        }

        private void CacheAxis()
        {
            var direction = (growthDirection == Vector2.zero) ? Vector2.up : growthDirection;
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                _axis = GrowthAxis.X;
                _axisSign = Mathf.Sign(direction.x);
            }
            else
            {
                _axis = GrowthAxis.Y;
                _axisSign = Mathf.Sign(direction.y);
            }
            if (_axisSign == 0f) _axisSign = 1f;
        }

        private void CacheColliderBase()
        {
            var size = growthCollider.size;
            var offset = growthCollider.offset;

            if (_axis == GrowthAxis.X)
            {
                _colliderThickness = size.y;
                _colliderBaseAnchor = new Vector2(offset.x - _axisSign * size.x * 0.5f, offset.y);
            }
            else
            {
                _colliderThickness = size.x;
                _colliderBaseAnchor = new Vector2(offset.x, offset.y - _axisSign * size.y * 0.5f);
            }
        }

        private void OnDestroy()
        {
            _growthTween?.Kill();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, lightAffectRadius);

            var direction = (growthDirection == Vector2.zero) ? Vector2.up : growthDirection;
            var end = transform.position + (Vector3)(direction.normalized * targetGrowthLength);
            Gizmos.DrawLine(transform.position, end);
        }
    }
}

