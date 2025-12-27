using QFramework;
using ThatGameJam.Features.KeroseneLamp.Queries;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class VineMechanism2D : MechanismControllerBase
    {
        [Header("Light")]
        [SerializeField] private float lightAffectRadius = 3f;
        [SerializeField] private float lightRequiredSeconds = 0f;

        [Header("Growth")]
        [SerializeField] private float growthSpeed = 1f;
        [SerializeField] private float decaySpeed = 0.75f;
        [SerializeField] private Vector2 growthDirection = Vector2.up;
        [SerializeField] private float growthLength = 3f;
        [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private BoxCollider2D growthCollider;
        [SerializeField] private bool enableColliderDuringGrowth = true;

        [SerializeField] [Range(0f, 1f)] private float growthValue;

        private float _lightTimer;
        private Vector3 _visualFullScale;
        private Vector2 _colliderBaseAnchor;
        private float _colliderThickness;
        private GrowthAxis _axis;
        private float _axisSign = 1f;
        private bool _scaleTransformOnly;

        private enum GrowthAxis
        {
            X,
            Y
        }

        private void Awake()
        {
            if (growthCollider == null)
            {
                growthCollider = GetComponent<BoxCollider2D>();
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _scaleTransformOnly = visualRoot != null
                && growthCollider != null
                && visualRoot == growthCollider.transform;

            CacheAxis();

            if (growthCollider != null)
            {
                if (!_scaleTransformOnly)
                {
                    CacheColliderBase();
                }
            }
            else
            {
                LogKit.W("VineMechanism2D missing BoxCollider2D; platform collision will not be generated.");
            }

            if (visualRoot != null)
            {
                _visualFullScale = visualRoot.localScale;
            }

            ResetGrowthImmediate();
        }

        private void Update()
        {
            var hasLight = IsLighted();
            UpdateLightTimer(hasLight, Time.deltaTime);

            var isLightReady = lightRequiredSeconds <= 0f
                ? hasLight
                : _lightTimer >= lightRequiredSeconds;

            var target = isLightReady ? 1f : 0f;
            var speed = isLightReady ? growthSpeed : decaySpeed;

            growthValue = Mathf.MoveTowards(growthValue, target, Mathf.Max(0f, speed) * Time.deltaTime);
            ApplyGrowth(growthValue);
        }

        protected override void OnHardReset()
        {
            ResetGrowthImmediate();
        }

        private bool IsLighted()
        {
            if (lightAffectRadius <= 0f)
            {
                return false;
            }

            var lamps = this.SendQuery(new GetGameplayEnabledLampsQuery());
            var pos = (Vector2)transform.position;
            var radius = lightAffectRadius;

            for (var i = 0; i < lamps.Count; i++)
            {
                var lampPos = (Vector2)lamps[i].WorldPos;
                if (Vector2.Distance(pos, lampPos) <= radius)
                {
                    return true;
                }
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

            if (hasLight)
            {
                _lightTimer = Mathf.Min(lightRequiredSeconds, _lightTimer + deltaTime);
            }
            else
            {
                _lightTimer = Mathf.Max(0f, _lightTimer - deltaTime);
            }
        }

        private void CacheAxis()
        {
            var direction = growthDirection;
            if (direction == Vector2.zero)
            {
                direction = Vector2.up;
            }

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

            if (_axisSign == 0f)
            {
                _axisSign = 1f;
            }
        }

        private void CacheColliderBase()
        {
            var size = growthCollider.size;
            var offset = growthCollider.offset;

            // Preserve the base anchor so length changes extend away from the base.
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

        private void ApplyGrowth(float normalized)
        {
            var clamped = Mathf.Clamp01(normalized);
            var eased = growthCurve != null ? growthCurve.Evaluate(clamped) : clamped;
            SetGrowth(growthLength * eased);

            if (growthCollider != null)
            {
                if (enableColliderDuringGrowth)
                {
                    growthCollider.enabled = clamped > 0f;
                }
                else
                {
                    growthCollider.enabled = clamped >= 1f;
                }
            }
        }

        private void ResetGrowthImmediate()
        {
            _lightTimer = 0f;
            growthValue = 0f;
            ApplyGrowth(0f);
        }

        private void SetGrowth(float length)
        {
            var clampedLength = Mathf.Max(0f, length);
            var lengthScale = growthLength > 0f ? Mathf.Clamp01(clampedLength / growthLength) : 0f;

            if (visualRoot != null)
            {
                var scale = _visualFullScale;
                if (_axis == GrowthAxis.X)
                {
                    scale.x = _visualFullScale.x * lengthScale;
                }
                else
                {
                    scale.y = _visualFullScale.y * lengthScale;
                }

                visualRoot.localScale = scale;
            }

            if (growthCollider != null)
            {
                if (_scaleTransformOnly)
                {
                    return;
                }

                Vector2 size;
                Vector2 offset;

                if (_axis == GrowthAxis.X)
                {
                    size = new Vector2(clampedLength, _colliderThickness);
                    offset = new Vector2(_colliderBaseAnchor.x + _axisSign * clampedLength * 0.5f, _colliderBaseAnchor.y);
                }
                else
                {
                    size = new Vector2(_colliderThickness, clampedLength);
                    offset = new Vector2(_colliderBaseAnchor.x, _colliderBaseAnchor.y + _axisSign * clampedLength * 0.5f);
                }

                growthCollider.size = size;
                growthCollider.offset = offset;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, lightAffectRadius);

            var direction = growthDirection == Vector2.zero ? Vector2.up : growthDirection;
            var end = transform.position + (Vector3)(direction.normalized * growthLength);
            Gizmos.DrawLine(transform.position, end);
        }
    }
}
