using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class BugStompInteraction : MonoBehaviour
    {
        [SerializeField, Tooltip("目标虫子移动脚本（为空则在同物体上寻找）。")]
        private BugMovementBase movement;
        [SerializeField, Tooltip("是否启用踩踏反馈（关闭后不影响玩家与虫子）。")]
        private bool enableStompResponse = false;
        [SerializeField, Tooltip("踩踏反弹的最小向上速度（仅启用踩踏反馈时使用）。")]
        private float bounceVelocity = 8f;
        [SerializeField, Tooltip("玩家物体的 Tag（为空则不做 Tag 校验）。")]
        private string playerTag = "Player";

        private void Awake()
        {
            if (movement == null)
            {
                movement = GetComponent<BugMovementBase>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (!enableStompResponse)
            {
                return;
            }

            var body = other.GetComponentInParent<Rigidbody2D>();
            if (body != null)
            {
                var velocity = body.linearVelocity;
                velocity.y = Mathf.Max(velocity.y, bounceVelocity);
                body.linearVelocity = velocity;
            }

            if (movement != null)
            {
                movement.ResetToHome();
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }
    }
}
