using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Utilities
{
    public class MovableGrabObjectBase : MonoBehaviour
    {
        [SerializeField] private Transform _movementRoot;
        [SerializeField] private Collider2D _grabRange;
        [SerializeField] private Vector2 _fallbackRangeSize = new Vector2(1.5f, 1.5f);
        [SerializeField] private Vector2 _fallbackRangeOffset;

        private Collider2D _cachedCollider;

        public Vector2 AnchorPosition
        {
            get
            {
                var root = _movementRoot != null ? _movementRoot : transform;
                return root.position;
            }
        }

        public bool IsPointWithinRange(Vector2 worldPoint)
        {
            var rangeCollider = ResolveRangeCollider();
            if (rangeCollider != null)
            {
                return rangeCollider.OverlapPoint(worldPoint);
            }

            var center = AnchorPosition + _fallbackRangeOffset;
            var half = _fallbackRangeSize * 0.5f;
            return worldPoint.x >= center.x - half.x &&
                   worldPoint.x <= center.x + half.x &&
                   worldPoint.y >= center.y - half.y &&
                   worldPoint.y <= center.y + half.y;
        }

        private Collider2D ResolveRangeCollider()
        {
            if (_grabRange != null)
            {
                return _grabRange;
            }

            if (_cachedCollider == null)
            {
                _cachedCollider = GetComponent<Collider2D>();
            }

            return _cachedCollider;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_grabRange == null)
            {
                _cachedCollider = GetComponent<Collider2D>();
            }
        }
#endif
    }
}
