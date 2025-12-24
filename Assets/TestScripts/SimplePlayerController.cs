using UnityEngine;
using UnityEngine.InputSystem;

namespace ThatGameJam.Test
{
    /// <summary>
    /// 一个简单的2D角色控制器，用于快速原型测试。
    /// 需显式在 Inspector 关联 InputActionReference。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("输入配置 (外部关联)")]
        [Tooltip("关联移动 Action (Value, Vector2)")]
        public InputActionReference moveAction;
        [Tooltip("关联跳跃 Action (Button)")]
        public InputActionReference jumpAction;

        [Header("移动配置")]
        [Tooltip("角色左右移动的速度")]
        public float moveSpeed = 8f;

        [Tooltip("角色跳跃的高度")]
        public float jumpHeight = 3f;

        [Header("地面检测")]
        [Tooltip("地面检测点（通常放在角色脚下），如果不传则默认使用物体中心位置向下偏移")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.2f;
        public LayerMask groundLayer = 1; // 默认 Default 层

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private bool _isGrounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (jumpAction != null)
            {
                jumpAction.action.Enable();
                jumpAction.action.performed += OnJumpPerformed;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null)
            {
                jumpAction.action.performed -= OnJumpPerformed;
                jumpAction.action.Disable();
            }
        }

        private void Update()
        {
            // 读取移动输入
            if (moveAction != null)
            {
                _moveInput = moveAction.action.ReadValue<Vector2>();
            }

            // 地面检测
            Vector3 checkPos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.5f;
            _isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

            // 更新方向
            if (_moveInput.x != 0)
            {
                transform.localScale = new Vector3(Mathf.Sign(_moveInput.x), 1, 1);
            }
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = new Vector2(_moveInput.x * moveSpeed, _rb.linearVelocity.y);
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (_isGrounded)
            {
                DoJump();
            }
        }

        private void DoJump()
        {
            float jumpVelocity = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics2D.gravity.y) * _rb.gravityScale);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpVelocity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 checkPos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.5f;
            Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
        }
    }
}
