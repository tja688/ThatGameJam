using UnityEngine;
using Spine.Unity;

/// <summary>
/// Spine 颜色与透明度控制器
/// 挂载在拥有 SkeletonAnimation 的物体上
/// </summary>
[RequireComponent(typeof(SkeletonAnimation))]
public class SpineColorController : MonoBehaviour
{
    private SkeletonAnimation _skeletonAnimation;

    [Header("调试控制")]
    [Tooltip("总体颜色（叠加）")]
    [SerializeField] private Color _targetColor = Color.white;

    [Range(0, 1)] 
    [Tooltip("透明度 (Alpha)")]
    [SerializeField] private float _alpha = 1f;

    void Awake()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    // 在 Update 中实时更新，方便你在编辑器里拖动滑条看效果
    // 实际项目中，你也可以只在需要变化时调用 SetColor
    void Update()
    {
        if (_skeletonAnimation == null) return;

        UpdateColor();
    }

    private void UpdateColor()
    {
        // 获取 Spine 的骨架对象
        var skeleton = _skeletonAnimation.Skeleton;

        // 设置 RGB (控制变暗/变色)
        skeleton.R = _targetColor.r;
        skeleton.G = _targetColor.g;
        skeleton.B = _targetColor.b;

        // 设置 A (控制透明度)
        skeleton.A = _alpha;
    }

    // ==========================================
    // 公共 API：供其他脚本调用
    // ==========================================

    /// <summary>
    /// 设置角色变暗 (模拟受击或者进入阴影)
    /// </summary>
    /// <param name="darkness">0是全黑，1是原色</param>
    public void SetDarkness(float darkness)
    {
        // 保持原来的 Alpha 不变，只改 RGB
        float val = Mathf.Clamp01(darkness);
        _targetColor = new Color(val, val, val, _targetColor.a);
        // 如果不在 Update 里跑，需要手动调一次 UpdateColor();
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    public void SetAlpha(float alpha)
    {
        _alpha = Mathf.Clamp01(alpha);
    }

    /// <summary>
    /// 瞬间恢复正常
    /// </summary>
    public void ResetColor()
    {
        _targetColor = Color.white;
        _alpha = 1f;
    }
}