using QFramework;
using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class ElectricLure : MonoBehaviour
    {
        [SerializeField] private int priority = 100;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("ElectricLure expects Collider2D.isTrigger = true.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ApplyLure(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ApplyLure(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<BugMovementBase>();
            if (movement == null)
            {
                return;
            }

            movement.ClearTarget(this);
        }

        private void ApplyLure(Collider2D other)
        {
            var movement = other.GetComponentInParent<BugMovementBase>();
            if (movement == null)
            {
                return;
            }

            movement.SetTarget(this, transform.position, priority);
        }
    }
}
