using UnityEngine;
using Animancer;

/// <summary>
/// Spine角色 Animancer 8.0 适配测试
/// 核心特性：
/// 1. 使用 ClipTransition 替代 AnimationClip，方便在 Inspector 设置淡入淡出。
/// 2. 使用 state.Events(this) 这种 v8 推荐的事件归属权写法。
/// 3. 多图层混合测试。
/// </summary>
[AddComponentMenu("Test/Spine Animancer Smoke Test")]
public class SpineAnimancerSmokeTest : MonoBehaviour
{
    [Header("核心组件")]
    [SerializeField] private AnimancerComponent _animancer;

    [Header("基础层 (Layer 0)")]
    [Tooltip("待机动画 (对应 m/standby)")]
    [SerializeField] private ClipTransition _idleTransition;

    [Tooltip("移动动画 (对应 m/walk)")]
    [SerializeField] private ClipTransition _walkTransition;

    [Header("动作层 (Layer 0 - Override)")]
    [Tooltip("指令动画 (对应 One-Shot/Salute/Scared)")]
    [SerializeField] private ClipTransition _actionTransition;

    [Header("叠加层 (Layer 1)")]
    [Tooltip("表情/眨眼动画 (对应 expressions/eyes)")]
    [SerializeField] private ClipTransition _overlayTransition;

    // 内部状态
    private bool _isOverlayActive = false;
    private AnimancerLayer _overlayLayer;

    private void Awake()
    {
        if (_animancer == null)
            _animancer = GetComponent<AnimancerComponent>();

        // 初始化：确保第一帧就播放 Idle
        // 使用 Play(Transition) 是 8.0 的推荐写法
        _animancer.Play(_idleTransition);

        // 初始化叠加层 (Layer 1)
        _overlayLayer = _animancer.Layers[1];
        // 设置遮罩（如果有的话），这里先设权重为1但在没播放时它是不会生效的
        _overlayLayer.SetWeight(1f); 
    }

    private void Update()
    {
        HandleMovement();
        HandleAction();
        HandleOverlay();
    }

    private void HandleMovement()
    {
        // 如果正在播放动作动画（如 Scared/Salute），则暂时不切回移动状态
        // IsPlaying 检查的是特定的 Transition 实例
        if (_animancer.IsPlaying(_actionTransition)) return;

        float input = Input.GetAxis("Horizontal");

        if (Mathf.Abs(input) > 0.1f)
        {
            // 左右翻转
            if (input > 0) transform.localScale = new Vector3(1, 1, 1);
            else if (input < 0) transform.localScale = new Vector3(-1, 1, 1);

            // 播放移动：如果已经是 Walk，Animancer 会自动保持，不会重置
            _animancer.Play(_walkTransition);
        }
        else
        {
            // 播放待机
            _animancer.Play(_idleTransition);
        }
    }

    private void HandleAction()
    {
        // 按 Space 键触发一次性动作
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var state = _animancer.Play(_actionTransition);

            // 【关键修复】Animancer 8.0 写法
            // 传入 'this' 表示当前脚本拥有此事件，防止其他脚本误清理
            state.Events(this).OnEnd = () =>
            {
                // 动作结束后，平滑切回 Idle
                _animancer.Play(_idleTransition);
            };
        }
    }

    private void HandleOverlay()
    {
        // 按 E 键切换叠加动画（眨眼/表情）
        if (Input.GetKeyDown(KeyCode.E))
        {
            _isOverlayActive = !_isOverlayActive;

            if (_isOverlayActive)
            {
                // 在 Layer 1 上播放
                _overlayLayer.Play(_overlayTransition);
                Debug.Log("[Test] 开启叠加动画");
            }
            else
            {
                // 停止 Layer 1 上的动画（带淡出）
                _overlayLayer.Stop(); 
                Debug.Log("[Test] 关闭叠加动画");
            }
        }
    }
}