using DG.Tweening;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Automatically grows a tiled SpriteRenderer and BoxCollider2D to a target length when activated.
    /// Simplified logic based on VineMechanism2D.
    /// </summary>
    public class AutoGrowthController : MonoBehaviour
    {
        [Header("Growth Settings")]
        [SerializeField] private float targetGrowthLength = 5f;
        [SerializeField] private float growthDuration = 2.0f;
        [SerializeField] private Vector2 growthDirection = Vector2.up;
        [SerializeField] private Ease growthEase = Ease.OutQuint;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D growthCollider;

        private Vector2 _initialSpriteSize;
        private float _initialGrowthLength;
        private Vector2 _colliderBaseAnchor;
        private float _colliderThickness;
        private GrowthAxis _axis;
        private float _axisSign = 1f;
        private float _currentGrowthNormalized = 0f;
        private Tween _growthTween;

        private enum GrowthAxis { X, Y }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (growthCollider == null) growthCollider = GetComponent<BoxCollider2D>();

            InitializeGrowthMetrics();

            // Set initial state to 0
            ApplyGrowth(0f);
        }

        private void OnEnable()
        {
            // Start growing automatically when the object is activated
            StartGrowth();
        }

        private void InitializeGrowthMetrics()
        {
            // Calculate Axis
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

            // Cache Initial Values
            if (spriteRenderer != null)
            {
                _initialSpriteSize = spriteRenderer.size;
                _initialGrowthLength = (_axis == GrowthAxis.X) ? _initialSpriteSize.x : _initialSpriteSize.y;
            }

            if (growthCollider != null)
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
        }

        public void StartGrowth()
        {
            _growthTween?.Kill();
            _growthTween = DOTween.To(() => _currentGrowthNormalized, x =>
            {
                _currentGrowthNormalized = x;
                ApplyGrowth(_currentGrowthNormalized);
            }, 1f, growthDuration).SetEase(growthEase);
        }

        private void ApplyGrowth(float normalized)
        {
            float currentLength = Mathf.Lerp(_initialGrowthLength, targetGrowthLength, normalized);

            // Apply to Sprite (needs Tiled mode)
            if (spriteRenderer != null)
            {
                var size = spriteRenderer.size;
                if (_axis == GrowthAxis.X) size.x = currentLength;
                else size.y = currentLength;
                spriteRenderer.size = size;
            }

            // Apply to Collider
            if (growthCollider != null)
            {
                Vector2 size;
                Vector2 offset;

                if (_axis == GrowthAxis.X)
                {
                    size = new Vector2(currentLength, _colliderThickness);
                    offset = new Vector2(_colliderBaseAnchor.x + _axisSign * currentLength * 0.5f, _colliderBaseAnchor.y);
                }
                else
                {
                    size = new Vector2(_colliderThickness, currentLength);
                    offset = new Vector2(_colliderBaseAnchor.x, _colliderBaseAnchor.y + _axisSign * currentLength * 0.5f);
                }

                growthCollider.size = size;
                growthCollider.offset = offset;
            }
        }

        private void OnDestroy()
        {
            _growthTween?.Kill();
        }
    }
}
