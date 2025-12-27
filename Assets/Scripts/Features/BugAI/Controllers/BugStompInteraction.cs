using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class BugStompInteraction : MonoBehaviour
    {
        [SerializeField] private BugMovementBase movement;
        [SerializeField] private float bounceVelocity = 8f;
        [SerializeField] private string playerTag = "Player";

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
