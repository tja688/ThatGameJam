using DG.Tweening;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// 树木生长控制器 - 使用 DOTween 控制 9-Slice SpriteRenderer 的高度
    /// 支持多种缓动模式切换
    /// </summary>
    public class TreeGrowthController : MonoBehaviour
    {
        [Header("目标引用")]
        [SerializeField] private SpriteRenderer _treeRenderer;

        [Header("线性模式配置 (按键 1)")]
        [SerializeField] private float _linearHeight = 5f;
        [SerializeField] private float _linearDuration = 1f;

        [Header("缓动演示配置 (按键 2-6)")]
        [SerializeField] private float _easedHeight = 5f;
        [SerializeField] private float _easedDuration = 1.2f;

        private bool _isGrown = false;
        private float _targetHeight = 0f;
        private Tween _currentTween;

        private void Start()
        {
            if (_treeRenderer == null)
            {
                _treeRenderer = GetComponent<SpriteRenderer>();
            }

            if (_treeRenderer != null)
            {
                // 确保 SpriteRenderer 设置为 Tiled 或 Sliced 以支持九宫格
                if (_treeRenderer.drawMode == SpriteDrawMode.Simple)
                {
                    _treeRenderer.drawMode = SpriteDrawMode.Tiled;
                    Debug.Log("[TreeGrowth] 自动将 SpriteRenderer DrawMode 设置为 Tiled 以支持九宫格高度变化。");
                }

                // 默认初始化高度
                _targetHeight = _treeRenderer.size.y;
                _isGrown = false;
            }
            else
            {
                Debug.LogError("[TreeGrowth] 未找到 SpriteRenderer，请分配目标对象。");
            }
        }

        private void Update()
        {
            // 实时响应数字键
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                PerformAnimation(Ease.Linear, _linearHeight, _linearDuration, "Linear");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                PerformAnimation(Ease.OutCubic, _easedHeight, _easedDuration, "OutCubic");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                PerformAnimation(Ease.OutSine, _easedHeight, _easedDuration, "OutSine (Sinusoidal)");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                PerformAnimation(Ease.OutBounce, _easedHeight, _easedDuration, "OutBounce");
            }
        }

        /// <summary>
        /// 执行生长或回缩动画
        /// </summary>
        private void PerformAnimation(Ease ease, float targetHeight, float duration, string modeName)
        {
            if (_treeRenderer == null) return;

            // 杀掉当前可能正在进行的补间
            _currentTween?.Kill();

            // 切换状态
            _isGrown = !_isGrown;

            // 注意：当 _isGrown 为 true 时走向配置的 Growth 高度；
            // 当 _isGrown 为 false 时回缩到初始高度 _targetHeight
            float finalY = _isGrown ? targetHeight : _targetHeight;

            Debug.Log($"[TreeGrowth] 模式: {modeName} | 动作: {(_isGrown ? "生长" : "回缩")} | 目标高度: {finalY}");

            // 执行 DOTween 高度变化
            _currentTween = DOTween.To(
                () => _treeRenderer.size.y,
                y => _treeRenderer.size = new Vector2(_treeRenderer.size.x, y),
                finalY,
                duration
            )
            .SetEase(ease);
        }

        [ContextMenu("Reset Height")]
        public void ResetHeight()
        {
            _currentTween?.Kill();
            _treeRenderer.size = new Vector2(_treeRenderer.size.x, _targetHeight);
            _isGrown = false;
        }
    }
}
