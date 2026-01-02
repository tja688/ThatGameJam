using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// 简单上下漂浮：每次单程从快到慢（ease-out）。
    /// </summary>
    [DisallowMultipleComponent]
    public class VerticalFloat : MonoBehaviour
    {
        [Header("Float Settings")]
        [Tooltip("最大上下偏移（基于初始位置）。")]
        [SerializeField] private float floatRange = 0.5f;

        [Tooltip("单程耗时（秒），越小越快。")]
        [SerializeField] private float moveDuration = 1.2f;

        [Tooltip("缓动强度：2 = 二次，3 = 三次，数值越大越明显。")]
        [Range(1f, 6f)]
        [SerializeField] private float easePower = 2f;

        [Tooltip("使用本地坐标，否则使用世界坐标。")]
        [SerializeField] private bool useLocalPosition = true;

        private Vector3 _anchorPosition;
        private float _legElapsed;
        private float _legFromOffset;
        private int _direction = 1;

        private void Awake()
        {
            CacheAnchor();
            ResetLeg();
        }

        private void OnEnable()
        {
            CacheAnchor();
            ResetLeg();
        }

        private void Update()
        {
            var range = Mathf.Max(0f, floatRange);
            if (range <= 0f)
            {
                ApplyOffset(0f);
                return;
            }

            var duration = Mathf.Max(0.01f, moveDuration);
            _legElapsed += Time.deltaTime;

            var t = Mathf.Clamp01(_legElapsed / duration);
            var eased = EaseOut(t);
            var targetOffset = _direction * range;
            var offset = Mathf.Lerp(_legFromOffset, targetOffset, eased);

            ApplyOffset(offset);

            if (t >= 1f)
            {
                _legFromOffset = targetOffset;
                _direction *= -1;
                _legElapsed = 0f;
            }
        }

        private void CacheAnchor()
        {
            _anchorPosition = useLocalPosition ? transform.localPosition : transform.position;
        }

        private void ResetLeg()
        {
            _legElapsed = 0f;
            _legFromOffset = 0f;
            _direction = 1;
        }

        private void ApplyOffset(float yOffset)
        {
            var pos = _anchorPosition;
            pos.y += yOffset;

            if (useLocalPosition)
            {
                transform.localPosition = pos;
            }
            else
            {
                transform.position = pos;
            }
        }

        private float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            var power = Mathf.Max(1f, easePower);
            return 1f - Mathf.Pow(1f - t, power);
        }

        private void OnValidate()
        {
            if (floatRange < 0f) floatRange = 0f;
            if (moveDuration < 0.01f) moveDuration = 0.01f;
            if (easePower < 1f) easePower = 1f;
        }
    }
}
