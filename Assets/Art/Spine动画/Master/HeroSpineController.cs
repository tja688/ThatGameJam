using UnityEngine;
using Spine.Unity;
using Spine;

[RequireComponent(typeof(SkeletonAnimation))]
public class HeroSpineController : MonoBehaviour
{
    [Header("Spine 组件")]
    public SkeletonAnimation skeletonAnimation;

    [Header("动画名称配置 (请核对Inspector中的名字)")]
    // 这里预填了你截图里的 "m" 系列名字，如果是 "w" 系列可以在面板里改
    [SerializeField] private string animIdleNormal = "m/standby-m";
    [SerializeField] private string animWalkNormal = "m/walk-m";
    
    [SerializeField] private string animIdleEyesOpen = "m/eyesopen/standby-m-eo";
    [SerializeField] private string animWalkEyesOpen = "m/eyesopen/walk-m-eo";
    
    [SerializeField] private string animScared = "scared";

    [Header("设置")]
    [SerializeField] private float moveSpeed = 5f;

    // 内部状态标记
    private bool _isEyesOpen = false; // Q键切换
    private bool _isScared = false;   // 是否正在受惊状态中

    void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    void Update()
    {
        // 1. 如果正在受惊（播放 scared 动画中），则不响应移动和切换输入
        if (_isScared) return;

        // 2. 处理特殊输入：E 键受惊
        if (Input.GetKeyDown(KeyCode.E))
        {
            PlayScared();
            return; // 受惊触发那一帧，直接返回
        }

        // 3. 处理状态切换：Q 键切换睁眼/闭眼
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _isEyesOpen = !_isEyesOpen;
            // 状态改变了，立刻刷新一次动画，防止延迟
            UpdateMovementAnimation(Input.GetAxis("Horizontal")); 
        }

        // 4. 处理移动逻辑 (WD / 方向键)
        float inputX = Input.GetAxis("Horizontal");
        MoveAndAnimate(inputX);
    }

    // 处理移动和常规动画切换
    private void MoveAndAnimate(float inputX)
    {
        // --- 物理/位移逻辑 ---
        if (Mathf.Abs(inputX) > 0.01f)
        {
            // 简单的位移
            transform.Translate(Vector3.right * inputX * moveSpeed * Time.deltaTime);

            // 处理翻转 (Spine 原生翻转是修改 ScaleX)
            if (inputX > 0)
                skeletonAnimation.Skeleton.ScaleX = 1; // 向右
            else
                skeletonAnimation.Skeleton.ScaleX = -1; // 向左
        }

        // --- 动画逻辑 ---
        UpdateMovementAnimation(inputX);
    }

    // 根据当前的输入和状态，决定播放哪个动画
    private void UpdateMovementAnimation(float inputX)
    {
        bool isMoving = Mathf.Abs(inputX) > 0.01f;
        string targetAnimName = "";

        // 根据状态组合出目标动画名
        if (_isEyesOpen)
        {
            targetAnimName = isMoving ? animWalkEyesOpen : animIdleEyesOpen;
        }
        else
        {
            targetAnimName = isMoving ? animWalkNormal : animIdleNormal;
        }

        // 核心：调用 Spine 播放，True 表示循环
        PlayAnimationCheck(targetAnimName, true);
    }

    // 触发受惊逻辑
    private void PlayScared()
    {
        _isScared = true; // 锁定状态

        // 播放 scared，不循环 (loop = false)
        var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, animScared, false);
        
        // 监听：当 scared 播放完毕时，解除锁定
        trackEntry.Complete += OnScaredComplete;
    }

    // 受惊动画播放完毕的回调
    private void OnScaredComplete(TrackEntry trackEntry)
    {
        _isScared = false; // 解锁
        trackEntry.Complete -= OnScaredComplete; // 这是一个好习惯，取消订阅防止内存泄漏
        
        // 动作做完了，恢复到当前的移动/待机状态
        UpdateMovementAnimation(Input.GetAxis("Horizontal"));
    }

    /// <summary>
    /// 辅助方法：防止重复播放同一个动画导致重置（Spine常见错误）
    /// </summary>
    private void PlayAnimationCheck(string newAnimName, bool loop)
    {
        // 获取当前轨道 0 正在播放的动画
        var currentTrack = skeletonAnimation.AnimationState.GetCurrent(0);

        // 如果当前有动画，且名字和新的一样，且正在播放中 -> 不需要重新 SetAnimation
        if (currentTrack != null && currentTrack.Animation.Name == newAnimName && !currentTrack.IsComplete)
        {
            return; 
        }

        // 否则，切换新动画
        skeletonAnimation.AnimationState.SetAnimation(0, newAnimName, loop);
    }
}