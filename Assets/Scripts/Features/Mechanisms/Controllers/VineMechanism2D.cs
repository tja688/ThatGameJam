using System.Collections;
using QFramework;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class VineMechanism2D : MechanismControllerBase
    {
        [SerializeField] private float lightAffectRadius = 3f;
        [SerializeField] private Vector2 growthDirection = Vector2.up;
        [SerializeField] private float growthLength = 3f;
        [SerializeField] private float growthDuration = 0.5f;
        [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private Transform visualRoot;
        [SerializeField] private BoxCollider2D growthCollider;
        [SerializeField] private bool enableColliderDuringGrowth = true;

        private Coroutine _growRoutine;
        private bool _isGrown;
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

        private void OnEnable()
        {
            this.RegisterEvent<LampSpawnedEvent>(OnLampSpawned)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<RunResetEvent>(OnRunReset)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnDisable()
        {
            StopGrowthRoutine();
        }

        private void OnLampSpawned(LampSpawnedEvent e)
        {
            if (_isGrown || _growRoutine != null)
            {
                return;
            }

            var distance = Vector2.Distance(transform.position, e.WorldPos);
            if (distance > lightAffectRadius)
            {
                return;
            }

            StartGrowth();
        }

        private void OnRunReset(RunResetEvent e)
        {
            ResetGrowthImmediate();
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

        private void StartGrowth()
        {
            StopGrowthRoutine();

            if (growthCollider != null)
            {
                growthCollider.enabled = enableColliderDuringGrowth || growthDuration <= 0f;
            }

            if (growthDuration <= 0f)
            {
                SetGrowth(growthLength);
                CompleteGrowth();
                return;
            }

            _growRoutine = StartCoroutine(GrowRoutine());
        }

        private IEnumerator GrowRoutine()
        {
            var elapsed = 0f;
            while (elapsed < growthDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / growthDuration);
                var eased = growthCurve != null ? growthCurve.Evaluate(t) : t;
                SetGrowth(growthLength * eased);
                yield return null;
            }

            SetGrowth(growthLength);
            CompleteGrowth();
        }

        private void CompleteGrowth()
        {
            _growRoutine = null;
            _isGrown = true;

            if (growthCollider != null && !enableColliderDuringGrowth)
            {
                growthCollider.enabled = true;
            }
        }

        private void ResetGrowthImmediate()
        {
            StopGrowthRoutine();
            _isGrown = false;

            SetGrowth(0f);

            if (growthCollider != null)
            {
                growthCollider.enabled = false;
            }
        }

        private void StopGrowthRoutine()
        {
            if (_growRoutine != null)
            {
                StopCoroutine(_growRoutine);
                _growRoutine = null;
            }
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
