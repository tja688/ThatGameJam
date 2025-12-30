using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.DoorGate.Events;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    /// <summary>
    /// 门表现层控制器（SpriteRenderer 尺寸驱动版）
    /// 负责监听门状态并驱动上门和下门 SpriteRenderer 的高度 (size.y) 进行线性变化
    /// </summary>
    public class DoorMechanism2D : MechanismControllerBase
    {
        [Header("基础配置")]
        [SerializeField] private string doorId;
        [SerializeField] private Collider2D doorCollider; // 整体碰撞体（可选）
        [SerializeField] private bool disableColliderOnOpen = true;

        [Header("门体关联")]
        [SerializeField] private SpriteRenderer upperDoorRenderer; // 上门渲染器
        [SerializeField] private SpriteRenderer lowerDoorRenderer; // 下门渲染器

        [Header("高度参数")]
        [SerializeField] private float upperOpenHeight = 0.0f; // 上门开启时的目标高度
        [SerializeField] private float lowerOpenHeight = 0.0f; // 下门开启时的目标高度
        [SerializeField] private float moveSpeed = 5.0f;       // 变化速度

        // 状态记录
        private float _upperInitialHeight;
        private float _lowerInitialHeight;
        private bool _isOpen;
        private bool _isInitialized;
        private bool _moveLoopPlaying;

        protected override void OnEnable()
        {
            base.OnEnable();
            // 订阅逻辑层的状态变更事件
            this.RegisterEvent<DoorStateChangedEvent>(OnDoorStateChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void Awake()
        {
            if (doorCollider == null)
            {
                doorCollider = GetComponent<Collider2D>();
            }


            InitInitialHeights();
        }

        /// <summary>
        /// 记录游戏开始时的初始高度作为关闭状态的基准
        /// </summary>
        private void InitInitialHeights()
        {
            if (_isInitialized) return;

            if (upperDoorRenderer != null) _upperInitialHeight = upperDoorRenderer.size.y;
            if (lowerDoorRenderer != null) _lowerInitialHeight = lowerDoorRenderer.size.y;

            _isInitialized = true;
        }

        private void Update()
        {
            UpdateDoorsHeight();
        }

        /// <summary>
        /// 每一帧驱动门体高度线性变化
        /// </summary>
        private void UpdateDoorsHeight()
        {
            if (!_isInitialized) return;

            var isMoving = false;

            // 计算上门高度
            if (upperDoorRenderer != null)
            {
                float targetHeight = _isOpen ? upperOpenHeight : _upperInitialHeight;
                Vector2 size = upperDoorRenderer.size;
                size.y = Mathf.MoveTowards(size.y, targetHeight, moveSpeed * Time.deltaTime);
                upperDoorRenderer.size = size;
                if (Mathf.Abs(size.y - targetHeight) > 0.001f)
                {
                    isMoving = true;
                }
            }

            // 计算下门高度
            if (lowerDoorRenderer != null)
            {
                float targetHeight = _isOpen ? lowerOpenHeight : _lowerInitialHeight;
                Vector2 size = lowerDoorRenderer.size;
                size.y = Mathf.MoveTowards(size.y, targetHeight, moveSpeed * Time.deltaTime);
                lowerDoorRenderer.size = size;
                if (Mathf.Abs(size.y - targetHeight) > 0.001f)
                {
                    isMoving = true;
                }
            }

            if (isMoving && !_moveLoopPlaying)
            {
                _moveLoopPlaying = true;
                AudioService.Play("SFX-INT-0003", new AudioContext
                {
                    Owner = transform
                });
            }
            else if (!isMoving && _moveLoopPlaying)
            {
                _moveLoopPlaying = false;
                AudioService.Stop("SFX-INT-0003", new AudioContext
                {
                    Owner = transform
                });
            }
        }

        /// <summary>
        /// 响应硬重置事件
        /// </summary>
        protected override void OnHardReset()
        {
            _isOpen = false;
            if (_moveLoopPlaying)
            {
                _moveLoopPlaying = false;
                AudioService.Stop("SFX-INT-0003", new AudioContext
                {
                    Owner = transform
                });
            }
            // 重置时立即回归初始高度
            if (upperDoorRenderer != null)
            {
                Vector2 size = upperDoorRenderer.size;
                size.y = _upperInitialHeight;
                upperDoorRenderer.size = size;
            }
            if (lowerDoorRenderer != null)
            {
                Vector2 size = lowerDoorRenderer.size;
                size.y = _lowerInitialHeight;
                lowerDoorRenderer.size = size;
            }


            ApplyColliderState();
        }

        private void OnDoorStateChanged(DoorStateChangedEvent e)
        {
            // ID 匹配校验
            if (!string.IsNullOrEmpty(doorId) && e.DoorId != doorId)
            {
                return;
            }

            _isOpen = e.IsOpen;
            ApplyColliderState();
        }

        /// <summary>
        /// 处理碰撞体的即时开关逻辑
        /// </summary>
        private void ApplyColliderState()
        {
            if (disableColliderOnOpen && doorCollider != null)
            {
                doorCollider.enabled = !_isOpen;
            }
        }
    }
}
